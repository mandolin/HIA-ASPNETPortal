using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：以附件方式输出历史数据库文档内容的兼容页面。
    ///
    /// English: Compatibility page that emits legacy database document content as an attachment.
    /// </summary>
    /// <remarks>
    /// 中文：当前不再提供数据库文件上传；本页仅保留已存在内容的下载兼容。它不构成私有文件授权服务，
    /// 后续若恢复数据库存储或引入按 Tab/模块的下载授权，必须重新设计访问控制和审计边界。
    ///
    /// English: Database file upload is currently unavailable; this page retains download compatibility for existing content only.
    /// It is not a private-file authorization service. Any restored database storage or tab/module download authorization
    /// must redesign access-control and audit boundaries.
    /// </remarks>
    public partial class ViewDocument : PortalPage<ViewDocument>
    {
        /// <summary>
        /// 中文：读取历史数据库文档内容的数据访问依赖。
        ///
        /// English: Data-access dependency that reads legacy database document content.
        /// </summary>
        [Dependency]
        public IDocumentsDb DocumentDB { private get; set; }

        /// <summary>
        /// 中文：验证文档标识并以安全附件响应输出已有数据库内容。
        ///
        /// English: Validates the document identifier and emits existing database content in a safe attachment response.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            int documentId;
            if (!int.TryParse(Request.Params["DocumentId"], out documentId) || documentId <= 0)
            {
                WriteNotFoundResponse();
                return;
            }

            IDocumentItemDetails item = DocumentDB.GetDocumentContent(documentId);
            if (item == null || item.Content == null || !item.ContentSize.HasValue || item.ContentSize.Value <= 0)
            {
                WriteNotFoundResponse();
                return;
            }

            int byteCount = Math.Min(item.Content.Length, item.ContentSize.Value);
            if (byteCount <= 0)
            {
                WriteNotFoundResponse();
                return;
            }

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

        private void WriteNotFoundResponse()
        {
            Response.Clear();
            Response.StatusCode = 404;
            Response.TrySkipIisCustomErrors = true;
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
