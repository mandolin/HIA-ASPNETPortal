using System;
using System.Linq;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>维护历史模块定义名称并执行受保护删除检查的 Legacy 页面。</zh-CN>
    ///   <en>Legacy page that maintains historical module-definition names and performs protected deletion checks.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>新模块定义必须在 <c>ModuleCatalog.aspx</c> 从已验证部署包登记。本页不再允许创建定义或修改桌面/移动入口，以避免恢复任意动态加载路径。</zh-CN>
    ///   <en>New module definitions must be registered from a validated deployment package in <c>ModuleCatalog.aspx</c>. This page no longer permits creating a definition or changing desktop/mobile entries, preventing arbitrary dynamic-load paths from returning.</en>
    /// </lang>
    /// </remarks>
    public partial class ModuleDefinitions : PortalPage<ModuleDefinitions>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>当前正在维护的模块定义标识。</zh-CN>
        ///   <en>The module-definition identifier currently being maintained.</en>
        /// </lang>
        /// </summary>
        private int defId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回门户页面时使用的可选 Tab 标识。</zh-CN>
        ///   <en>The optional Tab identifier used when returning to a portal page.</en>
        /// </lang>
        /// </summary>
        private int tabId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回门户页面时使用的可选 Tab 索引。</zh-CN>
        ///   <en>The optional Tab index used when returning to a portal page.</en>
        /// </lang>
        /// </summary>
        private int tabIndex;

        /// <summary>
        /// <lang>
        ///   <zh-CN>已通过权限和导航参数校验的当前模块定义快照。</zh-CN>
        ///   <en>The current module-definition snapshot after permission and navigation-parameter validation.</en>
        /// </lang>
        /// </summary>
        private IModuleDefinitionItem currentDefinition;

        /// <summary>
        /// <lang>
        ///   <zh-CN>旧模块定义数据服务，用于读取、更新名称及受保护删除。</zh-CN>
        ///   <en>Legacy module-definition data service used for reading, display-name updates, and protected deletion.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IModuleDefsDb ModuleDefConfig { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>模块实例数据服务，用于在删除前检查引用数量。</zh-CN>
        ///   <en>Module-instance data service used to check reference count before deletion.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IModulesDb ModulesConfig { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证管理员访问权，读取查询参数，并初始化历史模块定义信息。</zh-CN>
        ///   <en>Requires administrator access, reads query parameters, and initializes legacy module-definition information.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发页面加载的 Web Forms 事件源。</zh-CN>
        ///   <en>The Web Forms event source that triggered page loading.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>页面加载事件参数。</zh-CN>
        ///   <en>The page-load event arguments.</en>
        /// </l>
        /// </param>
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
        /// <lang>
        ///   <zh-CN>授权、验证导航参数，并绑定当前门户中存在的模块定义。</zh-CN>
        ///   <en>Authorizes the request, validates navigation parameters, and binds an existing module definition.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>请求可继续操作已验证定义时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the request may operate on a verified definition.</en>
        /// </l>
        /// </returns>
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
        /// <lang>
        ///   <zh-CN>更新历史模块定义的显示名称，并保留其已受控的路径。</zh-CN>
        ///   <en>Updates a legacy module-definition display name while preserving its controlled paths.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发更新的按钮事件源。</zh-CN>
        ///   <en>The button event source that triggered the update.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>按钮事件参数。</zh-CN>
        ///   <en>The button event arguments.</en>
        /// </l>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>新建请求会跳转目录页；已有定义只更新名称并写入运营审计，不重新验证或修改 DesktopSrc/MobileSrc。</zh-CN>
        ///   <en>A create request redirects to the catalog; an existing definition updates only its name and records an operations audit, without revalidating or changing DesktopSrc/MobileSrc.</en>
        /// </lang>
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
                /*
                 * <lang>
                 *   <zh-CN>保留既有受控路径，避免通过 legacy 表单创建或变更任意动态加载入口。</zh-CN>
                 *   <en>Preserve existing controlled paths so the legacy form cannot create or change arbitrary dynamic-load entries.</en>
                 * </lang>
                 */
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
        /// <lang>
        ///   <zh-CN>删除未被模块实例引用的历史模块定义。</zh-CN>
        ///   <en>Deletes a legacy module definition that no module instance references.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发删除的按钮事件源。</zh-CN>
        ///   <en>The button event source that triggered deletion.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>按钮事件参数。</zh-CN>
        ///   <en>The button event arguments.</en>
        /// </l>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>引用数量大于零时拒绝删除，要求先禁用、迁移或显式清理实例。删除成功会写入运营审计；不会删除物理部署目录。</zh-CN>
        ///   <en>Deletion is refused when references exist, requiring instances to be disabled, migrated, or explicitly cleaned first. A successful deletion writes an operations audit and never deletes a physical deployment directory.</en>
        /// </lang>
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
                /*
                 * <lang>
                 *   <zh-CN>旧删除会级联清除业务模块数据；这里先阻断被引用定义，要求管理员先处理实例级影响。</zh-CN>
                 *   <en>Legacy deletion cascades into business module data, so referenced definitions are blocked here and administrators must handle instance-level impact first.</en>
                 * </lang>
                 */
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
        /// <lang>
        ///   <zh-CN>取消当前编辑并返回指定门户 Tab。</zh-CN>
        ///   <en>Cancels the current edit and returns to the specified portal Tab.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发取消的按钮事件源。</zh-CN>
        ///   <en>The button event source that triggered cancellation.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>按钮事件参数。</zh-CN>
        ///   <en>The button event arguments.</en>
        /// </l>
        /// </param>
        protected void CancelBtn_Click(Object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, BuildPortalReturnUrl());
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>为历史表单保留的服务器端模块路径校验。</zh-CN>
        ///   <en>Server-side module-path validation retained for the legacy form.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>当前 UI 已将路径字段设为只读，新定义也重定向到目录页；保留此处理器是为了不破坏现有 ASPX 验证契约。</zh-CN>
        ///   <en>Current UI marks path fields read-only and redirects new definitions to the catalog; this handler remains so the existing ASPX validation contract is not broken.</en>
        /// </lang>
        /// </remarks>
        /// <param name="source">
        /// <l>
        ///   <zh-CN>验证器控件。</zh-CN>
        ///   <en>The validator control.</en>
        /// </l>
        /// </param>
        /// <param name="args">
        /// <l>
        ///   <zh-CN>包含待校验路径及验证结果的事件参数。</zh-CN>
        ///   <en>Event arguments containing the path to validate and the validation result.</en>
        /// </l>
        /// </param>
        protected void DesktopSrcPathValidator_ServerValidate(object source, ServerValidateEventArgs args)
        {
            string normalizedSource;
            string errorMessage;
            args.IsValid = PortalModulePathValidator.TryNormalizeDesktopSource(args.Value, out normalizedSource, out errorMessage);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>规范化当前历史表单中的桌面入口，供保留的兼容调用点使用。</zh-CN>
        ///   <en>Normalizes the desktop entry in the current legacy form for retained compatibility call sites.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>已通过路径校验的站内相对入口。</zh-CN>
        ///   <en>Site-relative entry that passed path validation.</en>
        /// </l>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <l>
        ///   <zh-CN>路径不符合受限动态加载边界时抛出。</zh-CN>
        ///   <en>Thrown when the path does not meet the constrained dynamic-loading boundary.</en>
        /// </l>
        /// </exception>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取可选正整数查询参数。</zh-CN>
        ///   <en>Reads an optional positive integer query parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="parameterName">
        /// <l>
        ///   <zh-CN>要读取的查询参数名称。</zh-CN>
        ///   <en>The query-parameter name to read.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>读取成功后的参数值；缺失时为 0。</zh-CN>
        ///   <en>The parsed value when reading succeeds, or 0 when the parameter is missing.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>参数缺失或格式合法时为 <c>true</c>；非法时已重定向到编辑拒绝页并返回 <c>false</c>。</zh-CN>
        ///   <en><c>true</c> when the parameter is missing or valid; invalid input redirects to the edit-denied page and returns <c>false</c>.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取可选非负整数查询参数。</zh-CN>
        ///   <en>Reads an optional non-negative integer query parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="parameterName">
        /// <l>
        ///   <zh-CN>要读取的查询参数名称。</zh-CN>
        ///   <en>The query-parameter name to read.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>读取成功后的参数值；缺失时为 0。</zh-CN>
        ///   <en>The parsed value when reading succeeds, or 0 when the parameter is missing.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>参数缺失或格式合法时为 <c>true</c>；非法时已重定向到编辑拒绝页并返回 <c>false</c>。</zh-CN>
        ///   <en><c>true</c> when the parameter is missing or valid; invalid input redirects to the edit-denied page and returns <c>false</c>.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>把当前模块定义快照绑定到历史编辑表单。</zh-CN>
        ///   <en>Binds the current module-definition snapshot to the legacy edit form.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>路径字段只读且验证器停用，是为了让页面只维护显示名；路径可信边界由模块目录与部署包机制维护。</zh-CN>
        ///   <en>Path fields are read-only and the validator is disabled so the page only maintains the display name; the trusted path boundary is maintained by the module catalog and deployment-package mechanism.</en>
        /// </lang>
        /// </remarks>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>构造受控的编辑完成返回地址。</zh-CN>
        ///   <en>Builds the controlled return URL after editing.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>指向指定门户 Tab 的站内地址；缺少 Tab 时返回门户首页。</zh-CN>
        ///   <en>An in-site URL to the specified portal Tab, or the portal home page when no Tab is supplied.</en>
        /// </l>
        /// </returns>
        private string BuildPortalReturnUrl()
        {
            if (tabId > 0)
            {
                return ResolveUrl("~/DesktopDefault.aspx?tabindex=" + tabIndex + "&tabid=" + tabId);
            }

            return ResolveUrl("~/Default.aspx");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>以 HTML 编码方式显示后台低敏提示。</zh-CN>
        ///   <en>Displays a low-sensitivity administration message with HTML encoding.</en>
        /// </lang>
        /// </summary>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>要显示给管理员的提示文本。</zh-CN>
        ///   <en>The message text to display to the administrator.</en>
        /// </l>
        /// </param>
        private void ShowMessage(string message)
        {
            MessageLabel.Text = Server.HtmlEncode(message ?? string.Empty);
        }
    }
}
