using System;

namespace ASPNET.StarterKit.Portal
{
    public interface IDiscussionItem
    {
        int ItemID { get; set; }
        int ModuleID { get; set; }
        string Parent { get; set; }
        string Title { get; set; }
        DateTime? CreatedDate { get; set; }
        string Body { get; set; }
        string DisplayOrder { get; set; }
        string CreatedByUser { get; set; }
    }
}