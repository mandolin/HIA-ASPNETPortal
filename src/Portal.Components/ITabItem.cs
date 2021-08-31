namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   This class encapsulates the basic attributes of a Tab, and is used
    ///   by the administration pages when manipulating tabs.
    /// </summary>
    public interface ITabItem
    {
        int? TabOrder { get; set; }
        string TabName { get; set; }
        int TabId { get; set; }
        string AccessRoles { get; set; }
        string MobileTabName { get; set; }
        bool? ShowMobile { get; set; }
    }
}