using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    public class DocumentsDb : IDocumentsDb
    {
        private readonly PortalDbContext _context;

        public DocumentsDb(PortalDbContext context)
        {
            _context = context;
        }

        #region IDocumentsDb Members

        /// <summary>
        /// 获取指定模块ID下的所有文档。
        /// </summary>
        /// <param name="moduleId">模块标识符。</param>
        /// <returns>文档集合。</returns>
        public IEnumerable<IDocumentItem> GetDocuments(int moduleId)
        {
            // 使用 LINQ 查询获取指定模块ID下的所有文档
            return _context.Documents.Where(i => i.ModuleId == moduleId).ToList<IDocumentItem>();
        }

        /// <summary>
        /// 获取单个文档。
        /// </summary>
        /// <param name="itemId">文档标识符。</param>
        /// <returns>指定ID的文档对象。</returns>
        public IDocumentItem GetSingleDocument(int itemId)
        {
            // 使用 Single 方法获取指定ID的文档
            return _context.Documents.Single(i => i.ItemId == itemId);
        }

        /// <summary>
        /// 获取文档的内容详情。
        /// </summary>
        /// <param name="itemId">文档标识符。</param>
        /// <returns>包含文档内容详情的对象。</returns>
        public IDocumentItemDetails GetDocumentContent(int itemId)
        {
            // 使用 Single 方法获取指定ID的文档内容详情
            return _context.Documents.Single(i => i.ItemId == itemId);
        }

        /// <summary>
        /// 删除指定ID的文档。
        /// </summary>
        /// <param name="itemId">文档标识符。</param>
        public void DeleteDocument(int itemId)
        {
            // 获取指定ID的文档对象
            var item = _context.Documents.Single(i => i.ItemId == itemId);
            // 从上下文中移除文档对象
            _context.Documents.Remove(item);
            // 提交更改到数据库
            _context.SaveChanges();
        }

        /// <summary>
        /// 更新指定ID的文档信息。
        /// </summary>
        /// <param name="moduleId">模块标识符。</param>
        /// <param name="itemId">文档标识符。</param>
        /// <param name="userName">用户名。</param>
        /// <param name="name">文档名称。</param>
        /// <param name="url">文件URL。</param>
        /// <param name="category">类别。</param>
        /// <param name="content">文件内容。</param>
        /// <param name="size">文件大小。</param>
        /// <param name="contentType">文件类型。</param>
        public void UpdateDocument(int moduleId, int itemId, string userName, string name, string url, string category,
                                  byte[] content, int size, string contentType)
        {
            // 如果用户名为空，则设置为 'unknown'
            userName = string.IsNullOrEmpty(userName) ? "unknown" : userName;

            DocumentItem item;
            if (itemId == 0)
            {
                item = new DocumentItem();
                _context.Documents.Add(item);
            }
            else
            {
                // 获取指定ID的文档对象
                item = _context.Documents.Single(i => i.ItemId == itemId);
            }

            // 更新文档的属性
            item.ModuleId = moduleId;
            item.CreatedByUser = userName;
            item.CreatedDate = DateTime.Now;
            item.FileFriendlyName = name;
            item.FileNameUrl = url;
            item.Category = category;
            item.Content = content;
            item.ContentSize = size;
            item.ContentType = contentType;

            // 提交更改到数据库
            _context.SaveChanges();
        }

        #endregion
    }
}
