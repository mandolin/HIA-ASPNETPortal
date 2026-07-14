using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：基于 Entity Framework 的旧文档模块数据访问实现。
    ///
    /// English: Entity Framework implementation of legacy document-module data access.
    /// </summary>
    /// <remarks>
    /// 中文：读取单项时不存在记录会返回 <c>null</c>，由页面将编辑请求收敛到拒绝页或将下载请求收敛到中性未找到响应。
    /// 写入方法不验证 URL、上传类型或权限，这些边界由调用页面和 <c>PortalDocumentPolicy</c> 负责。
    ///
    /// English: Reads of a missing single item return <c>null</c>, allowing pages to converge edit requests to an access-denied
    /// page and download requests to a neutral not-found response. Write operations do not validate URLs, upload types, or
    /// permission; those boundaries belong to calling pages and <c>PortalDocumentPolicy</c>.
    /// </remarks>
    public class DocumentsDb : IDocumentsDb
    {
        private readonly PortalDbContext _context;

        /// <summary>
        /// 中文：初始化文档实体上下文。
        /// English: Initializes the document entity context.
        /// </summary>
        /// <param name="context">中文：门户业务数据库上下文。English: Portal business database context.</param>
        public DocumentsDb(PortalDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 中文：读取指定模块下的全部文档记录。
        /// English: Reads all document records for the specified module.
        /// </summary>
        /// <param name="moduleId">中文：模块实例标识。English: Module-instance identifier.</param>
        /// <returns>中文：已物化的模块文档集合。English: Materialized collection of module documents.</returns>
        public IEnumerable<IDocumentItem> GetDocuments(int moduleId)
        {
            return _context.Documents.Where(item => item.ModuleId == moduleId).ToList<IDocumentItem>();
        }

        /// <summary>
        /// 中文：按文档项目标识读取记录。
        /// English: Reads a record by document-item identifier.
        /// </summary>
        /// <param name="itemId">中文：文档项目标识。English: Document-item identifier.</param>
        /// <returns>中文：匹配记录；不存在时为 <c>null</c>。English: Matching record, or <c>null</c> when absent.</returns>
        public IDocumentItem GetSingleDocument(int itemId)
        {
            return _context.Documents.SingleOrDefault(item => item.ItemId == itemId);
        }

        /// <summary>
        /// 中文：按文档项目标识读取历史数据库内容。
        /// English: Reads legacy database content by document-item identifier.
        /// </summary>
        /// <param name="itemId">中文：文档项目标识。English: Document-item identifier.</param>
        /// <returns>中文：匹配详情；不存在时为 <c>null</c>。English: Matching detail, or <c>null</c> when absent.</returns>
        public IDocumentItemDetails GetDocumentContent(int itemId)
        {
            return _context.Documents.SingleOrDefault(item => item.ItemId == itemId);
        }

        /// <summary>
        /// 中文：删除指定文档记录；调用方必须先完成模块归属和编辑权限校验。
        /// English: Deletes a document record; the caller must validate module ownership and edit permission first.
        /// </summary>
        /// <param name="itemId">中文：文档项目标识。English: Document-item identifier.</param>
        public void DeleteDocument(int itemId)
        {
            DocumentItem item = _context.Documents.Single(record => record.ItemId == itemId);
            _context.Documents.Remove(item);
            _context.SaveChanges();
        }

        /// <summary>
        /// 中文：创建或更新文档记录，并保留旧表的数据库内容字段以兼容已有 schema。
        /// English: Creates or updates a document record while retaining legacy database-content fields for schema compatibility.
        /// </summary>
        /// <param name="moduleId">中文：所属模块实例标识。English: Owning module-instance identifier.</param>
        /// <param name="itemId">中文：已有项目标识；零表示新建。English: Existing item identifier; zero creates a new record.</param>
        /// <param name="userName">中文：写入用户名；空值规范为 <c>unknown</c>。English: Writing user name; blank becomes <c>unknown</c>.</param>
        /// <param name="name">中文：显示名称。English: Display name.</param>
        /// <param name="url">中文：调用方已验证的浏览地址或上传路径。English: Browse address or upload path validated by the caller.</param>
        /// <param name="category">中文：业务分类。English: Business category.</param>
        /// <param name="content">中文：历史数据库二进制内容。English: Legacy database binary content.</param>
        /// <param name="size">中文：历史数据库内容大小。English: Legacy database content size.</param>
        /// <param name="contentType">中文：历史 MIME 类型提示。English: Legacy MIME-type hint.</param>
        public void UpdateDocument(int moduleId, int itemId, string userName, string name, string url, string category,
                                   byte[] content, int size, string contentType)
        {
            userName = string.IsNullOrEmpty(userName) ? "unknown" : userName;

            DocumentItem item;
            if (itemId == 0)
            {
                item = new DocumentItem();
                _context.Documents.Add(item);
            }
            else
            {
                item = _context.Documents.Single(record => record.ItemId == itemId);
            }

            item.ModuleId = moduleId;
            item.CreatedByUser = userName;
            item.CreatedDate = DateTime.Now;
            item.FileFriendlyName = name;
            item.FileNameUrl = url;
            item.Category = category;
            item.Content = content;
            item.ContentSize = size;
            item.ContentType = contentType;
            _context.SaveChanges();
        }
    }
}
