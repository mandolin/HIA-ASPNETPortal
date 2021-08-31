using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    [Table("Portal_Links")]
    public class LinkItem : ILinkItem
    {
        #region ILinkItem Members

        [Key]
        public int ItemId { get; set; }

        public int ModuleId { get; set; }
        public string CreatedByUser { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string MobileUrl { get; set; }
        public int? ViewOrder { get; set; }
        public string Description { get; set; }

        #endregion
    }
}