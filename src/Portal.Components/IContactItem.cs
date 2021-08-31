using System;

namespace ASPNET.StarterKit.Portal
{
    public interface IContactItem
    {
        int ItemId { get; set; }
        int ModuleId { get; set; }
        DateTime? CreatedDate { get; set; }
        string CreatedByUser { get; set; }
        string Name { get; set; }
        string Role { get; set; }
        string Email { get; set; }
        string Contact1 { get; set; }
        string Contact2 { get; set; }
    }
}