using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户角色、成员关系和用户查询的数据访问契约。
    ///
    /// English: Data-access contract for Portal roles, memberships, and user queries.
    /// </summary>
    public interface IRolesDb
    {
        /// <summary>
        /// 中文：获取指定门户已定义的全部角色。
        ///
        /// English: Gets all roles defined for the specified Portal.
        /// </summary>
        /// <param name="portalId">中文：门户数值标识。English: Numeric Portal identifier.</param>
        /// <returns>中文：门户角色集合。English: Collection of Portal roles.</returns>
        IEnumerable<IRoleItem> GetPortalRoles(int portalId);

        /// <summary>
        /// 中文：为指定门户创建角色。
        ///
        /// English: Creates a role for the specified Portal.
        /// </summary>
        /// <param name="portalId">中文：门户数值标识。English: Numeric Portal identifier.</param>
        /// <param name="roleName">中文：要创建的角色名称。English: Role name to create.</param>
        /// <returns>中文：新角色数值标识。English: Numeric identifier of the new role.</returns>
        int AddRole(int portalId, string roleName);

        /// <summary>
        /// 中文：删除指定角色定义；调用方必须先处理其成员关系和业务引用。
        ///
        /// English: Deletes the specified role definition; callers must handle its memberships and business references first.
        /// </summary>
        /// <param name="roleId">中文：要删除的角色数值标识。English: Numeric identifier of the role to delete.</param>
        void DeleteRole(int roleId);

        /// <summary>
        /// 中文：更新指定角色的显示名称。
        ///
        /// English: Updates the display name of the specified role.
        /// </summary>
        /// <param name="roleId">中文：要更新的角色数值标识。English: Numeric identifier of the role to update.</param>
        /// <param name="roleName">中文：新的角色名称。English: New role name.</param>
        void UpdateRole(int roleId, string roleName);

        /// <summary>
        /// 中文：获取指定角色的成员用户。
        ///
        /// English: Gets member users of the specified role.
        /// </summary>
        /// <param name="roleId">中文：角色数值标识。English: Numeric role identifier.</param>
        /// <returns>中文：角色成员用户集合。English: Collection of role member users.</returns>
        IEnumerable<IUserItem> GetRoleMembers(int roleId);

        /// <summary>
        /// 中文：将用户加入角色；现有会话中的角色 Cookie 不会立即失效。
        ///
        /// English: Adds a user to a role; existing role cookies are not invalidated immediately.
        /// </summary>
        /// <param name="roleId">中文：角色数值标识。English: Numeric role identifier.</param>
        /// <param name="userId">中文：用户数值标识。English: Numeric user identifier.</param>
        void AddUserRole(int roleId, int userId);

        /// <summary>
        /// 中文：从角色移除用户；现有会话中的角色 Cookie 不会立即失效。
        ///
        /// English: Removes a user from a role; existing role cookies are not invalidated immediately.
        /// </summary>
        /// <param name="roleId">中文：角色数值标识。English: Numeric role identifier.</param>
        /// <param name="userId">中文：用户数值标识。English: Numeric user identifier.</param>
        void DeleteUserRole(int roleId, int userId);

        /// <summary>
        /// 中文：获取可供旧后台管理界面选择的用户集合。
        ///
        /// English: Gets the user collection available to legacy administration UI.
        /// </summary>
        /// <returns>中文：用户集合。English: User collection.</returns>
        IEnumerable<IUserItem> GetUsers();
    }
}
