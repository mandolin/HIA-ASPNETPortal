using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    public class RolesDb : IRolesDb
    {
        private readonly PortalSecurityDbContext _context;

        /// <summary>
        /// 初始化角色数据库操作类。
        /// </summary>
        /// <param name="context">数据库上下文。</param>
        public RolesDb(PortalSecurityDbContext context)
        {
            _context = context;
        }

        #region IRolesDb Members

        /// <summary>
        /// 获取指定门户ID下的所有角色。
        /// </summary>
        /// <param name="portalId">门户ID。</param>
        /// <returns>角色集合。</returns>
        public IEnumerable<IRoleItem> GetPortalRoles(int portalId)
        {
            // 使用LINQ查询获取所有具有指定portalId的角色，并将其转换为IRoleItem接口类型列表。
            return _context.Roles.Where(i => i.PortalId == portalId).ToList();
        }

        /// <summary>
        /// 添加一个新角色。
        /// </summary>
        /// <param name="portalId">门户ID。</param>
        /// <param name="roleName">角色名称。</param>
        /// <returns>新角色的ID。</returns>
        public int AddRole(int portalId, string roleName)
        {
            // 创建新的角色对象并设置属性。
            var item = new RoleItem
            {
                PortalId = portalId,
                RoleName = roleName
            };

            // 将新角色添加到上下文。
            _context.Roles.Add(item);

            // 保存更改到数据库。
            _context.SaveChanges();

            // 返回新角色的ID。
            return item.RoleId;
        }

        /// <summary>
        /// 删除指定ID的角色。
        /// </summary>
        /// <param name="roleId">角色ID。</param>
        public void DeleteRole(int roleId)
        {
            // 根据角色ID获取角色对象。
            var item = _context.Roles.Single(i => i.RoleId == roleId);
            var affectedUserIds = item.Users == null
                ? new List<int>()
                : item.Users.Select(user => user.UserId).ToList();

            // 从上下文中移除角色。
            _context.Roles.Remove(item);

            // 保存更改到数据库。
            _context.SaveChanges();
            IncrementSecurityVersions(affectedUserIds, "RoleDeleted");
        }

        /// <summary>
        /// 更新指定ID的角色名称。
        /// </summary>
        /// <param name="roleId">角色ID。</param>
        /// <param name="roleName">新角色名称。</param>
        public void UpdateRole(int roleId, string roleName)
        {
            // 根据角色ID获取角色对象。
            var item = _context.Roles.Single(i => i.RoleId == roleId);
            var affectedUserIds = item.Users == null
                ? new List<int>()
                : item.Users.Select(user => user.UserId).ToList();

            // 更新角色名称。
            item.RoleName = roleName;

            // 保存更改到数据库。
            _context.SaveChanges();
            IncrementSecurityVersions(affectedUserIds, "RoleUpdated");
        }

        /// <summary>
        /// 获取指定角色ID的所有成员。
        /// </summary>
        /// <param name="roleId">角色ID。</param>
        /// <returns>角色成员集合。</returns>
        public IEnumerable<IUserItem> GetRoleMembers(int roleId)
        {
            // 根据角色ID获取角色对象，并获取其所有成员。
            return _context.Roles.Single(i => i.RoleId == roleId).Users.ToList();
        }

        /// <summary>
        /// 向角色中添加用户。
        /// </summary>
        /// <param name="roleId">角色ID。</param>
        /// <param name="userId">用户ID。</param>
        public void AddUserRole(int roleId, int userId)
        {
            // 根据用户ID获取用户对象。
            var item = _context.Users.Single(i => i.UserId == userId);

            // 根据角色ID获取角色对象，并向其中添加用户。
            var role = _context.Roles.Single(i => i.RoleId == roleId);
            role.Users.Add(item);

            // 保存更改到数据库。
            _context.SaveChanges();
            IncrementSecurityVersion(userId, "RoleMembershipAdded");
        }

        /// <summary>
        /// 从角色中删除用户。
        /// </summary>
        /// <param name="roleId">角色ID。</param>
        /// <param name="userId">用户ID。</param>
        public void DeleteUserRole(int roleId, int userId)
        {
            // 根据用户ID获取用户对象。
            var item = _context.Users.Single(i => i.UserId == userId);

            // 根据角色ID获取角色对象，并从中移除用户。
            var role = _context.Roles.Single(i => i.RoleId == roleId);
            role.Users.Remove(item);

            // 保存更改到数据库。
            _context.SaveChanges();
            IncrementSecurityVersion(userId, "RoleMembershipRemoved");
        }

        /// <summary>
        /// 获取所有用户，并按名称排序。
        /// </summary>
        /// <returns>用户集合。</returns>
        public IEnumerable<IUserItem> GetUsers()
        {
            // 使用LINQ查询获取所有用户，并按名称排序后返回。
            return _context.Users.OrderBy(i => i.Name).ToList();
        }

        #endregion

        private void IncrementSecurityVersions(IEnumerable<int> userIds, string reason)
        {
            foreach (int userId in userIds.Distinct())
            {
                IncrementSecurityVersion(userId, reason);
            }
        }

        private void IncrementSecurityVersion(int userId, string reason)
        {
            if (userId <= 0 || !HasSecurityStateTable())
            {
                return;
            }

            _context.Database.ExecuteSqlCommand(
                @"
UPDATE [dbo].[Portal_UserSecurityStates]
SET [SecurityVersion] = [SecurityVersion] + 1,
    [ChangedUtc] = SYSUTCDATETIME(),
    [ChangeReason] = @p1
WHERE [UserId] = @p0;

IF @@ROWCOUNT = 0
BEGIN
    INSERT INTO [dbo].[Portal_UserSecurityStates] ([UserId], [SecurityVersion], [ChangedUtc], [ChangeReason])
    SELECT @p0, @p2, SYSUTCDATETIME(), @p1
    WHERE EXISTS (SELECT 1 FROM [dbo].[Portal_Users] WHERE [UserID] = @p0);
END",
                userId,
                NormalizeReason(reason),
                1);
        }

        private bool HasSecurityStateTable()
        {
            try
            {
                string sql = "SELECT CASE WHEN OBJECT_ID(N'[dbo].[Portal_UserSecurityStates]', N'U') IS NULL THEN 0 ELSE 1 END";
                return _context.Database.SqlQuery<int>(sql).Single() == 1;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string NormalizeReason(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Unspecified";
            }

            string normalized = value.Trim();
            return normalized.Substring(0, Math.Min(normalized.Length, 100));
        }
    }
}
