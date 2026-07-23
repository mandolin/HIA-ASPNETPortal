using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>基于 Entity Framework 的旧文档模块数据访问实现。</zh-CN>
    ///   <en>Entity Framework implementation of legacy document-module data access.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>读取单项时不存在记录会返回 <c>null</c>，由页面将编辑请求收敛到拒绝页或将下载请求收敛到中性未找到响应。 写入方法不验证 URL、上传类型或权限，这些边界由调用页面和 <c>PortalDocumentPolicy</c> 负责。</zh-CN>
    ///   <en>Reads of a missing single item return <c>null</c>, allowing pages to converge edit requests to an access-denied page and download requests to a neutral not-found response. Write operations do not validate URLs, upload types, or permission; those boundaries belong to calling pages and <c>PortalDocumentPolicy</c>.</en>
    /// </lang>
    /// </remarks>
    public class DocumentsDb : IDocumentsDb
    {
        private readonly PortalDbContext _context;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化文档实体上下文。</zh-CN>
        ///   <en>Initializes the document entity context.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>门户业务数据库上下文。</zh-CN>
        ///   <en>Portal business database context.</en>
        /// </l>
        /// </param>
        public DocumentsDb(PortalDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定模块下的全部文档记录。</zh-CN>
        ///   <en>Reads all document records for the specified module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>模块实例标识。</zh-CN>
        ///   <en>Module-instance identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>已物化的模块文档集合。</zh-CN>
        ///   <en>Materialized collection of module documents.</en>
        /// </l>
        /// </returns>
        public IEnumerable<IDocumentItem> GetDocuments(int moduleId)
        {
            return _context.Documents.Where(item => item.ModuleId == moduleId).ToList<IDocumentItem>();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按文档项目标识读取记录。</zh-CN>
        ///   <en>Reads a record by document-item identifier.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>文档项目标识。</zh-CN>
        ///   <en>Document-item identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配记录；不存在时为 <c>null</c>。</zh-CN>
        ///   <en>Matching record, or <c>null</c> when absent.</en>
        /// </l>
        /// </returns>
        public IDocumentItem GetSingleDocument(int itemId)
        {
            return _context.Documents.SingleOrDefault(item => item.ItemId == itemId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按文档项目标识读取历史数据库内容。</zh-CN>
        ///   <en>Reads legacy database content by document-item identifier.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>文档项目标识。</zh-CN>
        ///   <en>Document-item identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配详情；不存在时为 <c>null</c>。</zh-CN>
        ///   <en>Matching detail, or <c>null</c> when absent.</en>
        /// </l>
        /// </returns>
        public IDocumentItemDetails GetDocumentContent(int itemId)
        {
            return _context.Documents.SingleOrDefault(item => item.ItemId == itemId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除指定文档记录；调用方必须先完成模块归属和编辑权限校验。</zh-CN>
        ///   <en>Deletes a document record; the caller must validate module ownership and edit permission first.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>文档项目标识。</zh-CN>
        ///   <en>Document-item identifier.</en>
        /// </l>
        /// </param>
        public void DeleteDocument(int itemId)
        {
            DocumentItem item = _context.Documents.Single(record => record.ItemId == itemId);
            _context.Documents.Remove(item);
            _context.SaveChanges();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建或更新文档记录，并保留旧表的数据库内容字段以兼容已有 schema。</zh-CN>
        ///   <en>Creates or updates a document record while retaining legacy database-content fields for schema compatibility.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>所属模块实例标识。</zh-CN>
        ///   <en>Owning module-instance identifier.</en>
        /// </l>
        /// </param>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>已有项目标识；零表示新建。</zh-CN>
        ///   <en>Existing item identifier; zero creates a new record.</en>
        /// </l>
        /// </param>
        /// <param name="userName">
        /// <l>
        ///   <zh-CN>写入用户名；空值规范为 <c>unknown</c>。</zh-CN>
        ///   <en>Writing user name; blank becomes <c>unknown</c>.</en>
        /// </l>
        /// </param>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>显示名称。</zh-CN>
        ///   <en>Display name.</en>
        /// </l>
        /// </param>
        /// <param name="url">
        /// <l>
        ///   <zh-CN>调用方已验证的浏览地址或上传路径。</zh-CN>
        ///   <en>Browse address or upload path validated by the caller.</en>
        /// </l>
        /// </param>
        /// <param name="category">
        /// <l>
        ///   <zh-CN>业务分类。</zh-CN>
        ///   <en>Business category.</en>
        /// </l>
        /// </param>
        /// <param name="content">
        /// <l>
        ///   <zh-CN>历史数据库二进制内容。</zh-CN>
        ///   <en>Legacy database binary content.</en>
        /// </l>
        /// </param>
        /// <param name="size">
        /// <l>
        ///   <zh-CN>历史数据库内容大小。</zh-CN>
        ///   <en>Legacy database content size.</en>
        /// </l>
        /// </param>
        /// <param name="contentType">
        /// <l>
        ///   <zh-CN>历史 MIME 类型提示。</zh-CN>
        ///   <en>Legacy MIME-type hint.</en>
        /// </l>
        /// </param>
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
