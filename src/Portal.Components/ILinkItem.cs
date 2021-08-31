using System;

namespace ASPNET.StarterKit.Portal
{
    public interface ILinkItem
    {
        int ItemId { get; set; }
        int ModuleId { get; set; }
        string CreatedByUser { get; set; }
        DateTime? CreatedDate { get; set; }
        string Title { get; set; }
        string Url { get; set; }
        string MobileUrl { get; set; }
        int? ViewOrder { get; set; }
        string Description { get; set; }
    }
}