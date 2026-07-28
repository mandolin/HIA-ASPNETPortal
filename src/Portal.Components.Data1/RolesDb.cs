using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>旧门户角色、成员关系和角色权限映射的数据访问实现。</zh-CN>
    ///   <en>Data-access implementation for legacy portal roles, role membership, and role-permission mappings.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本类型仍直接维护旧 `Portal_Roles` / `Portal_UserRoles` 结构。会改变角色成员或角色权限的写入路径会递增受影响用户的安全版本，使旧认证票据在下一请求重新判定。</zh-CN>
    ///   <en>This type still maintains the legacy `Portal_Roles` / `Portal_UserRoles` structures directly. Write paths that change membership or role permissions increment affected users' security versions so older authentication tickets are re-evaluated on the next request.</en>
    /// </lang>
    /// </remarks>
    public class RolesDb : IRolesDb
    {
        private readonly PortalSecurityDbContext _context;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化角色数据访问实现。</zh-CN>
        ///   <en>Initializes the role data-access implementation.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>门户安全数据库上下文。</zh-CN>
        ///   <en>Portal security database context.</en>
        /// </l>
        /// </param>
        public RolesDb(PortalSecurityDbContext context)
        {
            _context = context;
        }

        #region IRolesDb Members

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定门户下的全部角色。</zh-CN>
        ///   <en>Reads all roles for the specified Portal.</en>
        /// </lang>
        /// </summary>
        /// <param name="portalId">
        /// <l>
        ///   <zh-CN>门户标识。</zh-CN>
        ///   <en>Portal identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>可由后台维护的角色集合；不含 <c>All Users</c> 的权限配置载体记录。</zh-CN>
        ///   <en>Roles that can be maintained through administration, excluding the <c>All Users</c> permission-configuration carrier.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN><c>All Users</c> 在访问判定中仍是虚拟通配角色。细粒度权限表需要外键目标时，迁移会维护一个同名但无成员关系的配置载体记录；后台角色列表必须隐藏它，以免被误当成可分配或可删除的普通角色。</zh-CN>
        ///   <en><c>All Users</c> remains a virtual wildcard for access checks. When the fine-grained permission table needs a foreign-key target, migration maintains a same-named configuration carrier with no membership; administration lists must hide it so it is not mistaken for an assignable or deletable regular role.</en>
        /// </lang>
        /// </remarks>
        public IEnumerable<IRoleItem> GetPortalRoles(int portalId)
        {
            return _context.Roles
                .Where(i => i.PortalId == portalId && i.RoleName != PortalRoleNames.AllUsers)
                .ToList();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>为门户新增一个角色。</zh-CN>
        ///   <en>Adds a new role for a Portal.</en>
        /// </lang>
        /// </summary>
        /// <param name="portalId">
        /// <l>
        ///   <zh-CN>门户标识。</zh-CN>
        ///   <en>Portal identifier.</en>
        /// </l>
        /// </param>
        /// <param name="roleName">
        /// <l>
        ///   <zh-CN>角色名称。</zh-CN>
        ///   <en>Role name.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>新角色标识。</zh-CN>
        ///   <en>New role identifier.</en>
        /// </l>
        /// </returns>
        public int AddRole(int portalId, string roleName)
        {
            var item = new RoleItem
            {
                PortalId = portalId,
                RoleName = roleName
            };

            _context.Roles.Add(item);

            // <lang>
            //   <zh-CN>保存后使用数据库生成的 RoleId 作为后台页面继续编辑和授权映射的稳定标识。</zh-CN>
            //   <en>After saving, the database-generated RoleId becomes the stable identifier used by administration pages for later editing and permission mapping.</en>
            // </lang>
            _context.SaveChanges();
            return item.RoleId;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除指定角色，并使原成员的安全版本失效。</zh-CN>
        ///   <en>Deletes the specified role and invalidates security versions for previous members.</en>
        /// </lang>
        /// </summary>
        /// <param name="roleId">
        /// <l>
        ///   <zh-CN>角色标识。</zh-CN>
        ///   <en>Role identifier.</en>
        /// </l>
        /// </param>
        public void DeleteRole(int roleId)
        {
            var item = _context.Roles.Single(i => i.RoleId == roleId);
            var affectedUserIds = GetRoleMemberIds(roleId);

            _context.Roles.Remove(item);

            // <lang>
            //   <zh-CN>先保存角色删除，再递增原成员安全版本；这样即使旧票据仍存在，下一请求也会重新读取角色状态。</zh-CN>
            //   <en>Persist the role deletion first, then increment former members' security versions so existing tickets re-read role state on the next request.</en>
            // </lang>
            _context.SaveChanges();
            IncrementSecurityVersions(affectedUserIds, "RoleDeleted");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新指定角色名称，并使成员票据重新判定角色信息。</zh-CN>
        ///   <en>Updates a role name and causes members' tickets to re-evaluate role information.</en>
        /// </lang>
        /// </summary>
        /// <param name="roleId">
        /// <l>
        ///   <zh-CN>角色标识。</zh-CN>
        ///   <en>Role identifier.</en>
        /// </l>
        /// </param>
        /// <param name="roleName">
        /// <l>
        ///   <zh-CN>新的角色名称。</zh-CN>
        ///   <en>New role name.</en>
        /// </l>
        /// </param>
        public void UpdateRole(int roleId, string roleName)
        {
            var item = _context.Roles.Single(i => i.RoleId == roleId);
            var affectedUserIds = GetRoleMemberIds(roleId);

            item.RoleName = roleName;

            // <lang>
            //   <zh-CN>角色名称会进入角色 Cookie 和后台显示；保存后递增安全版本，让旧票据尽快重新生成。</zh-CN>
            //   <en>Role names flow into role cookies and administration displays; after saving, increment security versions so older tickets are regenerated promptly.</en>
            // </lang>
            _context.SaveChanges();
            IncrementSecurityVersions(affectedUserIds, "RoleUpdated");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定角色的成员用户。</zh-CN>
        ///   <en>Reads users assigned to the specified role.</en>
        /// </lang>
        /// </summary>
        /// <param name="roleId">
        /// <l>
        ///   <zh-CN>角色标识。</zh-CN>
        ///   <en>Role identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>按用户名排序的成员用户集合。</zh-CN>
        ///   <en>Member users ordered by user name.</en>
        /// </l>
        /// </returns>
        public IEnumerable<IUserItem> GetRoleMembers(int roleId)
        {
            // <lang>
            //   <zh-CN>旧 EF 导航集合在某些运行路径中可能未初始化；显式读取中间表更稳定。</zh-CN>
            //   <en>The legacy EF navigation collection can be uninitialized in some runtime paths; reading the join table explicitly is more stable.</en>
            // </lang>
            var userIds = GetRoleMemberIds(roleId);
            return _context.Users
                .Where(user => userIds.Contains(user.UserId))
                .OrderBy(user => user.Name)
                .ToList<IUserItem>();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把用户加入指定角色。</zh-CN>
        ///   <en>Adds a user to the specified role.</en>
        /// </lang>
        /// </summary>
        /// <param name="roleId">
        /// <l>
        ///   <zh-CN>角色标识。</zh-CN>
        ///   <en>Role identifier.</en>
        /// </l>
        /// </param>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>用户标识。</zh-CN>
        ///   <en>User identifier.</en>
        /// </l>
        /// </param>
        public void AddUserRole(int roleId, int userId)
        {
            // <lang>
            //   <zh-CN>显式确认用户和角色存在，再用中间表写入，避免旧 EF 导航集合为空导致后台错误页。</zh-CN>
            //   <en>Confirm that user and role exist, then write the join table explicitly to avoid admin error pages from null legacy EF collections.</en>
            // </lang>
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
        /// <lang>
        ///   <zh-CN>从指定角色移除用户。</zh-CN>
        ///   <en>Removes a user from the specified role.</en>
        /// </lang>
        /// </summary>
        /// <param name="roleId">
        /// <l>
        ///   <zh-CN>角色标识。</zh-CN>
        ///   <en>Role identifier.</en>
        /// </l>
        /// </param>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>用户标识。</zh-CN>
        ///   <en>User identifier.</en>
        /// </l>
        /// </param>
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
        /// <lang>
        ///   <zh-CN>读取所有门户用户，并按名称排序。</zh-CN>
        ///   <en>Reads all Portal users ordered by name.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>用户集合。</zh-CN>
        ///   <en>User collection.</en>
        /// </l>
        /// </returns>
        public IEnumerable<IUserItem> GetUsers()
        {
            return _context.Users.OrderBy(i => i.Name).ToList();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定用户通过角色映射获得的权限键；权限扩展表未部署时保持空集合。</zh-CN>
        ///   <en>Reads permission keys granted through role mappings for the specified user; returns an empty collection when the permission extension table is not deployed.</en>
        /// </lang>
        /// </summary>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>用户登录名或邮箱。</zh-CN>
        ///   <en>User sign-in name or email.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>直接角色成员关系以及虚拟 <c>All Users</c> 配置载体授予的权限键集合。</zh-CN>
        ///   <en>Permission keys granted through direct role membership and the virtual <c>All Users</c> configuration carrier.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>为保持旧门户中 <c>All Users</c> 的通配语义，查询会把同名配置载体的启用权限合并给每个已认证身份，但不会要求或写入 <c>Portal_UserRoles</c> 成员记录。普通角色仍严格按用户成员关系查询。</zh-CN>
        ///   <en>To preserve the legacy wildcard semantics of <c>All Users</c>, this query unions enabled permissions from its same-named configuration carrier for every authenticated identity without requiring or writing a <c>Portal_UserRoles</c> membership row. Regular roles remain strictly membership based.</en>
        /// </lang>
        /// </remarks>
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
INNER JOIN [dbo].[Portal_Roles] AS [Roles]
    ON [Roles].[RoleID] = [RolePermissions].[RoleId]
LEFT JOIN [dbo].[Portal_UserRoles] AS [UserRoles]
    ON [UserRoles].[RoleID] = [RolePermissions].[RoleId]
LEFT JOIN [dbo].[Portal_Users] AS [Users]
    ON [Users].[UserID] = [UserRoles].[UserID]
WHERE [RolePermissions].[IsEnabled] = 1
  AND
  (
      [Roles].[RoleName] = @p1
      OR [Users].[Name] = @p0
      OR [Users].[Email] = @p0
  )
ORDER BY [RolePermissions].[PermissionKey]",
                name.Trim(),
                PortalRoleNames.AllUsers).ToList();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>替换角色权限映射，并递增该角色成员的安全版本以使旧票据在下一请求重新判定。</zh-CN>
        ///   <en>Replaces role-permission mappings and increments member security versions so older tickets are re-evaluated on the next request.</en>
        /// </lang>
        /// </summary>
        /// <param name="roleId">
        /// <l>
        ///   <zh-CN>角色数值标识。</zh-CN>
        ///   <en>Numeric role identifier.</en>
        /// </l>
        /// </param>
        /// <param name="permissionKeys">
        /// <l>
        ///   <zh-CN>新的权限键集合。</zh-CN>
        ///   <en>New permission-key collection.</en>
        /// </l>
        /// </param>
        /// <param name="updatedBy">
        /// <l>
        ///   <zh-CN>执行更新的维护者标识。</zh-CN>
        ///   <en>Maintainer identifier performing the update.</en>
        /// </l>
        /// </param>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>批量递增用户安全版本，重复用户只处理一次。</zh-CN>
        ///   <en>Increments user security versions in bulk, processing duplicate users only once.</en>
        /// </lang>
        /// </summary>
        /// <param name="userIds">
        /// <l>
        ///   <zh-CN>受影响用户标识集合。</zh-CN>
        ///   <en>Affected user identifiers.</en>
        /// </l>
        /// </param>
        /// <param name="reason">
        /// <l>
        ///   <zh-CN>安全版本变化原因。</zh-CN>
        ///   <en>Reason for the security-version change.</en>
        /// </l>
        /// </param>
        private void IncrementSecurityVersions(IEnumerable<int> userIds, string reason)
        {
            foreach (int userId in userIds.Distinct())
            {
                IncrementSecurityVersion(userId, reason);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定角色当前成员的用户标识。</zh-CN>
        ///   <en>Reads user identifiers for the current members of the specified role.</en>
        /// </lang>
        /// </summary>
        /// <param name="roleId">
        /// <l>
        ///   <zh-CN>角色标识。</zh-CN>
        ///   <en>Role identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>成员用户标识列表。</zh-CN>
        ///   <en>List of member user identifiers.</en>
        /// </l>
        /// </returns>
        private List<int> GetRoleMemberIds(int roleId)
        {
            return _context.Database.SqlQuery<int>(
                "SELECT [UserID] FROM [dbo].[Portal_UserRoles] WHERE [RoleID] = @p0",
                roleId).ToList();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>确认用户和角色都存在；不存在时让调用方收到一致的实体缺失异常。</zh-CN>
        ///   <en>Confirms that both user and role exist, surfacing a consistent entity-missing exception to callers when either is absent.</en>
        /// </lang>
        /// </summary>
        private void EnsureUserAndRoleExist(int roleId, int userId)
        {
            _context.Users.Single(user => user.UserId == userId);
            _context.Roles.Single(role => role.RoleId == roleId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查用户是否已经属于指定角色。</zh-CN>
        ///   <en>Checks whether the user already belongs to the specified role.</en>
        /// </lang>
        /// </summary>
        private bool HasUserRole(int roleId, int userId)
        {
            return _context.Database.SqlQuery<int>(
                "SELECT CASE WHEN EXISTS (SELECT 1 FROM [dbo].[Portal_UserRoles] WHERE [UserID] = @p0 AND [RoleID] = @p1) THEN 1 ELSE 0 END",
                userId,
                roleId).Single() == 1;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>递增单个用户的安全版本；表缺失或用户无效时静默跳过以兼容旧数据库。</zh-CN>
        ///   <en>Increments one user's security version; missing tables or invalid users are skipped silently for legacy database compatibility.</en>
        /// </lang>
        /// </summary>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查用户安全版本表是否已部署。</zh-CN>
        ///   <en>Checks whether the user security-state table has been deployed.</en>
        /// </lang>
        /// </summary>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查角色权限映射表是否已部署。</zh-CN>
        ///   <en>Checks whether the role-permission mapping table has been deployed.</en>
        /// </lang>
        /// </summary>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>归一化安全版本变化原因，并限制最大长度。</zh-CN>
        ///   <en>Normalizes the security-version change reason and enforces the maximum length.</en>
        /// </lang>
        /// </summary>
        private static string NormalizeReason(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Unspecified";
            }

            string normalized = value.Trim();
            return normalized.Substring(0, Math.Min(normalized.Length, 100));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>归一化角色权限更新人标识。</zh-CN>
        ///   <en>Normalizes the role-permission updater identifier.</en>
        /// </lang>
        /// </summary>
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
