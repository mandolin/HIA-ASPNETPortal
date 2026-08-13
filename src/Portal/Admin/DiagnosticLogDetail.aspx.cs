using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>已净化诊断日志详情页面。</zh-CN>
    ///   <en>Sanitized diagnostic-log detail page.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该页面只允许具备诊断详情权限的管理员进入，并且还受部署级明细开关控制。页面展示的是 <see cref="PortalDiagnostics"/> 已写入的结构化事件，不重新读取任意文件路径。</zh-CN>
    ///   <en>This page only allows administrators with diagnostics-detail permission and is additionally guarded by the deployment-level detail switch. It displays structured events already written by <see cref="PortalDiagnostics"/> and does not read arbitrary file paths.</en>
    /// </lang>
    /// </remarks>
    public partial class DiagnosticLogDetail : PortalPage<DiagnosticLogDetail>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>加载受控管理员诊断详情。</zh-CN>
        ///   <en>Loads controlled administrator diagnostic detail.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发页面加载的 Web Forms 事件源。</zh-CN>
        ///   <en>The Web Forms event source that triggered page loading.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>页面加载事件参数。</zh-CN>
        ///   <en>The page-load event arguments.</en>
        /// </l>
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>详情页在读取查询字符串前复核明细权限，拒绝时不探测配置、不查询事件。</zh-CN>
            //   <en>Recheck detail permission before reading the query string so rejection probes neither configuration nor events.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.OpsDiagnosticsDetail))
            {
                return;
            }

            // <lang>
            //   <zh-CN>部署级开关是第二道门禁；关闭时给出固定提示，不泄露日志服务或文件路径状态。</zh-CN>
            //   <en>The deployment-level switch is the second gate; when disabled, show a fixed message without exposing log-service or file-path state.</en>
            // </lang>
            if (!PortalDiagnostics.AreAdminLogDetailsEnabled())
            {
                MessageLabel.Text = "Diagnostic detail viewing is disabled by deployment configuration.";
                return;
            }

            // <lang>
            //   <zh-CN>只接受事件编号查询结构化日志，不把请求参数解释为物理路径或任意文件名。</zh-CN>
            //   <en>Use the request value only as an event-id lookup into structured logs, never as a physical path or arbitrary file name.</en>
            // </lang>
            string eventId = Request.QueryString["id"];
            PortalDiagnosticEntry entry = PortalDiagnosticQueryService.FindByEventId(eventId);
            if (entry == null)
            {
                // <lang>
                //   <zh-CN>未知或已清理事件统一落到固定未找到提示，避免回显查询值或底层异常。</zh-CN>
                //   <en>Unknown or purged events use a fixed not-found message, avoiding reflection of the query value or backend exception.</en>
                // </lang>
                MessageLabel.Text = "The requested diagnostic event was not found in structured logs.";
                return;
            }

            // <lang>
            //   <zh-CN>只有服务返回完整事件后才显示详情面板；字段投影继续由下方编码 helper 统一约束。</zh-CN>
            //   <en>Show the detail panel only after the service returns a complete event; field projection remains constrained by the encoding helper below.</en>
            // </lang>
            DetailPanel.Visible = true;
            /*
             * <lang>
             *   <zh-CN>诊断字段可能包含请求、异常或环境派生文本；Label 输出前统一 HTML 编码，异常明细交给 TextBox 控件按文本值呈现。</zh-CN>
             *   <en>Diagnostic fields may contain request-, exception-, or environment-derived text; Label values are HTML encoded before output, while exception details are assigned to the TextBox as text.</en>
             * </lang>
             */
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
        /// <lang>
        ///   <zh-CN>将受控诊断文本编码为可安全写入 <see cref="System.Web.UI.WebControls.Label"/> 的 HTML。</zh-CN>
        ///   <en>Encodes controlled diagnostic text for safe HTML output through a <see cref="System.Web.UI.WebControls.Label"/>.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>待显示的诊断值。</zh-CN>
        ///   <en>Diagnostic value to display.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>已 HTML 编码的文本。</zh-CN>
        ///   <en>HTML-encoded text.</en>
        /// </l>
        /// </returns>
        private string EncodeForLabel(string value)
        {
            return Server.HtmlEncode(value ?? string.Empty);
        }
    }
}
