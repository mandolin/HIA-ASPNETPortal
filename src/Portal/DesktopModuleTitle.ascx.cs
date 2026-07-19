using System;
using System.Web;
using System.Web.UI;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：桌面模块标题栏控件，负责显示模块标题和可选编辑入口。
    ///
    /// English: Desktop module header control that renders a module title and optional edit action.
    /// </summary>
    /// <remarks>
    /// 中文：P7.4 起标记从旧 table 标题栏切换为语义化容器；权限判断和编辑链接生成仍沿用旧模块配置。
    ///
    /// English: Starting with P7.4, the markup changes from the legacy table title bar to semantic containers, while
    /// permission checks and edit-link generation continue to use the legacy module configuration. P8.3 further separates
    /// the title and action areas so themes can style module actions consistently.
    /// </remarks>
    public partial class DesktopModuleTitle : UserControl
    {
        /// <summary>
        /// 中文：编辑页面打开目标窗口；为空时使用浏览器默认行为。
        ///
        /// English: Target window for the edit page; when empty, the browser default behavior is used.
        /// </summary>
        public string EditTarget;

        /// <summary>
        /// 中文：当前用户可编辑模块时显示的编辑入口文本。
        ///
        /// English: Edit-action text shown when the current user can edit the module.
        /// </summary>
        public string EditText;

        /// <summary>
        /// 中文：模块编辑页面的相对 URL，不包含当前模块 ID 查询参数。
        ///
        /// English: Relative URL for the module edit page, excluding the current module-id query parameter.
        /// </summary>
        public string EditUrl;

        /// <summary>
        /// 中文：根据父模块配置写入标题，并在用户具备编辑权限时显示编辑入口。
        ///
        /// English: Writes the title from the parent module configuration and shows the edit action when the user has permission.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // 从当前HttpContext的Items集合中获取PortalSettings对象，该对象包含与当前门户相关的设置信息
            // Obtain PortalSettings from Current Context
            var portalSettings = PortalContext.GetPortalSettings();

            // 将父级控件转换为IPortalModuleControl接口类型，以便访问模块配置信息
            // Obtain reference to parent portal module
            var portalModule = (IPortalModuleControl) Parent;

            // 设置模块标题文本，显示在控件中的Label（ModuleTitle）上
            // Display Modular Title Text and Edit Buttons
            ModuleTitle.Text = portalModule.ModuleConfiguration.ModuleTitle;
            ModuleActions.Visible = false;
            EditButton.Visible = false;

            // 检查是否应显示编辑按钮
            // Display the Edit button if the parent portalmodule has configured the PortalModuleTitle User Control
            // to display it -- and the current client has edit access permissions
            // 中文：没有实际文本的编辑入口不能渲染为空按钮；否则 P7 主题下会出现无意义的空框。
            // English: Edit actions without text must stay hidden; otherwise P7 themes render meaningless empty buttons.
            if (!string.IsNullOrWhiteSpace(EditText) &&
                (portalSettings.AlwaysShowEditButton ||
                 PortalSecurity.IsInRoles(portalModule.ModuleConfiguration.AuthorizedEditRoles)))
            {
                // 如果条件满足，设置编辑按钮的文本、导航URL和目标窗口
                EditButton.Text = EditText;
                EditButton.NavigateUrl = EditUrl + "?mid=" + portalModule.ModuleId;
                EditButton.Target = EditTarget;
                EditButton.ToolTip = "Open module action: " + EditText;
                EditButton.Visible = true;
                ModuleActions.Visible = true;
            }
        }
    }
}
