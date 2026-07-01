using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class SiteSettings : PortalModuleControl<SiteSettings>
    {
        // Dependency Injection: A property for accessing the global database configuration.
        [Dependency]
        public IGlobalsDb PortalConfig { private get; set; }

        //*******************************************************
        //
        // The Page_Load server event handler on this user control is used
        // to populate the current site settings from the config system
        //
        //*******************************************************

        protected void Page_Load(object sender, EventArgs e)
        {
            // Verify that the current user has access to access this page
            // 验证当前用户是否有权限访问此页面。
            if (PortalSecurity.IsInRoles("Admins") == false)
            {
                // If not, redirect to an access denied page
                // 如果没有权限，则重定向到拒绝访问页面。
                Response.Redirect("~/Admin/EditAccessDenied.aspx");
            }

            // If this is the first visit to the page, populate the site data
            // 如果这是第一次访问页面，则填充站点数据。
            if (Page.IsPostBack == false)
            {
                // Obtain PortalSettings from Current Context
                // 从当前上下文获取PortalSettings。
                var portalSettings = (PortalSettings)Context.Items["PortalSettings"];

                // Set the text box with the current site name
                // 设置文本框以显示当前站点名称。
                SiteName.Text = portalSettings.PortalName;

                // Set the checkbox state according to whether the edit button should always be shown
                // 根据是否应始终显示编辑按钮来设置复选框状态。
                showEdit.Checked = portalSettings.AlwaysShowEditButton;
            }
        }

        //*******************************************************
        //
        // The Apply_Click server event handler is used
        // to update the Site Name within the Portal Config System
        //
        //*******************************************************

        protected void Apply_Click(Object sender, EventArgs e)
        {
            // Obtain PortalSettings from Current Context
            // 从当前上下文获取PortalSettings。
            var portalSettings = (PortalSettings)Context.Items["PortalSettings"];

            // Update the portal information in the database
            // 在数据库中更新门户信息。
            PortalConfig.UpdatePortalInfo(portalSettings.PortalId, SiteName.Text, showEdit.Checked);

            // Redirect to this site to refresh
            // 重新定向到此站点以刷新页面。
            Response.Redirect(Request.RawUrl);
        }
    }
}