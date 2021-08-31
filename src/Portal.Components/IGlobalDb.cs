namespace ASPNET.StarterKit.Portal
{
    public interface IGlobalsDb
    {
        IGlobalItem GetSinglePortal(int portalId);
        void UpdatePortalInfo(int portalId, string portalName, bool alwaysShow);
    }
}