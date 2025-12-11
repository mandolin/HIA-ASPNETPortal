using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 文档模块控件，用于显示文档列表。
    /// </summary>
    public partial class Document : PortalModuleControl<Document>
    {
        [Dependency]
        public IDocumentsDb DocumentDB { private get; set; }

        /// <summary>
        /// 用户控件的页面加载事件处理器，用于从数据库获取文档信息并绑定到 DataGrid 控件。
        /// </summary>
        /// <param name="sender">事件源对象。</param>
        /// <param name="e">事件参数。</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // 从数据库获取指定模块的文档数据
            myDataGrid.DataSource = DocumentDB.GetDocuments(ModuleId);

            // 将数据绑定到 DataGrid 控件
            myDataGrid.DataBind();
        }

        /// <summary>
        /// 获取浏览路径的辅助方法。如果数据库中存储的内容大小非零，则创建指向该内容的 URL；
        /// 否则，返回 FileNameUrl 的值。
        /// </summary>
        /// <param name="url">文件的 URL。</param>
        /// <param name="size">文件大小。</param>
        /// <param name="documentId">文档的 ID。</param>
        /// <returns>指向文档的 URL。</returns>
        protected string GetBrowsePath(string url, object size, int documentId)
        {
            // 检查文件大小是否为非零值
            if (size != DBNull.Value && Convert.ToInt32(size) > 0)
            {
                // 如果数据库中有内容，则创建一个指向该内容的 URL
                return "~/DesktopModules/ViewDocument.aspx?DocumentID=" + documentId;
            }
            else
            {
                // 否则，返回 FileNameUrl 的值
                return url;
            }
        }
    }
}