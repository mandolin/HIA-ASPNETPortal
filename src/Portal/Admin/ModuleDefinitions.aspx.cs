using System;
using System.Linq;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 管理模块定义的页面。
    /// </summary>
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
        /// 页面加载时初始化模块定义信息。
        /// </summary>
        /// <param name="sender">事件源对象。</param>
        /// <param name="e">事件参数。</param>
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
        /// 更新按钮点击事件处理器，用于创建或更新模块定义。
        /// </summary>
        /// <param name="sender">事件源对象。</param>
        /// <param name="e">事件参数。</param>
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
        /// 删除按钮点击事件处理器，用于删除模块定义。
        /// </summary>
        /// <param name="sender">事件源对象。</param>
        /// <param name="e">事件参数。</param>
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
        /// 取消按钮点击事件处理器，用于取消操作并返回门户首页。
        /// </summary>
        /// <param name="sender">事件源对象。</param>
        /// <param name="e">事件参数。</param>
        protected void CancelBtn_Click(Object sender, EventArgs e)
        {
            // 重定向回门户首页
            Response.Redirect("~/DesktopDefault.aspx?tabindex=" + tabIndex + "&tabid=" + tabId);
        }

        /// <summary>
        /// 服务端校验模块路径，防止动态加载入口指向站点外、上级目录或非用户控件文件。
        /// </summary>
        protected void DesktopSrcPathValidator_ServerValidate(object source, ServerValidateEventArgs args)
        {
            string normalizedSource;
            string errorMessage;
            args.IsValid = PortalModulePathValidator.TryNormalizeDesktopSource(args.Value, out normalizedSource, out errorMessage);
        }

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
