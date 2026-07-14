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
        private int defId = -1;
        private int tabId;
        private int tabIndex;

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
        /// <remarks>
        /// 查询参数沿用历史 <see cref="int.Parse(string)"/> 行为；非整数值会按现有全局异常流程处理。本批只说明该风险，
        /// 不改变 URL 兼容性或错误页行为。
        /// Query parameters retain historical <see cref="int.Parse(string)"/> behavior; non-integer values flow through
        /// the existing global exception handling. This batch documents the risk without changing URL compatibility or
        /// error-page behavior.
        /// </remarks>
        protected void Page_Load(object sender, EventArgs e)
        {
            // 验证当前用户是否有权访问此页面
            PortalAuthorization.RequireAdmin();

            // 计算模块定义 ID
            if (Request.Params["defid"] != null)
            {
                defId = int.Parse(Request.Params["defid"]);
            }
            if (Request.Params["tabid"] != null)
            {
                tabId = int.Parse(Request.Params["tabid"]);
            }
            if (Request.Params["tabindex"] != null)
            {
                tabIndex = int.Parse(Request.Params["tabindex"]);
            }

            // 如果这是第一次访问页面，则绑定模块定义数据
            if (!Page.IsPostBack)
            {
                if (defId == -1)
                {
                    // 新模块定义由受信任部署目录完成注册，避免继续引入任意路径入口。
                    // New module definitions are registered through the trusted deployment catalog to avoid new
                    // arbitrary-path entry points.
                    Response.Redirect("~/Admin/ModuleCatalog.aspx");
                    return;
                }
                else
                {
                    // 从数据库中获取要编辑的模块定义
                    IModuleDefinitionItem modDefRow = ModuleDefConfig.GetSingleModuleDefinition(defId);

                    // 加载信息
                    FriendlyName.Text = modDefRow.FriendlyName;
                    DesktopSrc.Text = modDefRow.DesktopSourceFile;
                    MobileSrc.Text = modDefRow.MobileSourceFile;

                    // 旧定义页只保留历史名称维护和安全删除检查；路径变更统一迁移到部署包目录。
                    // The legacy definition page retains historical name maintenance and safe-delete checks only;
                    // path changes move to the deployment-package catalog.
                    DesktopSrc.ReadOnly = true;
                    MobileSrc.ReadOnly = true;
                    Req2.Enabled = false;
                    DesktopSrcPathValidator.Enabled = false;
                }
            }
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
            if (Page.IsValid)
            {
                if (defId == -1)
                {
                    Response.Redirect("~/Admin/ModuleCatalog.aspx");
                    return;
                }
                else
                {
                    // 保留既有受控路径，避免通过 legacy 表单创建或变更任意动态加载入口。
                    // Preserve the existing controlled path, preventing arbitrary dynamic-load entries from being
                    // created or changed through the legacy form.
                    IModuleDefinitionItem current = ModuleDefConfig.GetSingleModuleDefinition(defId);
                    ModuleDefConfig.UpdateModuleDefinition(
                        defId,
                        FriendlyName.Text,
                        current.DesktopSourceFile,
                        current.MobileSourceFile);
                    PortalOperationAudit.Record(
                        "ModuleDefinition",
                        "UpdateName",
                        "ModuleDefinition",
                        defId.ToString(),
                        "Updated the legacy module-definition display name.",
                        Context);
                }

                // 重定向回门户管理页面
                Response.Redirect("~/DesktopDefault.aspx?tabindex=" + tabIndex + "&tabid=" + tabId);
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
            int instanceCount = ModulesConfig.GetModulesByModuleDefId(defId).Count();
            if (instanceCount > 0)
            {
                // 旧删除会级联清除业务模块数据。P3.2 要求先禁用、迁移或显式清理实例。
                // Legacy deletion cascades into business module data. P3.2 requires disabling, migration, or
                // explicit instance cleanup first.
                MessageLabel.Text = "This module definition is still used by " + instanceCount +
                                    " module instance(s). Disable or migrate it, then explicitly clean instances before deletion.";
                return;
            }

            // 删除模块定义
            ModuleDefConfig.DeleteModuleDefinition(defId);
            PortalOperationAudit.Record(
                "ModuleDefinition",
                "Delete",
                "ModuleDefinition",
                defId.ToString(),
                "Deleted an unused legacy module definition.",
                Context);

            // 重定向回门户管理页面
            Response.Redirect("~/DesktopDefault.aspx?tabindex=" + tabIndex + "&tabid=" + tabId);
        }

        /// <summary>
        /// 取消当前编辑并返回指定门户 Tab。
        /// Cancels the current edit and returns to the specified portal Tab.
        /// </summary>
        /// <param name="sender">事件源。Event source.</param>
        /// <param name="e">事件参数。Event arguments.</param>
        protected void CancelBtn_Click(Object sender, EventArgs e)
        {
            // 重定向回门户首页
            Response.Redirect("~/DesktopDefault.aspx?tabindex=" + tabIndex + "&tabid=" + tabId);
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
    }
}
