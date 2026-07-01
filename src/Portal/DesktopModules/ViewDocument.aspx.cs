using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 页面用于查看文档内容。
    /// </summary>
    public partial class ViewDocument : PortalPage<ViewDocument>
    {
        private int documentId = -1;

        [Dependency]
        public IDocumentsDb DocumentDB { private get; set; }

        /// <summary>
        /// 页面加载事件处理器，用于从数据库获取文档内容，并将其发送到客户端。
        /// </summary>
        /// <param name="sender">事件源对象。</param>
        /// <param name="e">事件参数。</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // 获取请求参数中的文档 ID
            if (Request.Params["DocumentId"] != null)
            {
                documentId = int.Parse(Request.Params["DocumentId"]);
            }

            // 如果文档 ID 不为 -1，则继续处理
            if (documentId != -1)
            {
                // 从数据库中获取文档内容
                IDocumentItemDetails item = DocumentDB.GetDocumentContent(documentId);

                // 设置响应头，指定文件名
                Response.AppendHeader("content-disposition", $"filename={item.FileNameUrl}");

                // 设置响应的内容类型
                Response.ContentType = item.ContentType;

                // 将文档内容写入响应输出流
                Response.OutputStream.Write(item.Content, 0, item.ContentSize.Value);

                // 结束响应
                Response.End();
            }
        }
    }
}