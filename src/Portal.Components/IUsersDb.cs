using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    public interface IUsersDb
    {
        /// <summary>
        /// 添加管理员或 legacy 流程创建的用户。
        /// Adds a user created by admin or legacy flow.
        /// </summary>
        int AddUser(String fullName, string email, string password);

        /// <summary>
        /// 添加自主注册用户，并写入注册审核元数据。
        /// Adds a self-registered user and writes registration review metadata.
        /// </summary>
        int AddSelfRegisteredUser(
            string fullName,
            string email,
            string password,
            string employeeCode,
            string inviteCode,
            bool requiresApproval);

        /// <summary>
        /// 删除用户。
        /// Deletes a user.
        /// </summary>
        void DeleteUser(int userId);

        /// <summary>
        /// 更新用户邮箱和密码。
        /// Updates user email and password.
        /// </summary>
        void UpdateUser(int userId, string email, string password);

        /// <summary>
        /// 批准用户注册。
        /// Approves a user registration.
        /// </summary>
        void ApproveUser(int userId, string approvedBy);

        /// <summary>
        /// 获取用户注册审核信息。
        /// Gets registration review information for a user.
        /// </summary>
        IUserRegistrationInfo GetRegistrationInfo(int userId);

        /// <summary>
        /// 校验临时注册链接。
        /// Validates a temporary registration invitation code.
        /// </summary>
        bool ValidateRegistrationInvite(string inviteCode, out string message);

        /// <summary>
        /// 获取用户所属角色。
        /// Gets roles assigned to a user.
        /// </summary>
        IEnumerable<IRoleItem> GetRolesByUser(String email);

        /// <summary>
        /// 获取用户所属角色名称。
        /// Gets role names assigned to a user.
        /// </summary>
        IEnumerable<string> GetRoleNamesByUser(String email);

        /// <summary>
        /// 获取单个用户。
        /// Gets a single user.
        /// </summary>
        IUserItem GetSingleUser(String email);

        /// <summary>
        /// 登录校验，返回成功登录的用户名。
        /// Checks sign-in and returns the signed-in user name.
        /// </summary>
        string Login(String emailOrName, string password);
    }
}
