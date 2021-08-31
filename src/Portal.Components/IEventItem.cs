using System;

namespace ASPNET.StarterKit.Portal
{
    public interface IEventItem
    {
        int ItemId { get; set; }
        int ModuleId { get; set; }
        string Title { get; set; }
        string CreatedByUser { get; set; }
        string WhereWhen { get; set; }
        DateTime? CreatedDate { get; set; }
        DateTime? ExpireDate { get; set; }
        string Description { get; set; }
    }
}