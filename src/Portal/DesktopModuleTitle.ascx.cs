using System;
using System.Web;
using System.Web.UI;

namespace ASPNET.StarterKit.Portal
{
    public partial class DesktopModuleTitle : UserControl
    {
        // 定义三个公共属性，分别用于存储编辑目标窗口、编辑文本以及编辑URL
        public string EditTarget;
        public string EditText;
        public string EditUrl;

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

            // 检查是否应显示编辑按钮
            // Display the Edit button if the parent portalmodule has configured the PortalModuleTitle User Control
            // to display it -- and the current client has edit access permissions
            if (portalSettings.AlwaysShowEditButton ||
                (PortalSecurity.IsInRoles(portalModule.ModuleConfiguration.AuthorizedEditRoles)) && (EditText != null))
            {
                // 如果条件满足，设置编辑按钮的文本、导航URL和目标窗口
                EditButton.Text = EditText;
                EditButton.NavigateUrl = EditUrl + "?mid=" + portalModule.ModuleId;
                EditButton.Target = EditTarget;
            }
        }
    }
}
