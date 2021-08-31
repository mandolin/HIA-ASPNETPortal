using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    public interface IDocumentsDb
    {
        IEnumerable<IDocumentItem> GetDocuments(int moduleId);
        IDocumentItem GetSingleDocument(int itemId);
        IDocumentItemDetails GetDocumentContent(int itemId);
        void DeleteDocument(int itemId);

        void UpdateDocument(int moduleId, int itemId, string userName, string name, string url, string category,
                            byte[] content, int size, string contentType);
    }
}