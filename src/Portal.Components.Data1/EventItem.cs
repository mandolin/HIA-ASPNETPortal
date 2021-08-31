using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    [Table("Portal_Events")]
    public class EventItem : IEventItem
    {
        #region IEventItem Members

        [Key]
        public int ItemId { get; set; }

        public int ModuleId { get; set; }
        public string Title { get; set; }
        public string CreatedByUser { get; set; }
        public string WhereWhen { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ExpireDate { get; set; }
        public string Description { get; set; }

        #endregion
    }
}