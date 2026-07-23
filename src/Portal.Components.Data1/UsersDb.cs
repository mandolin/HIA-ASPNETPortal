using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>基于门户安全数据库上下文的用户、凭据和注册审核数据访问实现。</zh-CN>
    ///   <en>User, credential, and registration-review data-access implementation backed by the Portal security database context.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P5.2 起，新建、注册和重置密码写入 <c>Portal_UserCredentials</c> 强哈希表，并通过 <c>Portal_UserSecurityStates</c> 维护会话安全版本。旧 <c>Portal_Users.Password</c> 中的 MD5 仅用于尚未升级用户的首次兼容登录；一旦强哈希凭据存在，登录不得再回退到旧字段。</zh-CN>
    ///   <en>Starting with P5.2, newly created, registered, and reset passwords are written to the <c>Portal_UserCredentials</c> strong-hash table, while <c>Portal_UserSecurityStates</c> maintains the session security version. Legacy MD5 values in <c>Portal_Users.Password</c> are used only for the first compatibility sign-in of users that have not yet been upgraded; once a strong credential exists, sign-in must not fall back to the legacy column.</en>
    /// </lang>
    /// </remarks>
    public class UsersDb : IUsersDb
    {
        private const long InitialSecurityVersion = 1;
        private const string CredentialTableName = "Portal_UserCredentials";
        private const string EmployeeTableName = "PortalBiz_Employees";
        private const string EmployeeBindingTableName = "PortalBiz_UserEmployeeBindings";
        private const string ProfileTableName = "PortalBiz_UserProfiles";
        private const string SecurityStateTableName = "Portal_UserSecurityStates";
        private readonly PortalSecurityDbContext _context;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化用户、凭据和注册审核数据访问实现。</zh-CN>
        ///   <en>Initializes the user, credential, and registration-review data-access implementation.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>门户安全数据库上下文。</zh-CN>
        ///   <en>Portal security database context.</en>
        /// </l>
        /// </param>
        public UsersDb(PortalSecurityDbContext context)
        {
            _context = context;
        }

        #region IUsersDb Members

        /// <summary>
        /// <lang>
        ///   <zh-CN>添加由管理员或既有流程创建的用户；非空密码写入强哈希凭据表。</zh-CN>
        ///   <en>Adds a user created by an administrator or an existing legacy flow; non-empty passwords are written to the strong-hash credential table.</en>
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
        ///   <en>Submitted password input; empty creates a non-signable placeholder user.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>成功时返回新用户标识；预期写入失败时返回 <c>-1</c>。</zh-CN>
        ///   <en>New user identifier on success; <c>-1</c> for expected write failures.</en>
        /// </l>
        /// </returns>
        public int AddUser(string fullName, string email, string password)
        {
            string normalizedPassword = password ?? string.Empty;
            string passwordPolicyMessage;
            if (!string.IsNullOrEmpty(normalizedPassword) &&
                !PortalPasswordPolicy.TryValidate(
                    normalizedPassword,
                    BuildPasswordPolicyContextTerms(fullName, email),
                    out passwordPolicyMessage))
            {
                return -1;
            }

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
                        // <lang>
                        //   <zh-CN>新用户不再生成 MD5；旧字段只保留既有数据的迁移样本。</zh-CN>
                        //   <en>New users no longer receive MD5; the legacy column remains only for existing data migration samples.</en>
                        // </lang>
                        Password = string.Empty
                    };

                    _context.Users.Add(item);
                    _context.SaveChanges();

                    EnsureSecurityState(item.UserId, "UserCreated");
                    TryEnsureUserProfile(
                        item.UserId,
                        item.Name,
                        item.Email,
                        PortalUserProfileStatuses.Active,
                        "system-admin");
                    if (!string.IsNullOrEmpty(normalizedPassword))
                    {
                        UpsertCredential(item.UserId, normalizedPassword, null, null, false, null);
                    }

                    _context.SaveChanges();
                    transaction.Commit();
                }

                // <lang>
                //   <zh-CN>管理员或旧入口创建的用户默认已批准；缺少扩展表时保持旧行为。</zh-CN>
                //   <en>Users created by administration or legacy entry points are approved by default; missing extension tables preserve legacy behavior.</en>
                // </lang>
                TryEnsureApprovedRegistration(item.UserId, "system-admin");
                return item.UserId;
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>保持旧接口约定，预期写入失败用 -1 表达，不在此记录敏感输入。</zh-CN>
                //   <en>Preserve the legacy interface contract: expected write failures return -1 and do not log sensitive input here.</en>
                // </lang>
                return -1;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>添加自主注册用户，并在同一事务写入强哈希凭据、注册审核元数据和邀请码使用次数。</zh-CN>
        ///   <en>Adds a self-registered user and writes the strong-hash credential, registration-review metadata, and invite usage in one transaction.</en>
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
        ///   <zh-CN>可为空的邀请码；空值代表当前允许的非邀请注册。</zh-CN>
        ///   <en>Optional invite code; an empty value represents currently allowed non-invite registration.</en>
        /// </l>
        /// </param>
        /// <param name="requiresApproval">
        /// <l>
        ///   <zh-CN>是否以待审核状态创建用户。</zh-CN>
        ///   <en>Whether to create the user in pending-approval status.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>成功时返回新用户标识；审核表、凭据表不可用或邀请码无效时返回 <c>-1</c>。</zh-CN>
        ///   <en>New user identifier on success; <c>-1</c> when review tables, credential tables, or invites are unavailable.</en>
        /// </l>
        /// </returns>
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

            string passwordPolicyMessage;
            if (!PortalPasswordPolicy.TryValidate(
                password,
                BuildPasswordPolicyContextTerms(fullName, email, employeeCode),
                out passwordPolicyMessage))
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
                    TryEnsureUserProfile(
                        item.UserId,
                        item.Name,
                        item.Email,
                        requiresApproval
                            ? PortalUserProfileStatuses.PendingApproval
                            : PortalUserProfileStatuses.Active,
                        "system-self-registration");
                    UpsertCredential(item.UserId, password, null, null, false, null);

                    // <lang>
                    //   <zh-CN>所有注册审核时间统一以 UTC 保存。</zh-CN>
                    //   <en>Store all registration-review timestamps in UTC.</en>
                    // </lang>
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

                    // <lang>
                    //   <zh-CN>空邀请码不会增加使用次数；有效邀请码的计数与用户创建同一事务提交。</zh-CN>
                    //   <en>Empty invite codes do not increment usage; valid invite counts commit in the same transaction as user creation.</en>
                    // </lang>
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
        /// <lang>
        ///   <zh-CN>删除指定用户及可用的注册审核元数据。</zh-CN>
        ///   <en>Deletes the specified user and available registration-review metadata.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>要删除的用户数值标识。</zh-CN>
        ///   <en>Numeric identifier of the user to delete.</en>
        /// </l>
        /// </param>
        public void DeleteUser(int userId)
        {
            // <lang>
            //   <zh-CN>先清理扩展元数据，避免显式 FK 部署阻止既有删除流程。</zh-CN>
            //   <en>Clean up extension metadata first so explicit FK deployments do not block the legacy delete flow.</en>
            // </lang>
            TryDeleteRegistration(userId);

            var item = _context.Users.Single(i => i.UserId == userId);

            _context.Users.Remove(item);
            _context.SaveChanges();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>批准用户注册，并恢复已拒绝记录的登录资格。</zh-CN>
        ///   <en>Approves a user registration and restores sign-in eligibility for a previously rejected record.</en>
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
        /// <exception cref="InvalidOperationException">
        /// <l>
        ///   <zh-CN>注册审核表不可用时引发。</zh-CN>
        ///   <en>Thrown when the registration-review table is unavailable.</en>
        /// </l>
        /// </exception>
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
            TrySetUserProfileStatus(
                userId,
                PortalUserProfileStatuses.Active,
                string.Empty,
                Normalize(approvedBy));
            IncrementSecurityVersion(userId, "RegistrationApproved");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>拒绝处于待审核状态的用户注册；管理员后续仍可重新批准。</zh-CN>
        ///   <en>Rejects a pending user registration; an administrator may approve it later.</en>
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
        /// <exception cref="InvalidOperationException">
        /// <l>
        ///   <zh-CN>注册审核表或用户审核记录不可用时引发。</zh-CN>
        ///   <en>Thrown when the registration-review table or user review record is unavailable.</en>
        /// </l>
        /// </exception>
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
            TrySetUserProfileStatus(
                userId,
                PortalUserProfileStatuses.Disabled,
                "RegistrationRejected",
                Normalize(rejectedBy));
            IncrementSecurityVersion(userId, "RegistrationRejected");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取用户注册审核信息；旧库或旧用户按已批准兼容视图展示。</zh-CN>
        ///   <en>Reads user registration-review information; legacy databases or users appear through an approved compatibility view.</en>
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
        ///   <zh-CN>注册审核只读信息，包含信息来源。</zh-CN>
        ///   <en>Read-only registration-review information including its source.</en>
        /// </l>
        /// </returns>
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
        /// <lang>
        ///   <zh-CN>校验临时注册链接邀请码。</zh-CN>
        ///   <en>Validates a temporary registration invite code.</en>
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
        ///   <zh-CN>失败时可安全展示的说明；成功时为空。</zh-CN>
        ///   <en>Display-safe explanation on failure; empty on success.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>邀请码有效，或当前允许空邀请码时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the code is valid or an empty code is currently allowed.</en>
        /// </l>
        /// </returns>
        public bool ValidateRegistrationInvite(string inviteCode, out string message)
        {
            // <lang>
            //   <zh-CN>空邀请码当前表示允许的非邀请注册，未来“必须邀请码”策略应作为独立设置实现。</zh-CN>
            //   <en>An empty invite code currently represents allowed non-invite registration; a required-invite policy must be implemented as a separate setting.</en>
            // </lang>
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
        /// <lang>
        ///   <zh-CN>更新指定用户的邮箱并重置强哈希凭据。</zh-CN>
        ///   <en>Updates the specified user's email and resets the strong-hash credential.</en>
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
        ///   <zh-CN>新的密码输入，不得写入审计正文或诊断日志。</zh-CN>
        ///   <en>New password input; it must not be written to audit details or diagnostic logs.</en>
        /// </l>
        /// </param>
        public void UpdateUser(int userId, string email, string password)
        {
            UpdateUserProfile(userId, null, null, null, email, password, "system-admin");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取用户资料扩展；缺少 P6.2 profile 表或记录时返回兼容视图。</zh-CN>
        ///   <en>Reads the user-profile extension and returns a compatibility view when the P6.2 profile table or row is missing.</en>
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
        ///   <zh-CN>资料扩展只读视图；用户不存在时为空。</zh-CN>
        ///   <en>Read-only profile view, or null when the user does not exist.</en>
        /// </l>
        /// </returns>
        public IUserProfileInfo GetUserProfileInfo(int userId)
        {
            UserItem user = _context.Users.AsNoTracking().SingleOrDefault(i => i.UserId == userId);
            if (user == null)
            {
                return null;
            }

            if (!HasUserProfileTable())
            {
                return CreateLegacyProfileInfo(user, false, "LegacyNoProfileTable");
            }

            UserProfileItem profile = _context.UserProfiles.AsNoTracking().SingleOrDefault(i => i.UserId == userId);
            if (profile == null)
            {
                return CreateLegacyProfileInfo(user, true, "ProfileMissingLegacyFallback");
            }

            return new UserProfileInfo(
                user.UserId,
                SafeName(user.Name),
                SafeName(profile.LoginName),
                SafeName(profile.DisplayName),
                SafeName(profile.Nickname),
                SafeName(profile.PreferredEmail),
                NormalizeProfileStatus(profile.Status),
                SafeName(profile.StatusReason),
                true,
                "UserProfile");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新用户资料扩展、同步旧邮箱，并在提供密码时重置强哈希凭据。</zh-CN>
        ///   <en>Updates the user-profile extension, synchronizes the legacy email, and resets the strong-hash credential when a password is provided.</en>
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
        ///   <zh-CN>新的稳定登录名；为空时保留旧登录名兼容值。</zh-CN>
        ///   <en>New stable login name; empty preserves the legacy-compatible value.</en>
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
        public void UpdateUserProfile(
            int userId,
            string loginName,
            string displayName,
            string nickname,
            string email,
            string password,
            string actor)
        {
            string normalizedEmail = Normalize(email);
            if (string.IsNullOrEmpty(normalizedEmail))
            {
                throw new InvalidOperationException("Email is required.");
            }

            string normalizedPassword = password ?? string.Empty;
            bool shouldResetCredential = !string.IsNullOrEmpty(normalizedPassword);
            if (shouldResetCredential && !HasCredentialTables())
            {
                throw new InvalidOperationException("Credential metadata tables are not available.");
            }

            string passwordPolicyMessage;
            if (shouldResetCredential &&
                !PortalPasswordPolicy.TryValidate(
                    normalizedPassword,
                    BuildPasswordPolicyContextTerms(loginName, displayName, nickname, email),
                    out passwordPolicyMessage))
            {
                throw new InvalidOperationException(passwordPolicyMessage);
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                var item = _context.Users.Single(i => i.UserId == userId);
                string normalizedLoginName = string.IsNullOrWhiteSpace(loginName)
                    ? Normalize(item.Name)
                    : Normalize(loginName);
                string normalizedDisplayName = string.IsNullOrWhiteSpace(displayName)
                    ? normalizedLoginName
                    : Normalize(displayName);
                string normalizedNickname = Normalize(nickname);
                string normalizedActor = Normalize(actor);

                if (string.IsNullOrEmpty(normalizedLoginName))
                {
                    throw new InvalidOperationException("Login name is required.");
                }

                bool hasProfileTable = HasUserProfileTable();
                if (hasProfileTable)
                {
                    if (HasLoginNameConflict(userId, normalizedLoginName))
                    {
                        throw new InvalidOperationException("Login name is already used by another user.");
                    }

                    if (HasEmailConflict(userId, normalizedEmail))
                    {
                        throw new InvalidOperationException("Email is already used by another user.");
                    }

                    UpsertUserProfile(
                        item.UserId,
                        normalizedLoginName,
                        normalizedDisplayName,
                        normalizedNickname,
                        normalizedEmail,
                        NormalizeProfileStatus(GetExistingProfileStatus(item.UserId)),
                        normalizedActor);
                }
                else if (HasLegacyEmailConflict(userId, normalizedEmail))
                {
                    throw new InvalidOperationException("Email is already used by another user.");
                }

                item.Email = normalizedEmail;
                item.Password = string.Empty;
                if (shouldResetCredential)
                {
                    UpsertCredential(userId, normalizedPassword, null, null, false, null);
                }

                _context.SaveChanges();
                if (shouldResetCredential)
                {
                    IncrementSecurityVersion(userId, "CredentialReset");
                }

                transaction.Commit();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>设置用户资料生命周期状态；页面或服务入口负责在成功后写入运营审计。</zh-CN>
        ///   <en>Sets the user-profile lifecycle status; the page or service entry point records operations audit after a successful state change.</en>
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
        ///   <zh-CN>目标状态，必须是已知 profile 状态。</zh-CN>
        ///   <en>Target status, which must be a known profile status.</en>
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
        /// <exception cref="InvalidOperationException">
        /// <l>
        ///   <zh-CN>资料表不可用或状态无效时引发。</zh-CN>
        ///   <en>Thrown when the profile table is unavailable or the status is invalid.</en>
        /// </l>
        /// </exception>
        public void SetUserProfileStatus(int userId, string status, string reason, string actor)
        {
            if (!HasUserProfileTable())
            {
                throw new InvalidOperationException("User profile metadata table is not available.");
            }

            string normalizedStatus = NormalizeProfileStatus(status);
            if (!IsKnownProfileStatus(normalizedStatus))
            {
                throw new InvalidOperationException("User profile status is invalid.");
            }

            string normalizedActor = Normalize(actor);
            string normalizedReason = NormalizeReason(reason);

            using (var transaction = _context.Database.BeginTransaction())
            {
                var item = _context.Users.Single(i => i.UserId == userId);
                var profile = _context.UserProfiles.SingleOrDefault(i => i.UserId == userId);
                DateTime nowUtc = DateTime.UtcNow;

                if (profile == null)
                {
                    profile = new UserProfileItem
                    {
                        UserId = item.UserId,
                        LoginName = Normalize(item.Name),
                        DisplayName = Normalize(item.Name),
                        PreferredEmail = NullIfEmpty(item.Email),
                        CreatedUtc = nowUtc,
                        CreatedBy = NullIfEmpty(normalizedActor)
                    };
                    _context.UserProfiles.Add(profile);
                }

                profile.Status = normalizedStatus;
                profile.StatusReason = NullIfEmpty(normalizedReason);
                profile.UpdatedUtc = nowUtc;
                profile.UpdatedBy = NullIfEmpty(normalizedActor);
                _context.SaveChanges();

                IncrementSecurityVersion(userId, normalizedReason);
                transaction.Commit();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取指定用户的所有角色。</zh-CN>
        ///   <en>Gets all roles of the specified user.</en>
        /// </lang>
        /// </summary>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>用户登录名称。</zh-CN>
        ///   <en>User sign-in name.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>角色集合；没有角色时为空集合。</zh-CN>
        ///   <en>Role collection; empty when the user has no roles.</en>
        /// </l>
        /// </returns>
        public IEnumerable<IRoleItem> GetRolesByUser(string name)
        {
            // <lang>
            //   <zh-CN>角色成员关系可能由 RolesDb 通过中间表直接写入；显式查询可避开 singleton DbContext 的导航集合缓存。</zh-CN>
            //   <en>Role memberships may be written directly through the join table by RolesDb; explicit querying avoids stale navigation collections on the singleton DbContext.</en>
            // </lang>
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
        /// <lang>
        ///   <zh-CN>获取指定用户的所有角色名称。</zh-CN>
        ///   <en>Gets names of all roles of the specified user.</en>
        /// </lang>
        /// </summary>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>用户登录名称。</zh-CN>
        ///   <en>User sign-in name.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>角色名称集合；没有角色时为空集合。</zh-CN>
        ///   <en>Role-name collection; empty when the user has no roles.</en>
        /// </l>
        /// </returns>
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
        /// <lang>
        ///   <zh-CN>获取单个用户。</zh-CN>
        ///   <en>Gets a single user.</en>
        /// </lang>
        /// </summary>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>用户登录名称。</zh-CN>
        ///   <en>User sign-in name.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配的用户对象。</zh-CN>
        ///   <en>Matching user object.</en>
        /// </l>
        /// </returns>
        public IUserItem GetSingleUser(string name)
        {
            return _context.Users.Single(i => i.Name == name);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按数值标识查找用户；页面请求目标缺失时返回空值而不是抛出查询异常。</zh-CN>
        ///   <en>Finds a user by numeric identifier, returning null instead of throwing when a page-request target is absent.</en>
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
        public IUserItem FindUserById(int userId)
        {
            return _context.Users.SingleOrDefault(i => i.UserId == userId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析登录标识并校验输入密码、注册审核状态和 P5.2 强凭据。</zh-CN>
        ///   <en>Resolves the sign-in identifier, then validates the submitted password, registration-review status, and P5.2 strong credential.</en>
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
        ///   <zh-CN>登录结果；失败时为通用失败对象。</zh-CN>
        ///   <en>Sign-in result; generic failure object on failure.</en>
        /// </l>
        /// </returns>
        public PortalSignInResult SignIn(string emailOrName, string password)
        {
            string normalizedLogin = Normalize(emailOrName);
            if (string.IsNullOrEmpty(normalizedLogin) || password == null)
            {
                return PortalSignInResult.Failed();
            }

            var resolution = new PortalLoginIdentifierResolver(
                _context,
                HasUserProfileTable(),
                HasEmployeeCodeSignInTables()).Resolve(normalizedLogin);
            if (!resolution.Found || resolution.Ambiguous)
            {
                return PortalSignInResult.Failed();
            }

            var item = _context.Users
                .AsNoTracking()
                .SingleOrDefault(i => i.UserId == resolution.UserId);

            if (item == null || !IsUserProfileLoginAllowed(item.UserId) || !IsLoginAllowed(item.UserId))
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
        public string Login(string emailOrName, string password)
        {
            PortalSignInResult result = SignIn(emailOrName, password);
            return result.Succeeded ? result.UserName : string.Empty;
        }

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
                    // <lang>
                    //   <zh-CN>审核表尚未部署时保持旧系统可登录，避免迁移未完成导致全站锁定。</zh-CN>
                    //   <en>Preserve legacy sign-in when the review table is not deployed, avoiding site-wide lockout during migration.</en>
                    // </lang>
                    return true;
                }

                var registration = _context.UserRegistrations.SingleOrDefault(i => i.UserId == userId);
                return registration == null ||
                       registration.Status == PortalUserRegistrationStatuses.Approved;
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>读取审核元数据失败时仍保持旧登录行为；该兼容回退须在未来安全迁移中重新评估。</zh-CN>
                //   <en>Keep legacy sign-in when review metadata cannot be read; this compatibility fallback must be reassessed during future security migration.</en>
                // </lang>
                return true;
            }
        }

        private bool IsUserProfileLoginAllowed(int userId)
        {
            try
            {
                if (!HasUserProfileTable())
                {
                    return true;
                }

                string status = _context.Database.SqlQuery<string>(
                    "SELECT TOP (1) [Status] FROM [dbo].[PortalBiz_UserProfiles] WHERE [UserId] = @p0",
                    userId).SingleOrDefault();
                return string.IsNullOrEmpty(status) ||
                       string.Equals(status, PortalUserProfileStatuses.Active, StringComparison.Ordinal);
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>profile 读取失败时保持旧路径可用，部署健康检查会另行暴露缺失或损坏的扩展表。</zh-CN>
                //   <en>Keep the legacy path usable when profile reads fail; deployment health checks will expose</en>
                // </lang>
                // missing or damaged extension tables separately.
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
                // <lang>
                //   <zh-CN>可选审核元数据失败不能阻断管理员旧入口；诊断与补偿策略留给后续运营治理。</zh-CN>
                //   <en>Optional review-metadata failures must not block the legacy administration entry; diagnostics and remediation belong to later operations governance.</en>
                // </lang>
            }
        }

        private void TryEnsureUserProfile(int userId, string loginName, string email, string status, string actor)
        {
            try
            {
                if (!HasUserProfileTable() || _context.UserProfiles.Any(i => i.UserId == userId))
                {
                    return;
                }

                DateTime nowUtc = DateTime.UtcNow;
                string normalizedLoginName = Normalize(loginName);
                var profile = new UserProfileItem
                {
                    UserId = userId,
                    LoginName = normalizedLoginName,
                    DisplayName = normalizedLoginName,
                    PreferredEmail = NullIfEmpty(email),
                    Status = NormalizeProfileStatus(status),
                    CreatedUtc = nowUtc,
                    CreatedBy = NullIfEmpty(actor),
                    UpdatedUtc = nowUtc,
                    UpdatedBy = NullIfEmpty(actor)
                };
                _context.UserProfiles.Add(profile);
            }
            catch (Exception)
            {
                if (HasUserProfileTable())
                {
                    throw;
                }
            }
        }

        private void TryUpdateUserProfileEmail(int userId, string email, string actor)
        {
            try
            {
                if (!HasUserProfileTable())
                {
                    return;
                }

                var profile = _context.UserProfiles.SingleOrDefault(i => i.UserId == userId);
                if (profile == null)
                {
                    return;
                }

                profile.PreferredEmail = NullIfEmpty(email);
                profile.UpdatedUtc = DateTime.UtcNow;
                profile.UpdatedBy = NullIfEmpty(actor);
            }
            catch (Exception)
            {
                if (HasUserProfileTable())
                {
                    throw;
                }
            }
        }

        private void TrySetUserProfileStatus(int userId, string status, string reason, string actor)
        {
            try
            {
                if (!HasUserProfileTable())
                {
                    return;
                }

                var profile = _context.UserProfiles.SingleOrDefault(i => i.UserId == userId);
                if (profile == null)
                {
                    return;
                }

                profile.Status = NormalizeProfileStatus(status);
                profile.StatusReason = NullIfEmpty(reason);
                profile.UpdatedUtc = DateTime.UtcNow;
                profile.UpdatedBy = NullIfEmpty(actor);
                _context.SaveChanges();
            }
            catch (Exception)
            {
                if (HasUserProfileTable())
                {
                    throw;
                }
            }
        }

        private void UpsertUserProfile(
            int userId,
            string loginName,
            string displayName,
            string nickname,
            string email,
            string status,
            string actor)
        {
            DateTime nowUtc = DateTime.UtcNow;
            var profile = _context.UserProfiles.SingleOrDefault(i => i.UserId == userId);
            if (profile == null)
            {
                profile = new UserProfileItem
                {
                    UserId = userId,
                    CreatedUtc = nowUtc,
                    CreatedBy = NullIfEmpty(actor)
                };
                _context.UserProfiles.Add(profile);
            }

            profile.LoginName = loginName;
            profile.DisplayName = NullIfEmpty(displayName);
            profile.Nickname = NullIfEmpty(nickname);
            profile.PreferredEmail = NullIfEmpty(email);
            profile.Status = NormalizeProfileStatus(status);
            profile.UpdatedUtc = nowUtc;
            profile.UpdatedBy = NullIfEmpty(actor);
        }

        private bool HasLoginNameConflict(int userId, string loginName)
        {
            int count = _context.Database.SqlQuery<int>(
                @"
SELECT COUNT(*)
FROM
(
    SELECT [UserId]
    FROM [dbo].[PortalBiz_UserProfiles]
    WHERE [LoginName] = @p0 AND [UserId] <> @p1

    UNION

    SELECT [UserID]
    FROM [dbo].[Portal_Users]
    WHERE [Name] = @p0 AND [UserID] <> @p1
) AS [Conflicts];",
                loginName,
                userId).Single();
            return count > 0;
        }

        private bool HasEmailConflict(int userId, string email)
        {
            int count = _context.Database.SqlQuery<int>(
                @"
SELECT COUNT(*)
FROM
(
    SELECT [UserId]
    FROM [dbo].[PortalBiz_UserProfiles]
    WHERE [PreferredEmail] = @p0 AND [UserId] <> @p1

    UNION

    SELECT [UserID]
    FROM [dbo].[Portal_Users]
    WHERE [Email] = @p0 AND [UserID] <> @p1
) AS [Conflicts];",
                email,
                userId).Single();
            return count > 0;
        }

        private bool HasLegacyEmailConflict(int userId, string email)
        {
            int count = _context.Database.SqlQuery<int>(
                "SELECT COUNT(*) FROM [dbo].[Portal_Users] WHERE [Email] = @p0 AND [UserID] <> @p1",
                email,
                userId).Single();
            return count > 0;
        }

        private string GetExistingProfileStatus(int userId)
        {
            var profile = _context.UserProfiles.SingleOrDefault(i => i.UserId == userId);
            if (profile != null)
            {
                return NormalizeProfileStatus(profile.Status);
            }

            if (HasRegistrationTable())
            {
                var registration = _context.UserRegistrations.SingleOrDefault(i => i.UserId == userId);
                if (registration != null)
                {
                    if (string.Equals(registration.Status, PortalUserRegistrationStatuses.PendingApproval, StringComparison.Ordinal))
                    {
                        return PortalUserProfileStatuses.PendingApproval;
                    }

                    if (string.Equals(registration.Status, PortalUserRegistrationStatuses.Rejected, StringComparison.Ordinal))
                    {
                        return PortalUserProfileStatuses.Disabled;
                    }
                }
            }

            return PortalUserProfileStatuses.Active;
        }

        private static IUserProfileInfo CreateLegacyProfileInfo(UserItem user, bool isAvailable, string source)
        {
            string legacyName = SafeName(user.Name);
            return new UserProfileInfo(
                user.UserId,
                legacyName,
                legacyName,
                legacyName,
                string.Empty,
                SafeName(user.Email),
                PortalUserProfileStatuses.Active,
                string.Empty,
                isAvailable,
                source);
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
                // <lang>
                //   <zh-CN>让后续用户删除流程继续暴露真实失败，避免扩展清理吞掉主流程错误。</zh-CN>
                //   <en>Let the subsequent user-delete flow surface the real failure instead of letting extension cleanup swallow it.</en>
                // </lang>
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

        private bool HasUserProfileTable()
        {
            return HasTable(ProfileTableName);
        }

        private bool HasEmployeeCodeSignInTables()
        {
            return HasTable(EmployeeTableName) && HasTable(EmployeeBindingTableName);
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>为密码策略提供账号相关上下文词；调用方不得记录返回值。</zh-CN>
        ///   <en>Provides account-related context terms for password policy checks; callers must not log the returned values.</en>
        /// </lang>
        /// </summary>
        /// <param name="terms">
        /// <l>
        ///   <zh-CN>用户名、邮箱、员工号、显示名等候选词。</zh-CN>
        ///   <en>Candidate user name, email, employee code, display name, and similar terms.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>供策略层只读使用的上下文词数组。</zh-CN>
        ///   <en>Context-term array for read-only policy checks.</en>
        /// </l>
        /// </returns>
        private static string[] BuildPasswordPolicyContextTerms(params string[] terms)
        {
            return terms ?? new string[0];
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

        private static string NormalizeProfileStatus(string value)
        {
            string normalized = Normalize(value);
            return string.IsNullOrEmpty(normalized)
                ? PortalUserProfileStatuses.Active
                : normalized;
        }

        private static bool IsKnownProfileStatus(string value)
        {
            return string.Equals(value, PortalUserProfileStatuses.Active, StringComparison.Ordinal) ||
                   string.Equals(value, PortalUserProfileStatuses.PendingApproval, StringComparison.Ordinal) ||
                   string.Equals(value, PortalUserProfileStatuses.PendingEmployeeBinding, StringComparison.Ordinal) ||
                   string.Equals(value, PortalUserProfileStatuses.Disabled, StringComparison.Ordinal) ||
                   string.Equals(value, PortalUserProfileStatuses.Left, StringComparison.Ordinal) ||
                   string.Equals(value, PortalUserProfileStatuses.Locked, StringComparison.Ordinal);
        }

        private static string NullIfEmpty(string value)
        {
            string normalized = Normalize(value);
            return string.IsNullOrEmpty(normalized) ? null : normalized;
        }
    }
}
