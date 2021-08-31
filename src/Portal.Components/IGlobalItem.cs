namespace ASPNET.StarterKit.Portal
{
    public interface IGlobalItem
    {
        int PortalId { get; set; }
        string PortalName { get; set; }
        bool? AlwaysShowEditButton { get; set; }
    }
}