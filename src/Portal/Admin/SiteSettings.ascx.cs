using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：旧门户站点名称和编辑按钮设置控件。
    ///
    /// English: Legacy Portal control for the site name and edit-button setting.
    /// </summary>
    public partial class SiteSettings : PortalModuleControl<SiteSettings>
    {
        /// <summary>
        /// 中文：门户全局设置数据访问依赖。
        ///
        /// English: Portal global-settings data-access dependency.
        /// </summary>
        [Dependency]
        public IGlobalsDb PortalConfig { private get; set; }

        /// <summary>
        /// 中文：在首次请求读取当前门户设置。
        ///
        /// English: Reads current Portal settings on the initial request.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.SettingsView))
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                PortalSettings portalSettings = PortalContext.GetPortalSettings();
                SiteName.Text = portalSettings.PortalName;
                showEdit.Checked = portalSettings.AlwaysShowEditButton;
            }
        }

        /// <summary>
        /// 中文：校验并保存站点设置，再安全刷新当前管理页面。
        ///
        /// English: Validates and saves site settings, then safely refreshes the current administration page.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void Apply_Click(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.SettingsEdit))
            {
                return;
            }

            string portalName;
            if (!PortalAdministrationPolicy.TryNormalizeRequiredSingleLineText(SiteName.Text, 150, out portalName))
            {
                ShowMessage("站点名称无效，未保存本次修改。");
                return;
            }

            try
            {
                PortalSettings portalSettings = PortalContext.GetPortalSettings();
                PortalConfig.UpdatePortalInfo(portalSettings.PortalId, portalName, showEdit.Checked);
                PortalOperationAudit.Record(
                    "PortalAdministration",
                    "UpdateSiteSettings",
                    "Portal",
                    portalSettings.PortalId.ToString(),
                    "Updated site settings.",
                    Context);
                PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, Request.RawUrl);
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.SiteSettings.Apply",
                    "Updating site settings failed.",
                    exception,
                    Context);
                ShowMessage("站点设置保存失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        private void ShowMessage(string message)
        {
            Message.Text = Server.HtmlEncode(message ?? string.Empty);
        }
    }
}
