namespace ASPNET.StarterKit.Portal
{
    public interface IRoleItem
    {
        int RoleId { get; set; }
        int PortalId { get; set; }
        string RoleName { get; set; }
    }
}