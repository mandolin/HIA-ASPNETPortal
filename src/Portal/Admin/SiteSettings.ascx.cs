using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>旧门户站点名称和编辑按钮设置控件。</zh-CN>
    ///   <en>Legacy Portal control for the site name and edit-button setting.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该控件仍服务旧门户 Admin 页，但权限、输入归一化、审计和错误诊断已接入新治理层。</zh-CN>
    ///   <en>This control still serves the legacy Portal Admin page, but permission, input normalization, audit and error diagnostics now flow through the newer governance layer.</en>
    /// </lang>
    /// </remarks>
    public partial class SiteSettings : PortalModuleControl<SiteSettings>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>门户全局设置数据访问依赖。</zh-CN>
        ///   <en>Portal global-settings data-access dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IGlobalsDb PortalConfig { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>在首次请求读取当前门户设置。</zh-CN>
        ///   <en>Reads current Portal settings on the initial request.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>读取路径使用设置查看权限；没有权限时统一授权 helper 会处理拒绝响应。</zh-CN>
            //   <en>The read path uses settings-view permission; the shared authorization helper handles the deny response when permission is missing.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.SettingsView))
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                // <lang>
                //   <zh-CN>首次加载从当前 PortalSettings 快照回填表单，避免回发时覆盖管理员刚输入的值。</zh-CN>
                //   <en>The initial load fills the form from the current PortalSettings snapshot so postback does not overwrite values just entered by the administrator.</en>
                // </lang>
                PortalSettings portalSettings = PortalContext.GetPortalSettings();
                SiteName.Text = portalSettings.PortalName;
                showEdit.Checked = portalSettings.AlwaysShowEditButton;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>校验并保存站点设置，再安全刷新当前管理页面。</zh-CN>
        ///   <en>Validates and saves site settings, then safely refreshes the current administration page.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void Apply_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>保存路径使用设置编辑权限，和只读显示分离，便于逐步拆分更细粒度系统配置权限。</zh-CN>
            //   <en>The save path uses settings-edit permission separately from read display, leaving room for finer-grained system-setting permissions later.</en>
            // </lang>
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
                // <lang>
                //   <zh-CN>旧 `IGlobalsDb` 仍负责写入门户名称和编辑按钮开关；本控件只负责输入清洗、审计和安全回跳。</zh-CN>
                //   <en>The legacy `IGlobalsDb` still writes portal name and edit-button toggle; this control handles input cleanup, audit and safe return navigation.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>异常详细内容写入诊断日志；页面提示只携带事件编号，避免把配置存储细节暴露给浏览器。</zh-CN>
                //   <en>Exception details are written to diagnostics; the page message carries only the event id to avoid exposing configuration-storage details to the browser.</en>
                // </lang>
                string eventId = PortalDiagnostics.Error(
                    "Admin.SiteSettings.Apply",
                    "Updating site settings failed.",
                    exception,
                    Context);
                ShowMessage("站点设置保存失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>显示站点设置页面级提示。</zh-CN>
        ///   <en>Displays a page-level message for site settings.</en>
        /// </lang>
        /// </summary>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>提示文本；写入控件前统一 HTML 编码。</zh-CN>
        ///   <en>Message text; HTML-encoded before being written to the control.</en>
        /// </l>
        /// </param>
        private void ShowMessage(string message)
        {
            Message.Text = Server.HtmlEncode(message ?? string.Empty);
        }
    }
}
