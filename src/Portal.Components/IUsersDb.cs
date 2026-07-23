using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户用户、登录、角色查询和注册审核的数据访问契约。</zh-CN>
    ///   <en>Data-access contract for Portal users, sign-in, role lookup, and registration review.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P5.2 起 <c>password</c> 参数表示一次性提交的密码输入，调用方不得记录、回显或自行生成摘要。 数据访问层负责强哈希写入、旧 MD5 兼容验证和安全版本维护。</zh-CN>
    ///   <en>Starting with P5.2, <c>password</c> parameters represent one-time submitted password input; callers must not log, echo, or pre-digest it. The data-access layer owns strong-hash writes, legacy MD5 compatibility verification, and security-version maintenance.</en>
    /// </lang>
    /// </remarks>
    public interface IUsersDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>添加由管理员或 legacy 流程创建的用户。</zh-CN>
        ///   <en>Adds a user created by an administrator or a legacy flow.</en>
        /// </lang>
        /// </summary>
        /// <param name="fullName">
        /// <l>
        ///   <zh-CN>用户登录名称。</zh-CN>
        ///   <en>User sign-in name.</en>
        /// </l>
        /// </param>
        /// <param name="email">
        /// <l>
        ///   <zh-CN>用户邮箱地址。</zh-CN>
        ///   <en>User email address.</en>
        /// </l>
        /// </param>
        /// <param name="password">
        /// <l>
        ///   <zh-CN>用户提交的密码输入；空值表示创建不可登录占位用户。</zh-CN>
        ///   <en>Submitted password input; empty means creating a non-signable placeholder user.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>成功时返回新用户标识；失败时返回负值。</zh-CN>
        ///   <en>New user identifier on success; a negative value on failure.</en>
        /// </l>
        /// </returns>
        int AddUser(String fullName, string email, string password);

        /// <summary>
        /// <lang>
        ///   <zh-CN>添加自主注册用户，并写入注册审核元数据。</zh-CN>
        ///   <en>Adds a self-registered user and writes registration-review metadata.</en>
        /// </lang>
        /// </summary>
        /// <param name="fullName">
        /// <l>
        ///   <zh-CN>用户登录名称。</zh-CN>
        ///   <en>User sign-in name.</en>
        /// </l>
        /// </param>
        /// <param name="email">
        /// <l>
        ///   <zh-CN>用户邮箱地址。</zh-CN>
        ///   <en>User email address.</en>
        /// </l>
        /// </param>
        /// <param name="password">
        /// <l>
        ///   <zh-CN>用户提交的密码输入。</zh-CN>
        ///   <en>Submitted password input.</en>
        /// </l>
        /// </param>
        /// <param name="employeeCode">
        /// <l>
        ///   <zh-CN>可为空的员工号。</zh-CN>
        ///   <en>Optional employee code.</en>
        /// </l>
        /// </param>
        /// <param name="inviteCode">
        /// <l>
        ///   <zh-CN>可为空的注册链接邀请码；空值表示当前允许的非邀请注册。</zh-CN>
        ///   <en>Optional registration invite code; an empty value represents currently allowed non-invite registration.</en>
        /// </l>
        /// </param>
        /// <param name="requiresApproval">
        /// <l>
        ///   <zh-CN>是否应以待审核状态创建。</zh-CN>
        ///   <en>Whether the user should be created in pending-approval status.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>成功时返回新用户标识；失败时返回负值。</zh-CN>
        ///   <en>New user identifier on success; a negative value on failure.</en>
        /// </l>
        /// </returns>
        int AddSelfRegisteredUser(
            string fullName,
            string email,
            string password,
            string employeeCode,
            string inviteCode,
            bool requiresApproval);

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除用户及可用的注册审核元数据。</zh-CN>
        ///   <en>Deletes a user and any available registration-review metadata.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>要删除的用户数值标识。</zh-CN>
        ///   <en>Numeric identifier of the user to delete.</en>
        /// </l>
        /// </param>
        void DeleteUser(int userId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新用户邮箱并重置用户凭据。</zh-CN>
        ///   <en>Updates a user's email and resets the user's credential.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>要更新的用户数值标识。</zh-CN>
        ///   <en>Numeric identifier of the user to update.</en>
        /// </l>
        /// </param>
        /// <param name="email">
        /// <l>
        ///   <zh-CN>新的邮箱地址。</zh-CN>
        ///   <en>New email address.</en>
        /// </l>
        /// </param>
        /// <param name="password">
        /// <l>
        ///   <zh-CN>新的密码输入，不得记录到审计正文。</zh-CN>
        ///   <en>New password input; it must not be recorded in audit details.</en>
        /// </l>
        /// </param>
        void UpdateUser(int userId, string email, string password);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取用户资料扩展；旧库应返回兼容视图而不是抛出缺表异常。</zh-CN>
        ///   <en>Reads the user-profile extension; legacy databases should return a compatibility view instead of throwing a missing-table exception.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>用户数值标识。</zh-CN>
        ///   <en>Numeric user identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>资料扩展只读视图。</zh-CN>
        ///   <en>Read-only profile extension view.</en>
        /// </l>
        /// </returns>
        IUserProfileInfo GetUserProfileInfo(int userId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新用户资料扩展、同步旧邮箱，并可选重置用户凭据。</zh-CN>
        ///   <en>Updates the user-profile extension, synchronizes the legacy email, and optionally resets the user's credential.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>要更新的用户数值标识。</zh-CN>
        ///   <en>Numeric identifier of the user to update.</en>
        /// </l>
        /// </param>
        /// <param name="loginName">
        /// <l>
        ///   <zh-CN>新的稳定登录名。</zh-CN>
        ///   <en>New stable login name.</en>
        /// </l>
        /// </param>
        /// <param name="displayName">
        /// <l>
        ///   <zh-CN>正式显示名。</zh-CN>
        ///   <en>Formal display name.</en>
        /// </l>
        /// </param>
        /// <param name="nickname">
        /// <l>
        ///   <zh-CN>昵称或偏好称呼。</zh-CN>
        ///   <en>Nickname or preferred name.</en>
        /// </l>
        /// </param>
        /// <param name="email">
        /// <l>
        ///   <zh-CN>新的邮箱地址。</zh-CN>
        ///   <en>New email address.</en>
        /// </l>
        /// </param>
        /// <param name="password">
        /// <l>
        ///   <zh-CN>可为空的新密码输入；为空时不重置凭据。</zh-CN>
        ///   <en>Optional new password input; empty means no credential reset.</en>
        /// </l>
        /// </param>
        /// <param name="actor">
        /// <l>
        ///   <zh-CN>执行更新的管理员标识。</zh-CN>
        ///   <en>Identifier of the administrator performing the update.</en>
        /// </l>
        /// </param>
        void UpdateUserProfile(
            int userId,
            string loginName,
            string displayName,
            string nickname,
            string email,
            string password,
            string actor);

        /// <summary>
        /// <lang>
        ///   <zh-CN>设置用户资料生命周期状态，并递增安全版本以使旧身份票据和角色 Cookie 在后续请求中失效或重新判定。</zh-CN>
        ///   <en>Sets the user-profile lifecycle status and increments the security version so older authentication tickets and role cookies expire or re-evaluate on later requests.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>要更新的用户数值标识。</zh-CN>
        ///   <en>Numeric identifier of the user to update.</en>
        /// </l>
        /// </param>
        /// <param name="status">
        /// <l>
        ///   <zh-CN>目标状态，必须来自 <see cref="PortalUserProfileStatuses"/>。</zh-CN>
        ///   <en>Target status, which must come from <see cref="PortalUserProfileStatuses"/>.</en>
        /// </l>
        /// </param>
        /// <param name="reason">
        /// <l>
        ///   <zh-CN>不含敏感值的状态变更原因。</zh-CN>
        ///   <en>Non-sensitive status-change reason.</en>
        /// </l>
        /// </param>
        /// <param name="actor">
        /// <l>
        ///   <zh-CN>执行状态变更的操作者标识。</zh-CN>
        ///   <en>Identifier of the operator performing the status change.</en>
        /// </l>
        /// </param>
        void SetUserProfileStatus(int userId, string status, string reason, string actor);

        /// <summary>
        /// <lang>
        ///   <zh-CN>批准用户注册，使其符合当前审核状态下的登录条件。</zh-CN>
        ///   <en>Approves a user registration so it meets the current review-status sign-in condition.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>要批准的用户数值标识。</zh-CN>
        ///   <en>Numeric identifier of the user to approve.</en>
        /// </l>
        /// </param>
        /// <param name="approvedBy">
        /// <l>
        ///   <zh-CN>批准操作人标识。</zh-CN>
        ///   <en>Approving operator identifier.</en>
        /// </l>
        /// </param>
        void ApproveUser(int userId, string approvedBy);

        /// <summary>
        /// <lang>
        ///   <zh-CN>拒绝处于待审核状态的用户注册。</zh-CN>
        ///   <en>Rejects a user registration that is pending approval.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>要拒绝的用户数值标识。</zh-CN>
        ///   <en>Numeric identifier of the user to reject.</en>
        /// </l>
        /// </param>
        /// <param name="rejectedBy">
        /// <l>
        ///   <zh-CN>拒绝操作人标识。</zh-CN>
        ///   <en>Rejecting operator identifier.</en>
        /// </l>
        /// </param>
        void RejectUser(int userId, string rejectedBy);

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取用户注册审核信息；旧用户或旧库应返回兼容视图。</zh-CN>
        ///   <en>Gets registration-review information for a user; legacy users or databases should return a compatible view.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>用户数值标识。</zh-CN>
        ///   <en>Numeric user identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>注册审核只读信息。</zh-CN>
        ///   <en>Read-only registration-review information.</en>
        /// </l>
        /// </returns>
        IUserRegistrationInfo GetRegistrationInfo(int userId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>校验临时注册链接邀请码；当前空邀请码为有效的非邀请注册。</zh-CN>
        ///   <en>Validates a temporary registration invite code; an empty code is currently valid for non-invite registration.</en>
        /// </lang>
        /// </summary>
        /// <param name="inviteCode">
        /// <l>
        ///   <zh-CN>可为空的邀请码。</zh-CN>
        ///   <en>Optional invite code.</en>
        /// </l>
        /// </param>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>校验失败时可展示的说明。</zh-CN>
        ///   <en>Display-safe message when validation fails.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>邀请码有效，或当前允许空邀请码时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the code is valid or an empty code is currently allowed.</en>
        /// </l>
        /// </returns>
        bool ValidateRegistrationInvite(string inviteCode, out string message);

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取用户所属角色。</zh-CN>
        ///   <en>Gets roles assigned to a user.</en>
        /// </lang>
        /// </summary>
        /// <param name="email">
        /// <l>
        ///   <zh-CN>既有接口使用的用户名称或邮箱标识。</zh-CN>
        ///   <en>User name or email identifier used by the legacy interface.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>角色集合；用户没有角色时应为空集合。</zh-CN>
        ///   <en>Role collection; should be empty when the user has no roles.</en>
        /// </l>
        /// </returns>
        IEnumerable<IRoleItem> GetRolesByUser(String email);

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取用户所属角色的名称，用于构造请求级角色身份。</zh-CN>
        ///   <en>Gets names of roles assigned to a user for constructing the request-level role identity.</en>
        /// </lang>
        /// </summary>
        /// <param name="email">
        /// <l>
        ///   <zh-CN>既有接口使用的用户名称或邮箱标识。</zh-CN>
        ///   <en>User name or email identifier used by the legacy interface.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>角色名称集合；用户没有角色时应为空集合。</zh-CN>
        ///   <en>Role-name collection; should be empty when the user has no roles.</en>
        /// </l>
        /// </returns>
        IEnumerable<string> GetRoleNamesByUser(String email);

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取单个用户。</zh-CN>
        ///   <en>Gets a single user.</en>
        /// </lang>
        /// </summary>
        /// <param name="email">
        /// <l>
        ///   <zh-CN>既有接口使用的用户名称或邮箱标识。</zh-CN>
        ///   <en>User name or email identifier used by the legacy interface.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配的用户。</zh-CN>
        ///   <en>Matching user.</en>
        /// </l>
        /// </returns>
        IUserItem GetSingleUser(String email);

        /// <summary>
        /// <lang>
        ///   <zh-CN>按数值标识查找单个用户；管理员页面可将其用于验证请求目标，缺失时正常返回空值。</zh-CN>
        ///   <en>Finds one user by numeric identifier; administration pages may use it to validate a request target, returning null normally when the user is absent.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>用户数值标识。</zh-CN>
        ///   <en>Numeric user identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配用户；不存在时为 <c>null</c>。</zh-CN>
        ///   <en>Matching user, or <c>null</c> when absent.</en>
        /// </l>
        /// </returns>
        IUserItem FindUserById(int userId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>校验登录密码和注册审核状态，成功时返回登录结果。</zh-CN>
        ///   <en>Validates a sign-in password and registration-review status, then returns the sign-in result on success.</en>
        /// </lang>
        /// </summary>
        /// <param name="emailOrName">
        /// <l>
        ///   <zh-CN>用户输入的邮箱、登录名称或员工号。</zh-CN>
        ///   <en>Email, sign-in name, or employee code entered by the user.</en>
        /// </l>
        /// </param>
        /// <param name="password">
        /// <l>
        ///   <zh-CN>用户提交的密码输入。</zh-CN>
        ///   <en>Submitted password input.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>登录结果；失败时使用通用失败对象。</zh-CN>
        ///   <en>Sign-in result; generic failure object on failure.</en>
        /// </l>
        /// </returns>
        PortalSignInResult SignIn(string emailOrName, string password);

        /// <summary>
        /// <lang>
        ///   <zh-CN>兼容旧调用点的登录方法；新代码应优先使用 <see cref="SignIn"/>。</zh-CN>
        ///   <en>Sign-in method retained for legacy call sites; new code should prefer <see cref="SignIn"/>.</en>
        /// </lang>
        /// </summary>
        /// <param name="emailOrName">
        /// <l>
        ///   <zh-CN>用户输入的邮箱、登录名称或员工号。</zh-CN>
        ///   <en>Email, sign-in name, or employee code entered by the user.</en>
        /// </l>
        /// </param>
        /// <param name="password">
        /// <l>
        ///   <zh-CN>用户提交的密码输入。</zh-CN>
        ///   <en>Submitted password input.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>登录成功且允许访问时返回用户名；否则为空字符串。</zh-CN>
        ///   <en>User name when sign-in succeeds and access is allowed; otherwise an empty string.</en>
        /// </l>
        /// </returns>
        string Login(String emailOrName, string password);

        /// <summary>
        /// <lang>
        ///   <zh-CN>按用户名称读取当前安全版本；缺少 P5.2 表时返回 <c>0</c>。</zh-CN>
        ///   <en>Reads the current security version by user name; returns <c>0</c> when P5.2 tables are absent.</en>
        /// </lang>
        /// </summary>
        /// <param name="userName">
        /// <l>
        ///   <zh-CN>用户登录名称。</zh-CN>
        ///   <en>User sign-in name.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前安全版本。</zh-CN>
        ///   <en>Current security version.</en>
        /// </l>
        /// </returns>
        long GetSecurityVersionByUserName(string userName);

        /// <summary>
        /// <lang>
        ///   <zh-CN>按用户标识读取当前安全版本；缺少 P5.2 表时返回 <c>0</c>。</zh-CN>
        ///   <en>Reads the current security version by user id; returns <c>0</c> when P5.2 tables are absent.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>用户数值标识。</zh-CN>
        ///   <en>Numeric user identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前安全版本。</zh-CN>
        ///   <en>Current security version.</en>
        /// </l>
        /// </returns>
        long GetSecurityVersionByUserId(int userId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>递增指定用户的安全版本，使旧身份票据和角色 Cookie 在下一次请求失效。</zh-CN>
        ///   <en>Increments the security version of the specified user so older auth tickets and role cookies become invalid on the next request.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>用户数值标识。</zh-CN>
        ///   <en>Numeric user identifier.</en>
        /// </l>
        /// </param>
        /// <param name="reason">
        /// <l>
        ///   <zh-CN>非敏感变更原因。</zh-CN>
        ///   <en>Non-sensitive change reason.</en>
        /// </l>
        /// </param>
        void IncrementSecurityVersion(int userId, string reason);
    }
}
