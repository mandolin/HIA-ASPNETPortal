using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    public interface IRolesDb
    {
        IEnumerable<IRoleItem> GetPortalRoles(int portalId);
        int AddRole(int portalId, string roleName);
        void DeleteRole(int roleId);
        void UpdateRole(int roleId, string roleName);
        IEnumerable<IUserItem> GetRoleMembers(int roleId);
        void AddUserRole(int roleId, int userId);
        void DeleteUserRole(int roleId, int userId);
        IEnumerable<IUserItem> GetUsers();
    }
}