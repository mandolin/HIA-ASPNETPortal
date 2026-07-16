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
        /// 中文：将用户加入角色，并在可用时递增用户安全版本以便旧角色 Cookie 下次请求重新判定。
        ///
        /// English: Adds a user to a role and increments the user security version when available so older role
        /// cookies are re-evaluated on the next request.
        /// </summary>
        /// <param name="roleId">中文：角色数值标识。English: Numeric role identifier.</param>
        /// <param name="userId">中文：用户数值标识。English: Numeric user identifier.</param>
        void AddUserRole(int roleId, int userId);

        /// <summary>
        /// 中文：从角色移除用户，并在可用时递增用户安全版本以便旧角色 Cookie 下次请求重新判定。
        ///
        /// English: Removes a user from a role and increments the user security version when available so older role
        /// cookies are re-evaluated on the next request.
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

        /// <summary>
        /// 中文：读取指定用户通过当前角色映射获得的权限键。
        ///
        /// English: Gets permission keys granted to the specified user through current role mappings.
        /// </summary>
        /// <param name="name">中文：用户登录名或邮箱。English: User sign-in name or email.</param>
        /// <returns>中文：权限键集合；权限表缺失时返回空集合。English: Permission keys; empty when the permission table is unavailable.</returns>
        IEnumerable<string> GetPermissionKeysByUserName(string name);

        /// <summary>
        /// 中文：替换指定角色的权限映射，并递增该角色现有成员的安全版本。
        ///
        /// English: Replaces permission mappings for the specified role and increments security versions of current
        /// role members.
        /// </summary>
        /// <param name="roleId">中文：角色数值标识。English: Numeric role identifier.</param>
        /// <param name="permissionKeys">中文：新的权限键集合。English: New permission-key collection.</param>
        /// <param name="updatedBy">中文：执行更新的维护者标识。English: Maintainer identifier performing the update.</param>
        void SaveRolePermissions(int roleId, IEnumerable<string> permissionKeys, string updatedBy);
    }
}
