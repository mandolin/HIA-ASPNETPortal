using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>显示当前模块已登记文档链接的文档模块控件。</zh-CN>
    ///   <en>Document-module control that displays registered document links for the current module.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>服务器上传文件继续通过受限静态目录访问；数据库内容大于零的历史记录通过 <c>ViewDocument.aspx</c> 以附件方式下载。手填旧链接在渲染前会经过 <see cref="PortalNavigationPolicy"/> 校验，非法协议或路径跳转不会生成可点击地址。</zh-CN>
    ///   <en>Server-uploaded files continue through a restricted static directory; legacy records with database content download through <c>ViewDocument.aspx</c> as attachments. Manually entered legacy links are validated by <see cref="PortalNavigationPolicy"/> before rendering, so invalid schemes or traversal paths do not produce a clickable address.</en>
    /// </lang>
    /// </remarks>
    public partial class Document : PortalModuleControl<Document>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>按模块读取文档项目的数据访问依赖。</zh-CN>
        ///   <en>Data-access dependency that reads document items by module.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IDocumentsDb DocumentDB { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前模块的文档记录并绑定列表。</zh-CN>
        ///   <en>Reads document records for the current module and binds the list.</en>
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
            myDataGrid.DataSource = DocumentDB.GetDocuments(ModuleId);
            myDataGrid.DataBind();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>为文档列表生成经验证的浏览地址。</zh-CN>
        ///   <en>Produces a validated browse URL for the document list.</en>
        /// </lang>
        /// </summary>
        /// <param name="url">
        /// <l>
        ///   <zh-CN>旧记录中的链接地址。</zh-CN>
        ///   <en>Link address stored by a legacy record.</en>
        /// </l>
        /// </param>
        /// <param name="size">
        /// <l>
        ///   <zh-CN>数据库内容大小；大于零表示走历史数据库下载入口。</zh-CN>
        ///   <en>Database-content size; a positive value selects the legacy database download endpoint.</en>
        /// </l>
        /// </param>
        /// <param name="documentId">
        /// <l>
        ///   <zh-CN>文档项目标识。</zh-CN>
        ///   <en>Document-item identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>允许的浏览 URL；旧链接非法时为空。</zh-CN>
        ///   <en>Allowed browse URL, or empty when a legacy link is invalid.</en>
        /// </l>
        /// </returns>
        protected string GetBrowsePath(string url, object size, int documentId)
        {
            if (size != DBNull.Value && Convert.ToInt32(size) > 0)
            {
                return "~/DesktopModules/ViewDocument.aspx?DocumentID=" + documentId;
            }

            string normalizedUrl;
            return PortalNavigationPolicy.TryNormalizeBrowseUrl(url, Context.Request, out normalizedUrl)
                ? normalizedUrl
                : string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将数据库记录中的显示字段编码为安全 HTML 文本。</zh-CN>
        ///   <en>Encodes a display field from a database record as safe HTML text.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>绑定记录中的原始字段值。</zh-CN>
        ///   <en>Raw field value from the bound record.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>已 HTML 编码的显示文本。</zh-CN>
        ///   <en>HTML-encoded display text.</en>
        /// </l>
        /// </returns>
        protected string EncodeText(object value)
        {
            return Server.HtmlEncode(value == null ? string.Empty : Convert.ToString(value));
        }
    }
}
