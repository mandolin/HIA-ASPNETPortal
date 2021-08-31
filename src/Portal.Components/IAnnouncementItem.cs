using System;

namespace ASPNET.StarterKit.Portal
{
    public interface IAnnouncementItem
    {
        int ItemId { get; set; }
        int ModuleId { get; set; }
        string CreatedByUser { get; set; }
        DateTime? CreatedDate { get; set; }
        string Title { get; set; }
        string MoreLink { get; set; }
        string MobileMoreLink { get; set; }
        DateTime? ExpireDate { get; set; }
        string Description { get; set; }
    }
}