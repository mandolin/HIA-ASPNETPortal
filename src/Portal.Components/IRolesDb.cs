using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户角色、成员关系和用户查询的数据访问契约。</zh-CN>
    ///   <en>Data-access contract for Portal roles, memberships, and user queries.</en>
    /// </lang>
    /// </summary>
    public interface IRolesDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>获取指定门户已定义的全部角色。</zh-CN>
        ///   <en>Gets all roles defined for the specified Portal.</en>
        /// </lang>
        /// </summary>
        /// <param name="portalId">
        /// <l>
        ///   <zh-CN>门户数值标识。</zh-CN>
        ///   <en>Numeric Portal identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>门户角色集合。</zh-CN>
        ///   <en>Collection of Portal roles.</en>
        /// </l>
        /// </returns>
        IEnumerable<IRoleItem> GetPortalRoles(int portalId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>为指定门户创建角色。</zh-CN>
        ///   <en>Creates a role for the specified Portal.</en>
        /// </lang>
        /// </summary>
        /// <param name="portalId">
        /// <l>
        ///   <zh-CN>门户数值标识。</zh-CN>
        ///   <en>Numeric Portal identifier.</en>
        /// </l>
        /// </param>
        /// <param name="roleName">
        /// <l>
        ///   <zh-CN>要创建的角色名称。</zh-CN>
        ///   <en>Role name to create.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>新角色数值标识。</zh-CN>
        ///   <en>Numeric identifier of the new role.</en>
        /// </l>
        /// </returns>
        int AddRole(int portalId, string roleName);

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除指定角色定义；调用方必须先处理其成员关系和业务引用。</zh-CN>
        ///   <en>Deletes the specified role definition; callers must handle its memberships and business references first.</en>
        /// </lang>
        /// </summary>
        /// <param name="roleId">
        /// <l>
        ///   <zh-CN>要删除的角色数值标识。</zh-CN>
        ///   <en>Numeric identifier of the role to delete.</en>
        /// </l>
        /// </param>
        void DeleteRole(int roleId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新指定角色的显示名称。</zh-CN>
        ///   <en>Updates the display name of the specified role.</en>
        /// </lang>
        /// </summary>
        /// <param name="roleId">
        /// <l>
        ///   <zh-CN>要更新的角色数值标识。</zh-CN>
        ///   <en>Numeric identifier of the role to update.</en>
        /// </l>
        /// </param>
        /// <param name="roleName">
        /// <l>
        ///   <zh-CN>新的角色名称。</zh-CN>
        ///   <en>New role name.</en>
        /// </l>
        /// </param>
        void UpdateRole(int roleId, string roleName);

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取指定角色的成员用户。</zh-CN>
        ///   <en>Gets member users of the specified role.</en>
        /// </lang>
        /// </summary>
        /// <param name="roleId">
        /// <l>
        ///   <zh-CN>角色数值标识。</zh-CN>
        ///   <en>Numeric role identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>角色成员用户集合。</zh-CN>
        ///   <en>Collection of role member users.</en>
        /// </l>
        /// </returns>
        IEnumerable<IUserItem> GetRoleMembers(int roleId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>将用户加入角色，并在可用时递增用户安全版本以便旧角色 Cookie 下次请求重新判定。</zh-CN>
        ///   <en>Adds a user to a role and increments the user security version when available so older role cookies are re-evaluated on the next request.</en>
        /// </lang>
        /// </summary>
        /// <param name="roleId">
        /// <l>
        ///   <zh-CN>角色数值标识。</zh-CN>
        ///   <en>Numeric role identifier.</en>
        /// </l>
        /// </param>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>用户数值标识。</zh-CN>
        ///   <en>Numeric user identifier.</en>
        /// </l>
        /// </param>
        void AddUserRole(int roleId, int userId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>从角色移除用户，并在可用时递增用户安全版本以便旧角色 Cookie 下次请求重新判定。</zh-CN>
        ///   <en>Removes a user from a role and increments the user security version when available so older role cookies are re-evaluated on the next request.</en>
        /// </lang>
        /// </summary>
        /// <param name="roleId">
        /// <l>
        ///   <zh-CN>角色数值标识。</zh-CN>
        ///   <en>Numeric role identifier.</en>
        /// </l>
        /// </param>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>用户数值标识。</zh-CN>
        ///   <en>Numeric user identifier.</en>
        /// </l>
        /// </param>
        void DeleteUserRole(int roleId, int userId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取可供旧后台管理界面选择的用户集合。</zh-CN>
        ///   <en>Gets the user collection available to legacy administration UI.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>用户集合。</zh-CN>
        ///   <en>User collection.</en>
        /// </l>
        /// </returns>
        IEnumerable<IUserItem> GetUsers();

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定用户通过当前角色映射获得的权限键。</zh-CN>
        ///   <en>Gets permission keys granted to the specified user through current role mappings.</en>
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
        ///   <zh-CN>权限键集合；权限表缺失时返回空集合。</zh-CN>
        ///   <en>Permission keys; empty when the permission table is unavailable.</en>
        /// </l>
        /// </returns>
        IEnumerable<string> GetPermissionKeysByUserName(string name);

        /// <summary>
        /// <lang>
        ///   <zh-CN>替换指定角色的权限映射，并递增该角色现有成员的安全版本。</zh-CN>
        ///   <en>Replaces permission mappings for the specified role and increments security versions of current role members.</en>
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
        void SaveRolePermissions(int roleId, IEnumerable<string> permissionKeys, string updatedBy);
    }
}
