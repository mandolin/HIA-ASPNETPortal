using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户用户、登录、角色查询和注册审核的数据访问契约。
    ///
    /// English: Data-access contract for Portal users, sign-in, role lookup, and registration review.
    /// </summary>
    /// <remarks>
    /// 中文：P5.2 起 <c>password</c> 参数表示一次性提交的密码输入，调用方不得记录、回显或自行生成摘要。
    /// 数据访问层负责强哈希写入、旧 MD5 兼容验证和安全版本维护。
    ///
    /// English: Starting with P5.2, <c>password</c> parameters represent one-time submitted password input; callers
    /// must not log, echo, or pre-digest it. The data-access layer owns strong-hash writes, legacy MD5 compatibility
    /// verification, and security-version maintenance.
    /// </remarks>
    public interface IUsersDb
    {
        /// <summary>
        /// 中文：添加由管理员或 legacy 流程创建的用户。
        ///
        /// English: Adds a user created by an administrator or a legacy flow.
        /// </summary>
        /// <param name="fullName">中文：用户登录名称。English: User sign-in name.</param>
        /// <param name="email">中文：用户邮箱地址。English: User email address.</param>
        /// <param name="password">中文：用户提交的密码输入；空值表示创建不可登录占位用户。English: Submitted password input; empty means creating a non-signable placeholder user.</param>
        /// <returns>中文：成功时返回新用户标识；失败时返回负值。English: New user identifier on success; a negative value on failure.</returns>
        int AddUser(String fullName, string email, string password);

        /// <summary>
        /// 中文：添加自主注册用户，并写入注册审核元数据。
        ///
        /// English: Adds a self-registered user and writes registration-review metadata.
        /// </summary>
        /// <param name="fullName">中文：用户登录名称。English: User sign-in name.</param>
        /// <param name="email">中文：用户邮箱地址。English: User email address.</param>
        /// <param name="password">中文：用户提交的密码输入。English: Submitted password input.</param>
        /// <param name="employeeCode">中文：可为空的员工号。English: Optional employee code.</param>
        /// <param name="inviteCode">中文：可为空的注册链接邀请码；空值表示当前允许的非邀请注册。English: Optional registration invite code; an empty value represents currently allowed non-invite registration.</param>
        /// <param name="requiresApproval">中文：是否应以待审核状态创建。English: Whether the user should be created in pending-approval status.</param>
        /// <returns>中文：成功时返回新用户标识；失败时返回负值。English: New user identifier on success; a negative value on failure.</returns>
        int AddSelfRegisteredUser(
            string fullName,
            string email,
            string password,
            string employeeCode,
            string inviteCode,
            bool requiresApproval);

        /// <summary>
        /// 中文：删除用户及可用的注册审核元数据。
        ///
        /// English: Deletes a user and any available registration-review metadata.
        /// </summary>
        /// <param name="userId">中文：要删除的用户数值标识。English: Numeric identifier of the user to delete.</param>
        void DeleteUser(int userId);

        /// <summary>
        /// 中文：更新用户邮箱并重置用户凭据。
        ///
        /// English: Updates a user's email and resets the user's credential.
        /// </summary>
        /// <param name="userId">中文：要更新的用户数值标识。English: Numeric identifier of the user to update.</param>
        /// <param name="email">中文：新的邮箱地址。English: New email address.</param>
        /// <param name="password">中文：新的密码输入，不得记录到审计正文。English: New password input; it must not be recorded in audit details.</param>
        void UpdateUser(int userId, string email, string password);

        /// <summary>
        /// 中文：批准用户注册，使其符合当前审核状态下的登录条件。
        ///
        /// English: Approves a user registration so it meets the current review-status sign-in condition.
        /// </summary>
        /// <param name="userId">中文：要批准的用户数值标识。English: Numeric identifier of the user to approve.</param>
        /// <param name="approvedBy">中文：批准操作人标识。English: Approving operator identifier.</param>
        void ApproveUser(int userId, string approvedBy);

        /// <summary>
        /// 中文：拒绝处于待审核状态的用户注册。
        ///
        /// English: Rejects a user registration that is pending approval.
        /// </summary>
        /// <param name="userId">中文：要拒绝的用户数值标识。English: Numeric identifier of the user to reject.</param>
        /// <param name="rejectedBy">中文：拒绝操作人标识。English: Rejecting operator identifier.</param>
        void RejectUser(int userId, string rejectedBy);

        /// <summary>
        /// 中文：获取用户注册审核信息；旧用户或旧库应返回兼容视图。
        ///
        /// English: Gets registration-review information for a user; legacy users or databases should return a compatible view.
        /// </summary>
        /// <param name="userId">中文：用户数值标识。English: Numeric user identifier.</param>
        /// <returns>中文：注册审核只读信息。English: Read-only registration-review information.</returns>
        IUserRegistrationInfo GetRegistrationInfo(int userId);

        /// <summary>
        /// 中文：校验临时注册链接邀请码；当前空邀请码为有效的非邀请注册。
        ///
        /// English: Validates a temporary registration invite code; an empty code is currently valid for non-invite registration.
        /// </summary>
        /// <param name="inviteCode">中文：可为空的邀请码。English: Optional invite code.</param>
        /// <param name="message">中文：校验失败时可展示的说明。English: Display-safe message when validation fails.</param>
        /// <returns>中文：邀请码有效，或当前允许空邀请码时为 <c>true</c>。English: <c>true</c> when the code is valid or an empty code is currently allowed.</returns>
        bool ValidateRegistrationInvite(string inviteCode, out string message);

        /// <summary>
        /// 中文：获取用户所属角色。
        ///
        /// English: Gets roles assigned to a user.
        /// </summary>
        /// <param name="email">中文：既有接口使用的用户名称或邮箱标识。English: User name or email identifier used by the legacy interface.</param>
        /// <returns>中文：角色集合；用户没有角色时应为空集合。English: Role collection; should be empty when the user has no roles.</returns>
        IEnumerable<IRoleItem> GetRolesByUser(String email);

        /// <summary>
        /// 中文：获取用户所属角色的名称，用于构造请求级角色身份。
        ///
        /// English: Gets names of roles assigned to a user for constructing the request-level role identity.
        /// </summary>
        /// <param name="email">中文：既有接口使用的用户名称或邮箱标识。English: User name or email identifier used by the legacy interface.</param>
        /// <returns>中文：角色名称集合；用户没有角色时应为空集合。English: Role-name collection; should be empty when the user has no roles.</returns>
        IEnumerable<string> GetRoleNamesByUser(String email);

        /// <summary>
        /// 中文：获取单个用户。
        ///
        /// English: Gets a single user.
        /// </summary>
        /// <param name="email">中文：既有接口使用的用户名称或邮箱标识。English: User name or email identifier used by the legacy interface.</param>
        /// <returns>中文：匹配的用户。English: Matching user.</returns>
        IUserItem GetSingleUser(String email);

        /// <summary>
        /// 中文：按数值标识查找单个用户；管理员页面可将其用于验证请求目标，缺失时正常返回空值。
        ///
        /// English: Finds one user by numeric identifier; administration pages may use it to validate a request target,
        /// returning null normally when the user is absent.
        /// </summary>
        /// <param name="userId">中文：用户数值标识。English: Numeric user identifier.</param>
        /// <returns>中文：匹配用户；不存在时为 <c>null</c>。English: Matching user, or <c>null</c> when absent.</returns>
        IUserItem FindUserById(int userId);

        /// <summary>
        /// 中文：校验登录密码和注册审核状态，成功时返回登录结果。
        ///
        /// English: Validates a sign-in password and registration-review status, then returns the sign-in result on success.
        /// </summary>
        /// <param name="emailOrName">中文：用户输入的邮箱或登录名称。English: Email or sign-in name entered by the user.</param>
        /// <param name="password">中文：用户提交的密码输入。English: Submitted password input.</param>
        /// <returns>中文：登录结果；失败时使用通用失败对象。English: Sign-in result; generic failure object on failure.</returns>
        PortalSignInResult SignIn(string emailOrName, string password);

        /// <summary>
        /// 中文：兼容旧调用点的登录方法；新代码应优先使用 <see cref="SignIn"/>。
        ///
        /// English: Sign-in method retained for legacy call sites; new code should prefer <see cref="SignIn"/>.
        /// </summary>
        /// <param name="emailOrName">中文：用户输入的邮箱或登录名称。English: Email or sign-in name entered by the user.</param>
        /// <param name="password">中文：用户提交的密码输入。English: Submitted password input.</param>
        /// <returns>中文：登录成功且允许访问时返回用户名；否则为空字符串。English: User name when sign-in succeeds and access is allowed; otherwise an empty string.</returns>
        string Login(String emailOrName, string password);

        /// <summary>
        /// 中文：按用户名称读取当前安全版本；缺少 P5.2 表时返回 <c>0</c>。
        ///
        /// English: Reads the current security version by user name; returns <c>0</c> when P5.2 tables are absent.
        /// </summary>
        /// <param name="userName">中文：用户登录名称。English: User sign-in name.</param>
        /// <returns>中文：当前安全版本。English: Current security version.</returns>
        long GetSecurityVersionByUserName(string userName);

        /// <summary>
        /// 中文：按用户标识读取当前安全版本；缺少 P5.2 表时返回 <c>0</c>。
        ///
        /// English: Reads the current security version by user id; returns <c>0</c> when P5.2 tables are absent.
        /// </summary>
        /// <param name="userId">中文：用户数值标识。English: Numeric user identifier.</param>
        /// <returns>中文：当前安全版本。English: Current security version.</returns>
        long GetSecurityVersionByUserId(int userId);

        /// <summary>
        /// 中文：递增指定用户的安全版本，使旧身份票据和角色 Cookie 在下一次请求失效。
        ///
        /// English: Increments the security version of the specified user so older auth tickets and role cookies become invalid on the next request.
        /// </summary>
        /// <param name="userId">中文：用户数值标识。English: Numeric user identifier.</param>
        /// <param name="reason">中文：非敏感变更原因。English: Non-sensitive change reason.</param>
        void IncrementSecurityVersion(int userId, string reason);
    }
}
