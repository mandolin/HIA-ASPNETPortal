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
            var affectedUserIds = GetRoleMemberIds(roleId);

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
            var affectedUserIds = GetRoleMemberIds(roleId);

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
            // 中文：旧 EF 导航集合在某些运行路径中可能未初始化；显式读取中间表更稳定。
            // English: The legacy EF navigation collection can be uninitialized in some runtime paths; reading the join table explicitly is more stable.
            var userIds = GetRoleMemberIds(roleId);
            return _context.Users
                .Where(user => userIds.Contains(user.UserId))
                .OrderBy(user => user.Name)
                .ToList<IUserItem>();
        }

        /// <summary>
        /// 向角色中添加用户。
        /// </summary>
        /// <param name="roleId">角色ID。</param>
        /// <param name="userId">用户ID。</param>
        public void AddUserRole(int roleId, int userId)
        {
            // 中文：显式确认用户和角色存在，再用中间表写入，避免旧 EF 导航集合为空导致后台错误页。
            // English: Confirm that user and role exist, then write the join table explicitly to avoid admin error pages from null legacy EF collections.
            EnsureUserAndRoleExist(roleId, userId);
            if (HasUserRole(roleId, userId))
            {
                return;
            }

            _context.Database.ExecuteSqlCommand(
                "INSERT INTO [dbo].[Portal_UserRoles] ([UserID], [RoleID]) VALUES (@p0, @p1)",
                userId,
                roleId);
            IncrementSecurityVersion(userId, "RoleMembershipAdded");
        }

        /// <summary>
        /// 从角色中删除用户。
        /// </summary>
        /// <param name="roleId">角色ID。</param>
        /// <param name="userId">用户ID。</param>
        public void DeleteUserRole(int roleId, int userId)
        {
            EnsureUserAndRoleExist(roleId, userId);
            if (!HasUserRole(roleId, userId))
            {
                return;
            }

            _context.Database.ExecuteSqlCommand(
                "DELETE FROM [dbo].[Portal_UserRoles] WHERE [UserID] = @p0 AND [RoleID] = @p1",
                userId,
                roleId);
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

        /// <summary>
        /// 中文：读取指定用户通过角色映射获得的权限键；权限扩展表未部署时保持空集合。
        ///
        /// English: Reads permission keys granted through role mappings for the specified user; returns an empty
        /// collection when the permission extension table is not deployed.
        /// </summary>
        /// <param name="name">中文：用户登录名或邮箱。English: User sign-in name or email.</param>
        /// <returns>中文：权限键集合。English: Permission-key collection.</returns>
        public IEnumerable<string> GetPermissionKeysByUserName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || !HasRolePermissionsTable())
            {
                return Enumerable.Empty<string>();
            }

            return _context.Database.SqlQuery<string>(
                @"
SELECT DISTINCT [RolePermissions].[PermissionKey]
FROM [dbo].[PortalCfg_RolePermissions] AS [RolePermissions]
INNER JOIN [dbo].[Portal_UserRoles] AS [UserRoles]
    ON [UserRoles].[RoleID] = [RolePermissions].[RoleId]
INNER JOIN [dbo].[Portal_Users] AS [Users]
    ON [Users].[UserID] = [UserRoles].[UserID]
WHERE [RolePermissions].[IsEnabled] = 1
  AND ([Users].[Name] = @p0 OR [Users].[Email] = @p0)
ORDER BY [RolePermissions].[PermissionKey]",
                name.Trim()).ToList();
        }

        /// <summary>
        /// 中文：替换角色权限映射，并递增该角色成员的安全版本以使旧票据在下一请求重新判定。
        ///
        /// English: Replaces role-permission mappings and increments member security versions so older tickets are
        /// re-evaluated on the next request.
        /// </summary>
        /// <param name="roleId">中文：角色数值标识。English: Numeric role identifier.</param>
        /// <param name="permissionKeys">中文：新的权限键集合。English: New permission-key collection.</param>
        /// <param name="updatedBy">中文：执行更新的维护者标识。English: Maintainer identifier performing the update.</param>
        public void SaveRolePermissions(int roleId, IEnumerable<string> permissionKeys, string updatedBy)
        {
            if (!HasRolePermissionsTable())
            {
                throw new InvalidOperationException("PortalCfg_RolePermissions is not available.");
            }

            var role = _context.Roles.Single(i => i.RoleId == roleId);
            string[] normalizedKeys = PortalPermissionRegistry.NormalizeDefinedKeys(permissionKeys);
            List<int> affectedUserIds = GetRoleMemberIds(role.RoleId);

            using (var transaction = _context.Database.BeginTransaction())
            {
                _context.Database.ExecuteSqlCommand(
                    "DELETE FROM [dbo].[PortalCfg_RolePermissions] WHERE [RoleId] = @p0",
                    role.RoleId);

                foreach (string permissionKey in normalizedKeys)
                {
                    _context.Database.ExecuteSqlCommand(
                        @"
INSERT INTO [dbo].[PortalCfg_RolePermissions]
    ([RoleId], [PermissionKey], [IsEnabled], [UpdatedUtc], [UpdatedBy])
VALUES
    (@p0, @p1, 1, SYSUTCDATETIME(), @p2)",
                        role.RoleId,
                        permissionKey,
                        NormalizeUpdatedBy(updatedBy));
                }

                IncrementSecurityVersions(affectedUserIds, "RolePermissionsChanged");
                transaction.Commit();
            }
        }

        #endregion

        private void IncrementSecurityVersions(IEnumerable<int> userIds, string reason)
        {
            foreach (int userId in userIds.Distinct())
            {
                IncrementSecurityVersion(userId, reason);
            }
        }

        private List<int> GetRoleMemberIds(int roleId)
        {
            return _context.Database.SqlQuery<int>(
                "SELECT [UserID] FROM [dbo].[Portal_UserRoles] WHERE [RoleID] = @p0",
                roleId).ToList();
        }

        private void EnsureUserAndRoleExist(int roleId, int userId)
        {
            _context.Users.Single(user => user.UserId == userId);
            _context.Roles.Single(role => role.RoleId == roleId);
        }

        private bool HasUserRole(int roleId, int userId)
        {
            return _context.Database.SqlQuery<int>(
                "SELECT CASE WHEN EXISTS (SELECT 1 FROM [dbo].[Portal_UserRoles] WHERE [UserID] = @p0 AND [RoleID] = @p1) THEN 1 ELSE 0 END",
                userId,
                roleId).Single() == 1;
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

        private bool HasRolePermissionsTable()
        {
            try
            {
                string sql = "SELECT CASE WHEN OBJECT_ID(N'[dbo].[PortalCfg_RolePermissions]', N'U') IS NULL THEN 0 ELSE 1 END";
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

        private static string NormalizeUpdatedBy(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "system";
            }

            string normalized = value.Trim();
            return normalized.Substring(0, Math.Min(normalized.Length, 100));
        }
    }
}
