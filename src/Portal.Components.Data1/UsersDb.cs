using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    public class UsersDb : IUsersDb
    {
        private readonly PortalSecurityDbContext _context;

        /// <summary>
        /// 初始化用户数据库操作类。
        /// </summary>
        /// <param name="context">数据库上下文。</param>
        public UsersDb(PortalSecurityDbContext context)
        {
            _context = context;
        }

        #region IUsersDb Members

        /// <summary>
        /// 添加用户。
        /// </summary>
        /// <param name="fullName">用户全名。</param>
        /// <param name="email">用户邮箱。</param>
        /// <param name="password">用户密码。</param>
        /// <returns>返回添加的用户ID，如果添加失败则返回-1。</returns>
        public int AddUser(string fullName, string email, string password)
        {
            // 创建一个新的用户对象并设置属性。
            var item = new UserItem
            {
                Name = fullName,
                Email = email,
                Password = password
            };

            // 执行命令并捕获可能的异常（例如用户名重复）。
            try
            {
                // 将新的用户对象添加到数据库上下文中。
                _context.Users.Add(item);

                // 保存更改到数据库。
                _context.SaveChanges();
            }
            catch (Exception)
            {
                // 如果发生错误，则返回-1表示添加失败。
                return -1;
            }

            // 返回成功添加的用户的ID。
            return item.UserId;
        }

        /// <summary>
        /// 删除指定ID的用户。
        /// </summary>
        /// <param name="userId">用户ID。</param>
        public void DeleteUser(int userId)
        {
            // 通过用户ID获取用户对象。
            var item = _context.Users.Single(i => i.UserId == userId);

            // 从数据库上下文中移除用户对象。
            _context.Users.Remove(item);

            // 保存更改到数据库。
            _context.SaveChanges();
        }

        /// <summary>
        /// 更新指定ID的用户信息。
        /// </summary>
        /// <param name="userId">用户ID。</param>
        /// <param name="email">新邮箱地址。</param>
        /// <param name="password">新密码。</param>
        public void UpdateUser(int userId, string email, string password)
        {
            // 通过用户ID获取用户对象。
            var item = _context.Users.Single(i => i.UserId == userId);

            // 更新用户的邮箱和密码。
            item.Email = email;
            item.Password = password;

            // 保存更改到数据库。
            _context.SaveChanges();
        }

        /// <summary>
        /// 获取指定用户名的所有角色。
        /// </summary>
        /// <param name="name">用户名。</param>
        /// <returns>角色集合。</returns>
        public IEnumerable<IRoleItem> GetRolesByUser(string name) 
        {
            // 通过用户名获取用户对象，并获取其所有角色。
            return _context.Users.Single(i => i.Name == name).Roles.ToList<IRoleItem>();
        }

        /// <summary>
        /// 获取指定用户名的所有角色名称。
        /// </summary>
        /// <param name="name">用户名。</param>
        /// <returns>角色名称集合。</returns>
        public IEnumerable<string> GetRoleNamesByUser(string name)
        {
            // 通过用户名获取用户对象，并选择其所有角色的名称。
            var item = _context.Users.Single(i => i.Name == name);
            return item.Roles.Select(i => i.RoleName);
        }

        /// <summary>
        /// 获取单个用户。
        /// </summary>
        /// <param name="name">用户名。</param>
        /// <returns>用户对象。</returns>
        public IUserItem GetSingleUser(string name)
        {
            // 通过用户名获取用户对象。
            return _context.Users.Single(i => i.Name == name);
        }

        /// <summary>
        /// 登录用户。
        /// </summary>
        /// <param name="emailOrName">邮箱或用户名。</param>
        /// <param name="password">密码。</param>
        /// <returns>登录成功的用户名，或空字符串表示登录失败。</returns>
        public string Login(string emailOrName, string password)
        {
            // 尝试通过邮箱或用户名以及密码查找用户。
            var item = _context.Users.SingleOrDefault(i => (i.Email == emailOrName || i.Name == emailOrName) && i.Password == password);

            // 如果找到了匹配的用户，则返回用户名；否则返回空字符串。
            if (item != null)
            {
                return item.Name.Trim();
            }
            return string.Empty;
        }

        #endregion
    }
}