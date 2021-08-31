using System.Collections.Generic;
using System.Linq;

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

        public IEnumerable<IDocumentItem> GetDocuments(int moduleId)
        {
            return _context.Documents.
                Where(i => i.ModuleId == moduleId).ToList<IDocumentItem>();
        }


        public IDocumentItem GetSingleDocument(int itemId)
        {
            return _context.Documents.Single(i => i.ItemId == itemId);
        }


        public IDocumentItemDetails GetDocumentContent(int itemId)
        {
            return _context.Documents.Single(i => i.ItemId == itemId);
        }

        public void DeleteDocument(int itemId)
        {
            DocumentItem item = _context.Documents.Single(i => i.ItemId == itemId);
            _context.Documents.Remove(item);
            _context.SaveChanges();
        }

        public void UpdateDocument(int moduleId, int itemId, string userName, string name, string url, string category,
                                   byte[] content, int size, string contentType)
        {
            if (userName.Length < 1)
            {
                userName = "unknown";
            }

            DocumentItem item = _context.Documents.Single(i => i.ItemId == itemId);

            item.ModuleId = moduleId;
            item.CreatedByUser = userName;
            item.FileFriendlyName = name;
            item.FileNameUrl = url;
            item.Category = category;
            item.Content = content;
            item.ContentSize = size;
            item.ContentType = contentType;

            _context.SaveChanges();
        }

        #endregion
    }
}