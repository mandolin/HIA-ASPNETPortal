using System;
using System.Linq;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
        /// 维护历史模块定义名称并执行受保护删除检查的 Legacy 页面。
        /// Legacy page that maintains historical module-definition names and performs protected deletion checks.
        /// </summary>
        /// <remarks>
        /// 新模块定义必须在 <c>ModuleCatalog.aspx</c> 从已验证部署包登记。本页不再允许创建定义或修改桌面/移动入口，
        /// 以避免恢复任意动态加载路径。
        /// New module definitions must be registered from a validated deployment package in <c>ModuleCatalog.aspx</c>.
        /// This page no longer permits creating a definition or changing desktop/mobile entries, preventing arbitrary
        /// dynamic-load paths from returning.
        /// </remarks>
    public partial class ModuleDefinitions : PortalPage<ModuleDefinitions>
    {
        private int defId;
        private int tabId;
        private int tabIndex;
        private IModuleDefinitionItem currentDefinition;

        /// <summary>
        /// 旧模块定义数据服务，用于读取、更新名称及受保护删除。
        /// Legacy module-definition data service used for reading, display-name updates, and protected deletion.
        /// </summary>
        [Dependency]
        public IModuleDefsDb ModuleDefConfig { private get; set; }

        /// <summary>
        /// 模块实例数据服务，用于在删除前检查引用数量。
        /// Module-instance data service used to check reference count before deletion.
        /// </summary>
        [Dependency]
        public IModulesDb ModulesConfig { private get; set; }

        /// <summary>
        /// 验证管理员访问权，读取查询参数，并初始化历史模块定义信息。
        /// Requires administrator access, reads query parameters, and initializes legacy module-definition information.
        /// </summary>
        /// <param name="sender">事件源。Event source.</param>
        /// <param name="e">事件参数。Event arguments.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                BindDefinition();
            }
        }

        /// <summary>
        /// 中文：授权、验证导航参数，并绑定当前门户中存在的模块定义。
        ///
        /// English: Authorizes the request, validates navigation parameters, and binds an existing module definition.
        /// </summary>
        /// <returns>中文：请求可继续操作已验证定义时为 <c>true</c>。English: <c>true</c> when the request may operate on a verified definition.</returns>
        private bool TryInitializeRequest()
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.ModuleDefinitionEdit))
            {
                return false;
            }

            if (!TryReadOptionalPositiveParameter("tabid", out tabId) ||
                !TryReadOptionalNonNegativeParameter("tabindex", out tabIndex))
            {
                return false;
            }

            string rawDefinitionId = Request.Params["defid"];
            if (string.IsNullOrWhiteSpace(rawDefinitionId))
            {
                PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ResolveUrl("~/Admin/ModuleCatalog.aspx"));
                return false;
            }

            if (!PortalNavigationPolicy.TryReadPositiveInt32(rawDefinitionId, out defId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            currentDefinition = ModuleDefConfig.GetModuleDefinitions()
                .FirstOrDefault(item => item.ModuleDefId == defId);
            if (currentDefinition != null)
            {
                return true;
            }

            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        /// <summary>
        /// 更新历史模块定义的显示名称，并保留其已受控的路径。
        /// Updates a legacy module-definition display name while preserving its controlled paths.
        /// </summary>
        /// <param name="sender">事件源。Event source.</param>
        /// <param name="e">事件参数。Event arguments.</param>
        /// <remarks>
        /// 新建请求会跳转目录页；已有定义只更新名称并写入运营审计，不重新验证或修改 DesktopSrc/MobileSrc。
        /// A create request redirects to the catalog; an existing definition updates only its name and records an
        /// operations audit, without revalidating or changing DesktopSrc/MobileSrc.
        /// </remarks>
        protected void UpdateBtn_Click(Object sender, EventArgs e)
        {
            if (!TryInitializeRequest() || !Page.IsValid)
            {
                return;
            }

            string friendlyName;
            if (!PortalAdministrationPolicy.TryNormalizeRequiredSingleLineText(FriendlyName.Text, 150, out friendlyName))
            {
                ShowMessage("模块定义名称无效，未保存本次修改。");
                return;
            }

            try
            {
                // 中文：保留既有受控路径，避免通过 legacy 表单创建或变更任意动态加载入口。
                // English: Preserve existing controlled paths so the legacy form cannot create or change arbitrary dynamic-load entries.
                ModuleDefConfig.UpdateModuleDefinition(
                    defId,
                    friendlyName,
                    currentDefinition.DesktopSourceFile,
                    currentDefinition.MobileSourceFile);
                PortalOperationAudit.Record(
                    "ModuleDefinition",
                    "UpdateName",
                    "ModuleDefinition",
                    defId.ToString(),
                    "Updated the legacy module-definition display name.",
                    Context);
                PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, BuildPortalReturnUrl());
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.ModuleDefinitions.Update",
                    "Updating a legacy module definition failed. ModuleDefinitionId=" + defId,
                    exception,
                    Context);
                ShowMessage("模块定义保存失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        /// <summary>
        /// 删除未被模块实例引用的历史模块定义。
        /// Deletes a legacy module definition that no module instance references.
        /// </summary>
        /// <param name="sender">事件源。Event source.</param>
        /// <param name="e">事件参数。Event arguments.</param>
        /// <remarks>
        /// 引用数量大于零时拒绝删除，要求先禁用、迁移或显式清理实例。删除成功会写入运营审计；不会删除物理部署目录。
        /// Deletion is refused when references exist, requiring instances to be disabled, migrated, or explicitly
        /// cleaned first. A successful deletion writes an operations audit and never deletes a physical deployment directory.
        /// </remarks>
        protected void DeleteBtn_Click(Object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            int instanceCount = ModulesConfig.GetModulesByModuleDefId(defId).Count();
            if (instanceCount > 0)
            {
                // 旧删除会级联清除业务模块数据。P3.2 要求先禁用、迁移或显式清理实例。
                // Legacy deletion cascades into business module data. P3.2 requires disabling, migration, or
                // explicit instance cleanup first.
                ShowMessage("该模块定义仍被 " + instanceCount + " 个模块实例使用。请先禁用、迁移或显式清理这些实例。");
                return;
            }

            try
            {
                ModuleDefConfig.DeleteModuleDefinition(defId);
                PortalOperationAudit.Record(
                    "ModuleDefinition",
                    "Delete",
                    "ModuleDefinition",
                    defId.ToString(),
                    "Deleted an unused legacy module definition.",
                    Context);
                PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, BuildPortalReturnUrl());
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.ModuleDefinitions.Delete",
                    "Deleting a legacy module definition failed. ModuleDefinitionId=" + defId,
                    exception,
                    Context);
                ShowMessage("模块定义删除失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        /// <summary>
        /// 取消当前编辑并返回指定门户 Tab。
        /// Cancels the current edit and returns to the specified portal Tab.
        /// </summary>
        /// <param name="sender">事件源。Event source.</param>
        /// <param name="e">事件参数。Event arguments.</param>
        protected void CancelBtn_Click(Object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, BuildPortalReturnUrl());
        }

        /// <summary>
        /// 为历史表单保留的服务器端模块路径校验。
        /// Server-side module-path validation retained for the legacy form.
        /// </summary>
        /// <remarks>
        /// 当前 UI 已将路径字段设为只读，新定义也重定向到目录页；保留此处理器是为了不破坏现有 ASPX 验证契约。
        /// Current UI marks path fields read-only and redirects new definitions to the catalog; this handler remains so
        /// the existing ASPX validation contract is not broken.
        /// </remarks>
        /// <param name="source">验证器控件。Validator control.</param>
        /// <param name="args">包含待校验路径及验证结果的事件参数。
        /// Event arguments containing the path to validate and the validation result.</param>
        protected void DesktopSrcPathValidator_ServerValidate(object source, ServerValidateEventArgs args)
        {
            string normalizedSource;
            string errorMessage;
            args.IsValid = PortalModulePathValidator.TryNormalizeDesktopSource(args.Value, out normalizedSource, out errorMessage);
        }

        /// <summary>
        /// 规范化当前历史表单中的桌面入口，供保留的兼容调用点使用。
        /// Normalizes the desktop entry in the current legacy form for retained compatibility call sites.
        /// </summary>
        /// <returns>已通过路径校验的站内相对入口。
        /// Site-relative entry that passed path validation.</returns>
        /// <exception cref="InvalidOperationException">路径不符合受限动态加载边界时抛出。
        /// Thrown when the path does not meet the constrained dynamic-loading boundary.</exception>
        private string NormalizeDesktopSrc()
        {
            string normalizedSource;
            string errorMessage;
            if (!PortalModulePathValidator.TryNormalizeDesktopSource(DesktopSrc.Text, out normalizedSource, out errorMessage))
            {
                throw new InvalidOperationException(errorMessage);
            }

            return normalizedSource;
        }

        private bool TryReadOptionalPositiveParameter(string parameterName, out int value)
        {
            value = 0;
            string rawValue = Request.Params[parameterName];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (PortalNavigationPolicy.TryReadPositiveInt32(rawValue, out value))
            {
                return true;
            }

            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        private bool TryReadOptionalNonNegativeParameter(string parameterName, out int value)
        {
            value = 0;
            string rawValue = Request.Params[parameterName];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (PortalNavigationPolicy.TryReadNonNegativeInt32(rawValue, out value))
            {
                return true;
            }

            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        private void BindDefinition()
        {
            FriendlyName.Text = currentDefinition.FriendlyName;
            DesktopSrc.Text = currentDefinition.DesktopSourceFile;
            MobileSrc.Text = currentDefinition.MobileSourceFile;
            DesktopSrc.ReadOnly = true;
            MobileSrc.ReadOnly = true;
            Req2.Enabled = false;
            DesktopSrcPathValidator.Enabled = false;
        }

        private string BuildPortalReturnUrl()
        {
            if (tabId > 0)
            {
                return ResolveUrl("~/DesktopDefault.aspx?tabindex=" + tabIndex + "&tabid=" + tabId);
            }

            return ResolveUrl("~/Default.aspx");
        }

        private void ShowMessage(string message)
        {
            MessageLabel.Text = Server.HtmlEncode(message ?? string.Empty);
        }
    }
}
