using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：显示当前模块已登记文档链接的文档模块控件。
    ///
    /// English: Document-module control that displays registered document links for the current module.
    /// </summary>
    /// <remarks>
    /// 中文：服务器上传文件继续通过受限静态目录访问；数据库内容大于零的历史记录通过
    /// <c>ViewDocument.aspx</c> 以附件方式下载。手填旧链接在渲染前会经过
    /// <see cref="PortalDocumentPolicy"/> 校验，非法协议或路径跳转不会生成可点击地址。
    ///
    /// English: Server-uploaded files continue through a restricted static directory; legacy records with database content
    /// download through <c>ViewDocument.aspx</c> as attachments. Manually entered legacy links are validated by
    /// <see cref="PortalDocumentPolicy"/> before rendering, so invalid schemes or traversal paths do not produce a clickable address.
    /// </remarks>
    public partial class Document : PortalModuleControl<Document>
    {
        /// <summary>
        /// 中文：按模块读取文档项目的数据访问依赖。
        ///
        /// English: Data-access dependency that reads document items by module.
        /// </summary>
        [Dependency]
        public IDocumentsDb DocumentDB { private get; set; }

        /// <summary>
        /// 中文：读取当前模块的文档记录并绑定列表。
        ///
        /// English: Reads document records for the current module and binds the list.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            myDataGrid.DataSource = DocumentDB.GetDocuments(ModuleId);
            myDataGrid.DataBind();
        }

        /// <summary>
        /// 中文：为文档列表生成经验证的浏览地址。
        ///
        /// English: Produces a validated browse URL for the document list.
        /// </summary>
        /// <param name="url">中文：旧记录中的链接地址。English: Link address stored by a legacy record.</param>
        /// <param name="size">中文：数据库内容大小；大于零表示走历史数据库下载入口。English: Database-content size; a positive value selects the legacy database download endpoint.</param>
        /// <param name="documentId">中文：文档项目标识。English: Document-item identifier.</param>
        /// <returns>中文：允许的浏览 URL；旧链接非法时为空。English: Allowed browse URL, or empty when a legacy link is invalid.</returns>
        protected string GetBrowsePath(string url, object size, int documentId)
        {
            if (size != DBNull.Value && Convert.ToInt32(size) > 0)
            {
                return "~/DesktopModules/ViewDocument.aspx?DocumentID=" + documentId;
            }

            string normalizedUrl;
            return PortalDocumentPolicy.TryNormalizeBrowseUrl(url, Context.Request, out normalizedUrl)
                ? normalizedUrl
                : string.Empty;
        }

        /// <summary>
        /// 中文：将数据库记录中的显示字段编码为安全 HTML 文本。
        ///
        /// English: Encodes a display field from a database record as safe HTML text.
        /// </summary>
        /// <param name="value">中文：绑定记录中的原始字段值。English: Raw field value from the bound record.</param>
        /// <returns>中文：已 HTML 编码的显示文本。English: HTML-encoded display text.</returns>
        protected string EncodeText(object value)
        {
            return Server.HtmlEncode(value == null ? string.Empty : Convert.ToString(value));
        }
    }
}
