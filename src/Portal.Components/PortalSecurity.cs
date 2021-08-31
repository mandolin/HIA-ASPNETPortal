using System;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   The PortalSecurity class encapsulates two helper methods that enable
    ///   developers to easily check the role status of the current browser client.
    /// </summary>
    public class PortalSecurity : IPortalSecurity
    {
        private readonly IModulesDb _modulesConfig;
        private readonly ITabsDb _tabsConfig;

        public PortalSecurity(ITabsDb tabsConfig, IModulesDb modulesConfig)
        {
            _tabsConfig = tabsConfig;
            _modulesConfig = modulesConfig;
        }

        #region IPortalSecurity Members

        /// <summary>
        ///   The HasEditPermissions method enables developers to easily check 
        ///   whether the current browser client has access to edit the settings
        ///   of a specified portal module
        /// </summary>
        public bool HasEditPermissions(int moduleId)
        {
            // Find the appropriate Module 
            IModuleItem module = _modulesConfig.GetSingleModule(moduleId);

            string editRoles = module.EditRoles;
            string accessRoles = _tabsConfig.GetSingleTab(module.TabId.Value).AccessRoles;

            if (IsInRoles(accessRoles) == false || IsInRoles(editRoles) == false)
            {
                return false;
            }
            return true;
        }

        #endregion

        /// <summary>
        ///   The Encrypt method encrypts a clean string into a hashed string
        /// </summary>
        public static string Encrypt(string cleanString)
        {
            Byte[] clearBytes = new UnicodeEncoding().GetBytes(cleanString);
            Byte[] hashedBytes = ((HashAlgorithm) CryptoConfig.CreateFromName("MD5")).ComputeHash(clearBytes);

            return BitConverter.ToString(hashedBytes);
        }

        /// <summary>
        ///   The IsInRole method enables developers to easily check the role
        ///   status of the current browser client.
        /// </summary>
        public static bool IsInRole(String role)
        {
            return HttpContext.Current.User.IsInRole(role);
        }

        /// <summary>
        ///   The IsInRoles method enables developers to easily check the role
        ///   status of the current browser client against an array of roles
        /// </summary>
        public static bool IsInRoles(String roles)
        {
            HttpContext context = HttpContext.Current;

            foreach (String role in roles.Split(new[] {';'}))
            {
                if (!string.IsNullOrEmpty(role) && ((role == "All Users") || (context.User.IsInRole(role))))
                {
                    return true;
                }
            }

            return false;
        }
    }
}