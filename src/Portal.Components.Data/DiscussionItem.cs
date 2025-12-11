using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    [Table("Portal_Discussion")]
    public class DiscussionItem : IDiscussionItem
    {
        #region IDiscussionItem Members

        [Key]
        public int ItemID { get; set; }

        public int ModuleID { get; set; }
        public int ChildCount { get; set; } = 0;
        public string Title { get; set; } = string.Empty;
        public string Parent { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
        public string Body { get; set; } = string.Empty;
        public string DisplayOrder { get; set; } = string.Empty;
        public string CreatedByUser { get; set; } = string.Empty;

        // 可选：方便调试或显示的 ToString 重写
        public override string ToString()
        {
            return $"[{ItemID}] {Title} - {CreatedByUser} ({CreatedDate:yyyy-MM-dd HH:mm})";
        }

        #endregion
    }
}