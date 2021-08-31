using System;

namespace ASPNET.StarterKit.Portal
{
    public interface IDocumentItem
    {
        string FileFriendlyName { get; set; }
        string FileNameUrl { get; set; }
        string CreatedByUser { get; set; }
        DateTime? CreatedDate { get; set; }
        string Category { get; set; }
        int? ContentSize { get; set; }
        int Size { get; }
        int ModuleId { get; set; }
        int ItemId { get; set; }
    }
}