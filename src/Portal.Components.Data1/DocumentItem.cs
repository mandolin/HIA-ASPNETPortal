using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    [Table("Portal_Documents")]
    public class DocumentItem : IDocumentItemDetails
    {
        #region IDocumentItemDetails Members

        public string FileFriendlyName { get; set; }
        public string FileNameUrl { get; set; }
        public string CreatedByUser { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string Category { get; set; }
        public int? ContentSize { get; set; }

        public int Size
        {
            get
            {
                if (ContentSize == null)
                {
                    return 0;
                }
                return ContentSize.Value;
            }
        }

        public int ModuleId { get; set; }

        [Key]
        public int ItemId { get; set; }

        public byte[] Content { get; set; }
        public string ContentType { get; set; }

        #endregion
    }
}