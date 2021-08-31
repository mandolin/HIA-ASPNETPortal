using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    public class RolesDb : IRolesDb
    {
        private readonly PortalSecurityDbContext _context;

        public RolesDb(PortalSecurityDbContext context)
        {
            _context = context;
        }

        #region IRolesDb Members

        public IEnumerable<IRoleItem> GetPortalRoles(int portalId)
        {
            return _context.Roles.
                Where(i => i.PortalId == portalId).ToList<IRoleItem>();
        }

        public int AddRole(int portalId, string roleName)
        {
            var item = new RoleItem
                           {
                               PortalId = portalId,
                               RoleName = roleName
                           };
            _context.Roles.Add(item);
            _context.SaveChanges();

            // return the role id 
            return item.RoleId;
        }

        public void DeleteRole(int roleId)
        {
            RoleItem item = _context.Roles.Single(i => i.RoleId == roleId);
            _context.Roles.Remove(item);
            _context.SaveChanges();
        }

        public void UpdateRole(int roleId, string roleName)
        {
            RoleItem item = _context.Roles.Single(i => i.RoleId == roleId);
            item.RoleName = roleName;
            _context.SaveChanges();
        }


        public IEnumerable<IUserItem> GetRoleMembers(int roleId)
        {
            return _context.Roles.
                Single(i => i.RoleId == roleId).Users.ToList<IUserItem>();
        }

        public void AddUserRole(int roleId, int userId)
        {
            //TODO: check if this really adds the user to the role!
            UserItem item = _context.Users.Single(i => i.UserId == userId);
            _context.Roles.Single(i => i.RoleId == roleId).Users.Add(item);

            _context.SaveChanges();
        }

        public void DeleteUserRole(int roleId, int userId)
        {
            //TODO: check if this really deletes the user from the role or it deletes the user completely!
            UserItem item = _context.Users.Single(i => i.UserId == userId);
            _context.Roles.Single(i => i.RoleId == roleId).Users.Remove(item);

            _context.SaveChanges();
        }

        public IEnumerable<IUserItem> GetUsers()
        {
            return _context.Users.
                OrderBy(i => i.Email).ToList<IUserItem>();
        }

        #endregion
    }
}