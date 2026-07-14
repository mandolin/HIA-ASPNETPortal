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
            // 中文：诊断字段可能包含请求或异常派生文本，标签输出前统一 HTML 编码。
            // English: Diagnostic fields can contain request- or exception-derived text, so labels are HTML encoded before output.
            EventIdLabel.Text = EncodeForLabel(entry.EventId);
            UtcTimeLabel.Text = EncodeForLabel(entry.UtcTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"));
            LevelLabel.Text = EncodeForLabel(entry.Level);
            CategoryLabel.Text = EncodeForLabel(entry.Category);
            MessageTextLabel.Text = EncodeForLabel(entry.Message);
            ExceptionTypeLabel.Text = EncodeForLabel(entry.ExceptionType);
            ExceptionDetailTextBox.Text = entry.ExceptionDetail;
            RequestPathLabel.Text = EncodeForLabel(entry.RequestPath);
            HttpMethodLabel.Text = EncodeForLabel(entry.HttpMethod);
            UserNameLabel.Text = EncodeForLabel(entry.UserName);
            ClientIpLabel.Text = EncodeForLabel(entry.ClientIp);
            PhysicalPathLabel.Text = EncodeForLabel(entry.PhysicalPath);
            UserAgentLabel.Text = EncodeForLabel(entry.UserAgent);
        }

        /// <summary>
        /// 中文：将受控诊断文本编码为可安全写入 <see cref="System.Web.UI.WebControls.Label"/> 的 HTML。
        ///
        /// English: Encodes controlled diagnostic text for safe HTML output through a <see cref="System.Web.UI.WebControls.Label"/>.
        /// </summary>
        /// <param name="value">中文：待显示的诊断值。English: Diagnostic value to display.</param>
        /// <returns>中文：已 HTML 编码的文本。English: HTML-encoded text.</returns>
        private string EncodeForLabel(string value)
        {
            return Server.HtmlEncode(value ?? string.Empty);
        }
    }
}
