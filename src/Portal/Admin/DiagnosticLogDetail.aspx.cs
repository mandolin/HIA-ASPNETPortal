using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 已净化诊断日志详情页面。
    /// Sanitized diagnostic-log detail page.
    /// </summary>
    public partial class DiagnosticLogDetail : PortalPage<DiagnosticLogDetail>
    {
        /// <summary>
        /// 加载受控管理员诊断详情。
        /// Loads controlled administrator diagnostic detail.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            PortalAuthorization.RequireAdmin();

            if (!PortalDiagnostics.AreAdminLogDetailsEnabled())
            {
                MessageLabel.Text = "Diagnostic detail viewing is disabled by deployment configuration.";
                return;
            }

            string eventId = Request.QueryString["id"];
            PortalDiagnosticEntry entry = PortalDiagnosticQueryService.FindByEventId(eventId);
            if (entry == null)
            {
                MessageLabel.Text = "The requested diagnostic event was not found in structured logs.";
                return;
            }

            DetailPanel.Visible = true;
            EventIdLabel.Text = entry.EventId;
            UtcTimeLabel.Text = entry.UtcTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
            LevelLabel.Text = entry.Level;
            CategoryLabel.Text = entry.Category;
            MessageTextLabel.Text = entry.Message;
            ExceptionTypeLabel.Text = entry.ExceptionType;
            ExceptionDetailTextBox.Text = entry.ExceptionDetail;
            RequestPathLabel.Text = entry.RequestPath;
            HttpMethodLabel.Text = entry.HttpMethod;
            UserNameLabel.Text = entry.UserName;
            ClientIpLabel.Text = entry.ClientIp;
            PhysicalPathLabel.Text = entry.PhysicalPath;
            UserAgentLabel.Text = entry.UserAgent;
        }
    }
}
