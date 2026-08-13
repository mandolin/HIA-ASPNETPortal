using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>以附件方式输出历史数据库文档内容的兼容页面。</zh-CN>
    ///   <en>Compatibility page that emits legacy database document content as an attachment.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>当前不再提供数据库文件上传；本页仅保留已存在内容的下载兼容。它不构成私有文件授权服务， 后续若恢复数据库存储或引入按 Tab/模块的下载授权，必须重新设计访问控制和审计边界。</zh-CN>
    ///   <en>Database file upload is currently unavailable; this page retains download compatibility for existing content only. It is not a private-file authorization service. Any restored database storage or tab/module download authorization must redesign access-control and audit boundaries.</en>
    /// </lang>
    /// </remarks>
    public partial class ViewDocument : PortalPage<ViewDocument>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>读取历史数据库文档内容的数据访问依赖。</zh-CN>
        ///   <en>Data-access dependency that reads legacy database document content.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IDocumentsDb DocumentDB { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>下载兼容页不渲染 HTML 外壳，因此不加载 App_Themes 样式。</zh-CN>
        ///   <en>The compatibility download page does not render an HTML shell, so it does not load App_Themes styles.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>依赖注入仍由 <see cref="PortalPage{T}"/> 执行；这里只跳过主题样式注入，避免无 <c>&lt;head runat="server" /&gt;</c> 页面触发 Web Forms 运行期错误。</zh-CN>
        ///   <en>Dependency injection is still performed by <see cref="PortalPage{T}"/>; this only skips stylesheet injection to avoid Web Forms runtime errors on pages without <c>&lt;head runat="server" /&gt;</c>.</en>
        /// </lang>
        /// </remarks>
        protected override bool ShouldApplyPortalTheme
        {
            get { return false; }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证文档标识并以安全附件响应输出已有数据库内容。</zh-CN>
        ///   <en>Validates the document identifier and emits existing database content in a safe attachment response.</en>
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
            //   <zh-CN>先校验正整数文档标识，再检查内容和声明长度；任何不满足条件的路径统一返回 404。</zh-CN>
            //   <en>Validate a positive document identifier first, then verify content and declared length; every invalid path returns the common 404.</en>
            // </lang>
            int documentId;
            if (!int.TryParse(Request.Params["DocumentId"], out documentId) || documentId <= 0)
            {
                WriteNotFoundResponse();
                return;
            }

            // <lang>
            //   <zh-CN>文档内容只通过数据服务读取，不从请求参数推断文件名或长度。</zh-CN>
            //   <en>Read document content only through the data service; never infer filename or length from request parameters.</en>
            // </lang>
            IDocumentItemDetails item = DocumentDB.GetDocumentContent(documentId);
            if (item == null || item.Content == null || !item.ContentSize.HasValue || item.ContentSize.Value <= 0)
            {
                WriteNotFoundResponse();
                return;
            }

            // <lang>
            //   <zh-CN>响应字节数取实际内容和数据库声明长度的较小值，限制历史异常元数据影响响应体。</zh-CN>
            //   <en>Bound the response byte count by the smaller of actual content and the database-declared length so abnormal legacy metadata cannot expand the body.</en>
            // </lang>
            int byteCount = Math.Min(item.Content.Length, item.ContentSize.Value);
            if (byteCount <= 0)
            {
                WriteNotFoundResponse();
                return;
            }

            // <lang>
            //   <zh-CN>下载文件名只来自统一策略，响应体只写入数据库中声明长度以内的字节，避免历史记录中的异常长度或文件名影响响应头。</zh-CN>
            //   <en>The download filename comes only from the shared policy, and the response writes no more than the declared byte count so abnormal legacy length or filename data cannot shape headers unexpectedly.</en>
            // </lang>
            string downloadFileName = PortalDocumentPolicy.GetSafeDownloadFileName(item.FileNameUrl);
            Response.Clear();
            Response.BufferOutput = false;
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.ContentType = "application/octet-stream";
            Response.AddHeader("Content-Disposition", "attachment; filename=\"" + downloadFileName + "\"");
            Response.AddHeader("Content-Length", byteCount.ToString());
            Response.OutputStream.Write(item.Content, 0, byteCount);
            Context.ApplicationInstance.CompleteRequest();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>结束当前附件下载请求并返回 404。</zh-CN>
        ///   <en>Completes the current attachment-download request with a 404 response.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>兼容页没有可展示 UI；找不到记录、内容为空或请求参数非法时统一返回 404，避免泄露旧文档存储细节。</zh-CN>
        ///   <en>The compatibility page has no display UI; invalid parameters, missing rows, and empty content all return 404 to avoid leaking legacy document-storage details.</en>
        /// </lang>
        /// </remarks>
        /// <summary>
        /// <lang>
        ///   <zh-CN>清空响应并以低泄露 404 完成历史附件请求。</zh-CN>
        ///   <en>Clears the response and completes the legacy attachment request with a low-disclosure 404.</en>
        /// </lang>
        /// </summary>
        private void WriteNotFoundResponse()
        {
            // <lang>
            //   <zh-CN>无论参数、记录还是内容失败都使用同一状态，避免暴露文档是否存在。</zh-CN>
            //   <en>Use the same status for parameter, record, and content failures so document existence is not disclosed.</en>
            // </lang>
            Response.Clear();
            Response.StatusCode = 404;
            Response.TrySkipIisCustomErrors = true;
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
