using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：基于门户安全数据库上下文的用户、凭据和注册审核数据访问实现。
    ///
    /// English: User, credential, and registration-review data-access implementation backed by the Portal security database context.
    /// </summary>
    /// <remarks>
    /// 中文：P5.2 起，新建、注册和重置密码写入 <c>Portal_UserCredentials</c> 强哈希表，并通过
    /// <c>Portal_UserSecurityStates</c> 维护会话安全版本。旧 <c>Portal_Users.Password</c> 中的 MD5
    /// 仅用于尚未升级用户的首次兼容登录；一旦强哈希凭据存在，登录不得再回退到旧字段。
    ///
    /// English: Starting with P5.2, newly created, registered, and reset passwords are written to the
    /// <c>Portal_UserCredentials</c> strong-hash table, while <c>Portal_UserSecurityStates</c> maintains the
    /// session security version. Legacy MD5 values in <c>Portal_Users.Password</c> are used only for the first
    /// compatibility sign-in of users that have not yet been upgraded; once a strong credential exists, sign-in
    /// must not fall back to the legacy column.
    /// </remarks>
    public class UsersDb : IUsersDb
    {
        private const long InitialSecurityVersion = 1;
        private const string CredentialTableName = "Portal_UserCredentials";
        private const string SecurityStateTableName = "Portal_UserSecurityStates";
        private readonly PortalSecurityDbContext _context;

        /// <summary>
        /// 中文：初始化用户、凭据和注册审核数据访问实现。
        ///
        /// English: Initializes the user, credential, and registration-review data-access implementation.
        /// </summary>
        /// <param name="context">中文：门户安全数据库上下文。English: Portal security database context.</param>
        public UsersDb(PortalSecurityDbContext context)
        {
            _context = context;
        }

        #region IUsersDb Members

        /// <summary>
        /// 中文：添加由管理员或既有流程创建的用户；非空密码写入强哈希凭据表。
        ///
        /// English: Adds a user created by an administrator or an existing legacy flow; non-empty passwords are written to the strong-hash credential table.
        /// </summary>
        /// <param name="fullName">中文：用户登录名称。English: User sign-in name.</param>
        /// <param name="email">中文：用户邮箱地址。English: User email address.</param>
        /// <param name="password">中文：用户提交的密码输入；空值表示创建不可登录占位用户。English: Submitted password input; empty creates a non-signable placeholder user.</param>
        /// <returns>中文：成功时返回新用户标识；预期写入失败时返回 <c>-1</c>。English: New user identifier on success; <c>-1</c> for expected write failures.</returns>
        public int AddUser(string fullName, string email, string password)
        {
            string normalizedPassword = password ?? string.Empty;
            if (!string.IsNullOrEmpty(normalizedPassword) && !HasCredentialTables())
            {
                return -1;
            }

            try
            {
                UserItem item;
                using (var transaction = _context.Database.BeginTransaction())
                {
                    item = new UserItem
                    {
                        Name = fullName,
                        Email = email,
                        // 中文：新用户不再生成 MD5；旧字段只保留既有数据的迁移样本。
                        // English: New users no longer receive MD5; the legacy column remains only for existing data migration samples.
                        Password = string.Empty
                    };

                    _context.Users.Add(item);
                    _context.SaveChanges();

                    EnsureSecurityState(item.UserId, "UserCreated");
                    if (!string.IsNullOrEmpty(normalizedPassword))
                    {
                        UpsertCredential(item.UserId, normalizedPassword, null, null, false, null);
                    }

                    _context.SaveChanges();
                    transaction.Commit();
                }

                // 中文：管理员或旧入口创建的用户默认已批准；缺少扩展表时保持旧行为。
                // English: Users created by administration or legacy entry points are approved by default; missing extension tables preserve legacy behavior.
                TryEnsureApprovedRegistration(item.UserId, "system-admin");
                return item.UserId;
            }
            catch (Exception)
            {
                // 中文：保持旧接口约定，预期写入失败用 -1 表达，不在此记录敏感输入。
                // English: Preserve the legacy interface contract: expected write failures return -1 and do not log sensitive input here.
                return -1;
            }
        }

        /// <summary>
        /// 中文：添加自主注册用户，并在同一事务写入强哈希凭据、注册审核元数据和邀请码使用次数。
        ///
        /// English: Adds a self-registered user and writes the strong-hash credential, registration-review metadata, and invite usage in one transaction.
        /// </summary>
        /// <param name="fullName">中文：用户登录名称。English: User sign-in name.</param>
        /// <param name="email">中文：用户邮箱地址。English: User email address.</param>
        /// <param name="password">中文：用户提交的密码输入。English: Submitted password input.</param>
        /// <param name="employeeCode">中文：可为空的员工号。English: Optional employee code.</param>
        /// <param name="inviteCode">中文：可为空的邀请码；空值代表当前允许的非邀请注册。English: Optional invite code; an empty value represents currently allowed non-invite registration.</param>
        /// <param name="requiresApproval">中文：是否以待审核状态创建用户。English: Whether to create the user in pending-approval status.</param>
        /// <returns>中文：成功时返回新用户标识；审核表、凭据表不可用或邀请码无效时返回 <c>-1</c>。English: New user identifier on success; <c>-1</c> when review tables, credential tables, or invites are unavailable.</returns>
        public int AddSelfRegisteredUser(
            string fullName,
            string email,
            string password,
            string employeeCode,
            string inviteCode,
            bool requiresApproval)
        {
            if (!HasRegistrationTable() || !HasCredentialTables())
            {
                return -1;
            }

            string normalizedInviteCode = Normalize(inviteCode);
            string inviteMessage;
            if (!ValidateRegistrationInvite(normalizedInviteCode, out inviteMessage))
            {
                return -1;
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var item = new UserItem
                    {
                        Name = fullName,
                        Email = email,
                        Password = string.Empty
                    };

                    _context.Users.Add(item);
                    _context.SaveChanges();

                    EnsureSecurityState(item.UserId, "SelfRegistration");
                    UpsertCredential(item.UserId, password, null, null, false, null);

                    // 中文：所有注册审核时间统一以 UTC 保存。
                    // English: Store all registration-review timestamps in UTC.
                    DateTime nowUtc = DateTime.UtcNow;
                    _context.UserRegistrations.Add(new UserRegistrationItem
                    {
                        UserId = item.UserId,
                        Status = requiresApproval
                            ? PortalUserRegistrationStatuses.PendingApproval
                            : PortalUserRegistrationStatuses.Approved,
                        RequiresApproval = requiresApproval,
                        EmployeeCode = NullIfEmpty(employeeCode),
                        InviteCode = NullIfEmpty(normalizedInviteCode),
                        RegisteredUtc = nowUtc,
                        ApprovedUtc = requiresApproval ? (DateTime?)null : nowUtc,
                        ApprovedBy = requiresApproval ? null : "system-self-registration"
                    });

                    // 中文：空邀请码不会增加使用次数；有效邀请码的计数与用户创建同一事务提交。
                    // English: Empty invite codes do not increment usage; valid invite counts commit in the same transaction as user creation.
                    IncrementInviteUsage(normalizedInviteCode);
                    _context.SaveChanges();
                    transaction.Commit();

                    return item.UserId;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// 中文：删除指定用户及可用的注册审核元数据。
        ///
        /// English: Deletes the specified user and available registration-review metadata.
        /// </summary>
        /// <param name="userId">中文：要删除的用户数值标识。English: Numeric identifier of the user to delete.</param>
        public void DeleteUser(int userId)
        {
            // 中文：先清理扩展元数据，避免显式 FK 部署阻止既有删除流程。
            // English: Clean up extension metadata first so explicit FK deployments do not block the legacy delete flow.
            TryDeleteRegistration(userId);

            var item = _context.Users.Single(i => i.UserId == userId);

            _context.Users.Remove(item);
            _context.SaveChanges();
        }

        /// <summary>
        /// 中文：批准用户注册，并恢复已拒绝记录的登录资格。
        ///
        /// English: Approves a user registration and restores sign-in eligibility for a previously rejected record.
        /// </summary>
        /// <param name="userId">中文：要批准的用户数值标识。English: Numeric identifier of the user to approve.</param>
        /// <param name="approvedBy">中文：批准操作人标识。English: Approving operator identifier.</param>
        /// <exception cref="InvalidOperationException">中文：注册审核表不可用时引发。English: Thrown when the registration-review table is unavailable.</exception>
        public void ApproveUser(int userId, string approvedBy)
        {
            if (!HasRegistrationTable())
            {
                throw new InvalidOperationException("Registration metadata table is not available.");
            }

            DateTime nowUtc = DateTime.UtcNow;
            var registration = _context.UserRegistrations.SingleOrDefault(i => i.UserId == userId);
            if (registration == null)
            {
                _context.UserRegistrations.Add(new UserRegistrationItem
                {
                    UserId = userId,
                    Status = PortalUserRegistrationStatuses.Approved,
                    RequiresApproval = false,
                    RegisteredUtc = nowUtc,
                    ApprovedUtc = nowUtc,
                    ApprovedBy = Normalize(approvedBy)
                });
            }
            else
            {
                registration.Status = PortalUserRegistrationStatuses.Approved;
                registration.ApprovedUtc = nowUtc;
                registration.ApprovedBy = Normalize(approvedBy);
                registration.RejectedUtc = null;
                registration.RejectedBy = null;
                registration.ReviewNote = null;
            }

            _context.SaveChanges();
            IncrementSecurityVersion(userId, "RegistrationApproved");
        }

        /// <summary>
        /// 中文：拒绝处于待审核状态的用户注册；管理员后续仍可重新批准。
        ///
        /// English: Rejects a pending user registration; an administrator may approve it later.
        /// </summary>
        /// <param name="userId">中文：要拒绝的用户数值标识。English: Numeric identifier of the user to reject.</param>
        /// <param name="rejectedBy">中文：拒绝操作人标识。English: Rejecting operator identifier.</param>
        /// <exception cref="InvalidOperationException">中文：注册审核表或用户审核记录不可用时引发。English: Thrown when the registration-review table or user review record is unavailable.</exception>
        public void RejectUser(int userId, string rejectedBy)
        {
            if (!HasRegistrationTable())
            {
                throw new InvalidOperationException("Registration metadata table is not available.");
            }

            var registration = _context.UserRegistrations.SingleOrDefault(i => i.UserId == userId);
            if (registration == null)
            {
                throw new InvalidOperationException("Registration metadata does not exist for this user.");
            }

            registration.Status = PortalUserRegistrationStatuses.Rejected;
            registration.RejectedUtc = DateTime.UtcNow;
            registration.RejectedBy = Normalize(rejectedBy);
            registration.ApprovedUtc = null;
            registration.ApprovedBy = null;
            registration.ReviewNote = null;
            _context.SaveChanges();
            IncrementSecurityVersion(userId, "RegistrationRejected");
        }

        /// <summary>
        /// 中文：读取用户注册审核信息；旧库或旧用户按已批准兼容视图展示。
        ///
        /// English: Reads user registration-review information; legacy databases or users appear through an approved compatibility view.
        /// </summary>
        /// <param name="userId">中文：用户数值标识。English: Numeric user identifier.</param>
        /// <returns>中文：注册审核只读信息，包含信息来源。English: Read-only registration-review information including its source.</returns>
        public IUserRegistrationInfo GetRegistrationInfo(int userId)
        {
            if (!HasRegistrationTable())
            {
                return CreateLegacyRegistrationInfo(userId, "LegacyNoTable");
            }

            var registration = _context.UserRegistrations.SingleOrDefault(i => i.UserId == userId);
            if (registration == null)
            {
                return CreateLegacyRegistrationInfo(userId, "LegacyNoRecord");
            }

            return new UserRegistrationInfo(
                registration.UserId,
                registration.Status,
                registration.RequiresApproval,
                registration.EmployeeCode,
                registration.InviteCode,
                registration.RegisteredUtc,
                registration.ApprovedUtc,
                registration.ApprovedBy,
                registration.RejectedUtc,
                registration.RejectedBy,
                registration.ReviewNote,
                "RegistrationMetadata");
        }

        /// <summary>
        /// 中文：校验临时注册链接邀请码。
        ///
        /// English: Validates a temporary registration invite code.
        /// </summary>
        /// <param name="inviteCode">中文：可为空的邀请码。English: Optional invite code.</param>
        /// <param name="message">中文：失败时可安全展示的说明；成功时为空。English: Display-safe explanation on failure; empty on success.</param>
        /// <returns>中文：邀请码有效，或当前允许空邀请码时为 <c>true</c>。English: <c>true</c> when the code is valid or an empty code is currently allowed.</returns>
        public bool ValidateRegistrationInvite(string inviteCode, out string message)
        {
            // 中文：空邀请码当前表示允许的非邀请注册，未来“必须邀请码”策略应作为独立设置实现。
            // English: An empty invite code currently represents allowed non-invite registration; a required-invite policy must be implemented as a separate setting.
            string normalizedInviteCode = Normalize(inviteCode);
            if (string.IsNullOrEmpty(normalizedInviteCode))
            {
                message = string.Empty;
                return true;
            }

            if (!HasInviteTable())
            {
                message = "注册链接数据表尚未部署。";
                return false;
            }

            var invite = _context.RegistrationInvites.SingleOrDefault(i => i.InviteCode == normalizedInviteCode);
            if (invite == null)
            {
                message = "注册链接不存在。";
                return false;
            }

            if (!invite.IsEnabled)
            {
                message = "注册链接已停用。";
                return false;
            }

            if (invite.ExpiresUtc < DateTime.UtcNow)
            {
                message = "注册链接已过期。";
                return false;
            }

            if (invite.MaxUses.HasValue && invite.UsedCount >= invite.MaxUses.Value)
            {
                message = "注册链接已达到使用次数上限。";
                return false;
            }

            message = string.Empty;
            return true;
        }

        /// <summary>
        /// 中文：更新指定用户的邮箱并重置强哈希凭据。
        ///
        /// English: Updates the specified user's email and resets the strong-hash credential.
        /// </summary>
        /// <param name="userId">中文：要更新的用户数值标识。English: Numeric identifier of the user to update.</param>
        /// <param name="email">中文：新的邮箱地址。English: New email address.</param>
        /// <param name="password">中文：新的密码输入，不得写入审计正文或诊断日志。English: New password input; it must not be written to audit details or diagnostic logs.</param>
        public void UpdateUser(int userId, string email, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("A password is required to reset user credentials.");
            }

            if (!HasCredentialTables())
            {
                throw new InvalidOperationException("Credential metadata tables are not available.");
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                var item = _context.Users.Single(i => i.UserId == userId);

                item.Email = email;
                item.Password = string.Empty;
                UpsertCredential(userId, password, null, null, false, null);
                _context.SaveChanges();
                IncrementSecurityVersion(userId, "CredentialReset");
                transaction.Commit();
            }
        }

        /// <summary>
        /// 中文：获取指定用户的所有角色。
        ///
        /// English: Gets all roles of the specified user.
        /// </summary>
        /// <param name="name">中文：用户登录名称。English: User sign-in name.</param>
        /// <returns>中文：角色集合；没有角色时为空集合。English: Role collection; empty when the user has no roles.</returns>
        public IEnumerable<IRoleItem> GetRolesByUser(string name)
        {
            // 中文：角色成员关系可能由 RolesDb 通过中间表直接写入；显式查询可避开 singleton DbContext 的导航集合缓存。
            // English: Role memberships may be written directly through the join table by RolesDb; explicit querying avoids stale navigation collections on the singleton DbContext.
            return _context.Database.SqlQuery<RoleItem>(
                @"
SELECT [Roles].[RoleID], [Roles].[PortalID], [Roles].[RoleName]
FROM [dbo].[Portal_Roles] AS [Roles]
INNER JOIN [dbo].[Portal_UserRoles] AS [UserRoles]
    ON [UserRoles].[RoleID] = [Roles].[RoleID]
INNER JOIN [dbo].[Portal_Users] AS [Users]
    ON [Users].[UserID] = [UserRoles].[UserID]
WHERE [Users].[Name] = @p0 OR [Users].[Email] = @p0
ORDER BY [Roles].[RoleName]",
                name).ToList<IRoleItem>();
        }

        /// <summary>
        /// 中文：获取指定用户的所有角色名称。
        ///
        /// English: Gets names of all roles of the specified user.
        /// </summary>
        /// <param name="name">中文：用户登录名称。English: User sign-in name.</param>
        /// <returns>中文：角色名称集合；没有角色时为空集合。English: Role-name collection; empty when the user has no roles.</returns>
        public IEnumerable<string> GetRoleNamesByUser(string name)
        {
            return _context.Database.SqlQuery<string>(
                @"
SELECT [Roles].[RoleName]
FROM [dbo].[Portal_Roles] AS [Roles]
INNER JOIN [dbo].[Portal_UserRoles] AS [UserRoles]
    ON [UserRoles].[RoleID] = [Roles].[RoleID]
INNER JOIN [dbo].[Portal_Users] AS [Users]
    ON [Users].[UserID] = [UserRoles].[UserID]
WHERE [Users].[Name] = @p0 OR [Users].[Email] = @p0
ORDER BY [Roles].[RoleName]",
                name).ToList();
        }

        /// <summary>
        /// 中文：获取单个用户。
        ///
        /// English: Gets a single user.
        /// </summary>
        /// <param name="name">中文：用户登录名称。English: User sign-in name.</param>
        /// <returns>中文：匹配的用户对象。English: Matching user object.</returns>
        public IUserItem GetSingleUser(string name)
        {
            return _context.Users.Single(i => i.Name == name);
        }

        /// <summary>
        /// 中文：按数值标识查找用户；页面请求目标缺失时返回空值而不是抛出查询异常。
        ///
        /// English: Finds a user by numeric identifier, returning null instead of throwing when a page-request target is absent.
        /// </summary>
        /// <param name="userId">中文：用户数值标识。English: Numeric user identifier.</param>
        /// <returns>中文：匹配用户；不存在时为 <c>null</c>。English: Matching user, or <c>null</c> when absent.</returns>
        public IUserItem FindUserById(int userId)
        {
            return _context.Users.SingleOrDefault(i => i.UserId == userId);
        }

        /// <summary>
        /// 中文：校验登录输入密码、注册审核状态和 P5.2 强凭据。
        ///
        /// English: Validates the submitted password, registration-review status, and P5.2 strong credential.
        /// </summary>
        /// <param name="emailOrName">中文：用户输入的邮箱或登录名称。English: Email or sign-in name entered by the user.</param>
        /// <param name="password">中文：用户提交的密码输入。English: Submitted password input.</param>
        /// <returns>中文：登录结果；失败时为通用失败对象。English: Sign-in result; generic failure object on failure.</returns>
        public PortalSignInResult SignIn(string emailOrName, string password)
        {
            string normalizedLogin = Normalize(emailOrName);
            if (string.IsNullOrEmpty(normalizedLogin) || password == null)
            {
                return PortalSignInResult.Failed();
            }

            var item = _context.Users
                .AsNoTracking()
                .SingleOrDefault(i => i.Email == normalizedLogin || i.Name == normalizedLogin);

            if (item == null || !IsLoginAllowed(item.UserId))
            {
                return PortalSignInResult.Failed();
            }

            bool hasCredentialTables = HasCredentialTables();
            if (hasCredentialTables)
            {
                var credential = _context.UserCredentials
                    .AsNoTracking()
                    .SingleOrDefault(i => i.UserId == item.UserId);

                if (credential != null)
                {
                    if (credential.RequiresReset)
                    {
                        return new PortalSignInResult(false, item.UserId, SafeName(item.Name), GetSecurityVersionByUserId(item.UserId), false, true);
                    }

                    if (!PortalPasswordHasher.Verify(
                        password,
                        credential.PasswordFormat,
                        credential.PasswordSalt,
                        credential.PasswordHash,
                        credential.IterationCount))
                    {
                        return PortalSignInResult.Failed();
                    }

                    MarkCredentialVerified(item.UserId);
                    return new PortalSignInResult(true, item.UserId, SafeName(item.Name), GetSecurityVersionByUserId(item.UserId), false, false);
                }
            }

            if (!VerifyLegacyPassword(item, password))
            {
                return PortalSignInResult.Failed();
            }

            bool upgradedLegacyCredential = false;
            if (hasCredentialTables)
            {
                DateTime nowUtc = DateTime.UtcNow;
                using (var transaction = _context.Database.BeginTransaction())
                {
                    EnsureSecurityState(item.UserId, "LegacyCredentialUpgrade");
                    UpsertCredential(item.UserId, password, nowUtc, nowUtc, false, null);
                    _context.SaveChanges();
                    transaction.Commit();
                }

                upgradedLegacyCredential = true;
            }

            return new PortalSignInResult(
                true,
                item.UserId,
                SafeName(item.Name),
                GetSecurityVersionByUserId(item.UserId),
                upgradedLegacyCredential,
                false);
        }

        /// <summary>
        /// 中文：兼容旧调用点的登录方法；新代码应优先使用 <see cref="SignIn"/>。
        ///
        /// English: Sign-in method retained for legacy call sites; new code should prefer <see cref="SignIn"/>.
        /// </summary>
        /// <param name="emailOrName">中文：用户输入的邮箱或登录名称。English: Email or sign-in name entered by the user.</param>
        /// <param name="password">中文：用户提交的密码输入。English: Submitted password input.</param>
        /// <returns>中文：登录成功且允许访问时返回用户名；否则为空字符串。English: User name when sign-in succeeds and access is allowed; otherwise an empty string.</returns>
        public string Login(string emailOrName, string password)
        {
            PortalSignInResult result = SignIn(emailOrName, password);
            return result.Succeeded ? result.UserName : string.Empty;
        }

        /// <summary>
        /// 中文：按用户名称读取当前安全版本；缺少 P5.2 表时返回 <c>0</c>。
        ///
        /// English: Reads the current security version by user name; returns <c>0</c> when P5.2 tables are absent.
        /// </summary>
        /// <param name="userName">中文：用户登录名称。English: User sign-in name.</param>
        /// <returns>中文：当前安全版本。English: Current security version.</returns>
        public long GetSecurityVersionByUserName(string userName)
        {
            string normalizedUserName = Normalize(userName);
            if (string.IsNullOrEmpty(normalizedUserName))
            {
                return 0;
            }

            try
            {
                var userIds = _context.Database.SqlQuery<int>(
                    "SELECT TOP (1) [UserID] FROM [dbo].[Portal_Users] WHERE [Name] = @p0",
                    normalizedUserName).ToList();
                return userIds.Count == 0 ? 0 : GetSecurityVersionByUserId(userIds[0]);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// 中文：按用户标识读取当前安全版本；缺少 P5.2 表时返回 <c>0</c>。
        ///
        /// English: Reads the current security version by user id; returns <c>0</c> when P5.2 tables are absent.
        /// </summary>
        /// <param name="userId">中文：用户数值标识。English: Numeric user identifier.</param>
        /// <returns>中文：当前安全版本。English: Current security version.</returns>
        public long GetSecurityVersionByUserId(int userId)
        {
            if (userId <= 0 || !HasSecurityStateTable())
            {
                return 0;
            }

            try
            {
                var versions = _context.Database.SqlQuery<long>(
                    "SELECT [SecurityVersion] FROM [dbo].[Portal_UserSecurityStates] WHERE [UserId] = @p0",
                    userId).ToList();
                if (versions.Count > 0)
                {
                    return versions[0];
                }

                EnsureSecurityState(userId, "AutoCreateSecurityState");
                return InitialSecurityVersion;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// 中文：递增指定用户的安全版本，使旧身份票据和角色 Cookie 在下一次请求失效。
        ///
        /// English: Increments the security version of the specified user so older auth tickets and role cookies become invalid on the next request.
        /// </summary>
        /// <param name="userId">中文：用户数值标识。English: Numeric user identifier.</param>
        /// <param name="reason">中文：非敏感变更原因。English: Non-sensitive change reason.</param>
        public void IncrementSecurityVersion(int userId, string reason)
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
                InitialSecurityVersion);
        }

        #endregion

        private bool IsLoginAllowed(int userId)
        {
            try
            {
                if (!HasRegistrationTable())
                {
                    // 中文：审核表尚未部署时保持旧系统可登录，避免迁移未完成导致全站锁定。
                    // English: Preserve legacy sign-in when the review table is not deployed, avoiding site-wide lockout during migration.
                    return true;
                }

                var registration = _context.UserRegistrations.SingleOrDefault(i => i.UserId == userId);
                return registration == null ||
                       registration.Status == PortalUserRegistrationStatuses.Approved;
            }
            catch (Exception)
            {
                // 中文：读取审核元数据失败时仍保持旧登录行为；该兼容回退须在未来安全迁移中重新评估。
                // English: Keep legacy sign-in when review metadata cannot be read; this compatibility fallback must be reassessed during future security migration.
                return true;
            }
        }

        private void TryEnsureApprovedRegistration(int userId, string approvedBy)
        {
            try
            {
                if (!HasRegistrationTable() || _context.UserRegistrations.Any(i => i.UserId == userId))
                {
                    return;
                }

                DateTime nowUtc = DateTime.UtcNow;
                _context.UserRegistrations.Add(new UserRegistrationItem
                {
                    UserId = userId,
                    Status = PortalUserRegistrationStatuses.Approved,
                    RequiresApproval = false,
                    RegisteredUtc = nowUtc,
                    ApprovedUtc = nowUtc,
                    ApprovedBy = Normalize(approvedBy)
                });
                _context.SaveChanges();
            }
            catch (Exception)
            {
                // 中文：可选审核元数据失败不能阻断管理员旧入口；诊断与补偿策略留给后续运营治理。
                // English: Optional review-metadata failures must not block the legacy administration entry; diagnostics and remediation belong to later operations governance.
            }
        }

        private void TryDeleteRegistration(int userId)
        {
            try
            {
                if (!HasRegistrationTable())
                {
                    return;
                }

                var registration = _context.UserRegistrations.SingleOrDefault(i => i.UserId == userId);
                if (registration != null)
                {
                    _context.UserRegistrations.Remove(registration);
                    _context.SaveChanges();
                }
            }
            catch (Exception)
            {
                // 中文：让后续用户删除流程继续暴露真实失败，避免扩展清理吞掉主流程错误。
                // English: Let the subsequent user-delete flow surface the real failure instead of letting extension cleanup swallow it.
            }
        }

        private void IncrementInviteUsage(string inviteCode)
        {
            string normalizedInviteCode = Normalize(inviteCode);
            if (string.IsNullOrEmpty(normalizedInviteCode) || !HasInviteTable())
            {
                return;
            }

            var invite = _context.RegistrationInvites.SingleOrDefault(i => i.InviteCode == normalizedInviteCode);
            if (invite != null)
            {
                invite.UsedCount++;
            }
        }

        private void UpsertCredential(
            int userId,
            string password,
            DateTime? legacyUpgradedUtc,
            DateTime? lastVerifiedUtc,
            bool requiresReset,
            string resetReason)
        {
            if (!HasCredentialTables())
            {
                throw new InvalidOperationException("Credential metadata tables are not available.");
            }

            PortalPasswordHash hash = PortalPasswordHasher.CreateHash(password);
            DateTime nowUtc = DateTime.UtcNow;
            var credential = _context.UserCredentials.SingleOrDefault(i => i.UserId == userId);
            if (credential == null)
            {
                credential = new UserCredentialItem
                {
                    UserId = userId,
                    CreatedUtc = nowUtc
                };
                _context.UserCredentials.Add(credential);
            }

            credential.CredentialVersion = 1;
            credential.PasswordFormat = hash.Format;
            credential.PasswordHash = hash.Hash;
            credential.PasswordSalt = hash.Salt;
            credential.IterationCount = hash.IterationCount;
            credential.UpdatedUtc = nowUtc;
            credential.LastVerifiedUtc = lastVerifiedUtc;
            credential.LegacyUpgradedUtc = legacyUpgradedUtc;
            credential.RequiresReset = requiresReset;
            credential.ResetReason = string.IsNullOrWhiteSpace(resetReason) ? null : NormalizeReason(resetReason);
        }

        private void MarkCredentialVerified(int userId)
        {
            if (!HasCredentialTable())
            {
                return;
            }

            _context.Database.ExecuteSqlCommand(
                "UPDATE [dbo].[Portal_UserCredentials] SET [LastVerifiedUtc] = SYSUTCDATETIME() WHERE [UserId] = @p0",
                userId);
        }

        private void EnsureSecurityState(int userId, string reason)
        {
            if (userId <= 0 || !HasSecurityStateTable())
            {
                return;
            }

            _context.Database.ExecuteSqlCommand(
                @"
IF NOT EXISTS (SELECT 1 FROM [dbo].[Portal_UserSecurityStates] WHERE [UserId] = @p0)
BEGIN
    INSERT INTO [dbo].[Portal_UserSecurityStates] ([UserId], [SecurityVersion], [ChangedUtc], [ChangeReason])
    SELECT @p0, @p2, SYSUTCDATETIME(), @p1
    WHERE EXISTS (SELECT 1 FROM [dbo].[Portal_Users] WHERE [UserID] = @p0);
END",
                userId,
                NormalizeReason(reason),
                InitialSecurityVersion);
        }

        private bool VerifyLegacyPassword(UserItem item, string password)
        {
            if (item == null || string.IsNullOrEmpty(item.Password))
            {
                return false;
            }

            string legacyDigest = PortalSecurity.Encrypt(password);
            return string.Equals(item.Password, legacyDigest, StringComparison.Ordinal);
        }

        private bool HasCredentialTables()
        {
            return HasCredentialTable() && HasSecurityStateTable();
        }

        private bool HasCredentialTable()
        {
            return HasTable(CredentialTableName);
        }

        private bool HasSecurityStateTable()
        {
            return HasTable(SecurityStateTableName);
        }

        private bool HasRegistrationTable()
        {
            return HasTable("PortalCfg_UserRegistrations");
        }

        private bool HasInviteTable()
        {
            return HasTable("PortalCfg_RegistrationInvites");
        }

        private bool HasTable(string tableName)
        {
            try
            {
                string sql = string.Format(
                    "SELECT CASE WHEN OBJECT_ID(N'[dbo].[{0}]', N'U') IS NULL THEN 0 ELSE 1 END",
                    tableName);
                return _context.Database.SqlQuery<int>(sql).Single() == 1;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static IUserRegistrationInfo CreateLegacyRegistrationInfo(int userId, string source)
        {
            return new UserRegistrationInfo(
                userId,
                PortalUserRegistrationStatuses.Approved,
                false,
                string.Empty,
                string.Empty,
                DateTime.MinValue,
                null,
                string.Empty,
                null,
                string.Empty,
                string.Empty,
                source);
        }

        private static string SafeName(string value)
        {
            return value == null ? string.Empty : value.Trim();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string NormalizeReason(string value)
        {
            string normalized = Normalize(value);
            if (string.IsNullOrEmpty(normalized))
            {
                return "Unspecified";
            }

            return normalized.Substring(0, Math.Min(normalized.Length, 100));
        }

        private static string NullIfEmpty(string value)
        {
            string normalized = Normalize(value);
            return string.IsNullOrEmpty(normalized) ? null : normalized;
        }
    }
}
