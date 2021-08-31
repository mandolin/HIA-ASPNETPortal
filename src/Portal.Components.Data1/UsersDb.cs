using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    public class UsersDb : IUsersDb
    {
        private readonly PortalSecurityDbContext _context;

        public UsersDb(PortalSecurityDbContext context)
        {
            _context = context;
        }

        #region IUsersDb Members

        public int AddUser(String fullName, string email, string password)
        {
            var item = new UserItem
                           {
                               Name = fullName,
                               Email = email,
                               Password = password
                           };

            // Execute the command in a try/catch to catch duplicate username errors
            try
            {
                _context.Users.Add(item);
                _context.SaveChanges();
            }
            catch
            {
                // failed to create a new user
                return -1;
            }

            return item.UserId;
        }

        public void DeleteUser(int userId)
        {
            UserItem item = _context.Users.Single(i => i.UserId == userId);
            _context.Users.Remove(item);
            _context.SaveChanges();
        }

        public void UpdateUser(int userId, string email, string password)
        {
            UserItem item = _context.Users.Single(i => i.UserId == userId);
            item.Email = email;
            item.Password = password;
            _context.SaveChanges();
        }

        public IEnumerable<IRoleItem> GetRolesByUser(String email)
        {
            return _context.Users.
                Single(i => i.Email == email).Roles.
                ToList<IRoleItem>();
        }

        public IEnumerable<string> GetRoleNamesByUser(String email)
        {
            UserItem item = _context.Users.Single(i => i.Email == email);
            return item.Roles.
                Select(i => i.RoleName);
        }

        public IUserItem GetSingleUser(String email)
        {
            return _context.Users.Single(i => i.Email == email);
        }

        public string Login(String email, string password)
        {
            UserItem item = _context.Users.SingleOrDefault(i => i.Email == email && i.Password == password);

            if (item != default(UserItem))
            {
                return item.Name.Trim();
            }
            return string.Empty;
        }

        #endregion
    }
}