using System;
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

        [Dependency]
        public IModuleDefsDb ModuleDefConfig { private get; set; }

        /// <summary>
        /// 页面加载时初始化模块定义信息。
        /// </summary>
        /// <param name="sender">事件源对象。</param>
        /// <param name="e">事件参数。</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // 验证当前用户是否有权访问此页面
            if (!PortalSecurity.IsInRoles("Admins"))
            {
                Response.Redirect("~/Admin/EditAccessDenied.aspx");
            }

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
                    // 新建模块定义
                    FriendlyName.Text = "New Definition";
                    DesktopSrc.Text = "DesktopModules/SomeModule.ascx";
                    MobileSrc.Text = "MobileModules/SomeModule.ascx";
                }
                else
                {
                    // 从数据库中获取要编辑的模块定义
                    IModuleDefinitionItem modDefRow = ModuleDefConfig.GetSingleModuleDefinition(defId);

                    // 加载信息
                    FriendlyName.Text = modDefRow.FriendlyName;
                    DesktopSrc.Text = modDefRow.DesktopSourceFile;
                    MobileSrc.Text = modDefRow.MobileSourceFile;
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
                    // 从当前上下文中获取 PortalSettings
                    var portalSettings = (PortalSettings)Context.Items["PortalSettings"];

                    // 向数据库中添加新的模块定义
                    ModuleDefConfig.AddModuleDefinition(FriendlyName.Text, DesktopSrc.Text, MobileSrc.Text);
                }
                else
                {
                    // 更新模块定义
                    ModuleDefConfig.UpdateModuleDefinition(defId, FriendlyName.Text, DesktopSrc.Text, MobileSrc.Text);
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
            // 删除模块定义
            ModuleDefConfig.DeleteModuleDefinition(defId);

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
    }
}