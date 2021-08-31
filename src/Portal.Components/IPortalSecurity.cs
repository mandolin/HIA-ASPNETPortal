namespace ASPNET.StarterKit.Portal
{
    public interface IPortalSecurity
    {
        /// <summary>
        ///   The HasEditPermissions method enables developers to easily check 
        ///   whether the current browser client has access to edit the settings
        ///   of a specified portal module
        /// </summary>
        bool HasEditPermissions(int moduleId);
    }
}