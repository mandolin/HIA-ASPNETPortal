using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：基于门户安全数据库上下文的既有用户和注册审核数据访问实现。
    ///
    /// English: Legacy user and registration-review data-access implementation backed by the Portal security database context.
    /// </summary>
    /// <remarks>
    /// 中文：此实现保留既有用户表的密码摘要格式，并在注册审核、邀请码和旧库兼容之间提供最小过渡行为。
    /// 它不实现新密码哈希迁移、邀请创建、全局会话撤销或细粒度授权。
    ///
    /// English: This implementation retains the legacy user-table password-digest format and provides minimal
    /// transition behavior across registration review, invite codes, and legacy databases. It does not implement
    /// a new password-hash migration, invite creation, global session revocation, or fine-grained authorization.
    /// </remarks>
    public class UsersDb : IUsersDb
    {
        private readonly PortalSecurityDbContext _context;

        /// <summary>
        /// 中文：初始化用户和注册审核数据访问实现。
        ///
        /// English: Initializes the user and registration-review data-access implementation.
        /// </summary>
        /// <param name="context">中文：门户安全数据库上下文。English: Portal security database context.</param>
        public UsersDb(PortalSecurityDbContext context)
        {
            _context = context;
        }

        #region IUsersDb Members

        /// <summary>
        /// 中文：添加由管理员或既有流程创建的用户。
        ///
        /// English: Adds a user created by an administrator or an existing legacy flow.
        /// </summary>
        /// <param name="fullName">中文：用户登录名称。English: User sign-in name.</param>
        /// <param name="email">中文：用户邮箱地址。English: User email address.</param>
        /// <param name="password">中文：既有格式密码摘要。English: Legacy-format password digest.</param>
        /// <returns>中文：成功时返回新用户标识；预期写入失败时返回 <c>-1</c>。English: New user identifier on success; <c>-1</c> for expected write failures.</returns>
        public int AddUser(string fullName, string email, string password)
        {
            // 中文：管理员和既有入口保留当前用户表写入格式。
            // English: Preserve the current user-table write format for administration and legacy entry points.
            var item = new UserItem
            {
                Name = fullName,
                Email = email,
                Password = password
            };

            try
            {
                // 中文：将新用户写入既有用户表。
                // English: Write the new user to the legacy user table.
                _context.Users.Add(item);

                _context.SaveChanges();

                // 中文：管理员或旧入口创建的用户默认已批准；缺少扩展表时保持旧行为。
                // English: Users created by administration or legacy entry points are approved by default; missing extension tables preserve legacy behavior.
                TryEnsureApprovedRegistration(item.UserId, "system-admin");
            }
            catch (Exception)
            {
                // 中文：保持旧接口约定，预期写入失败用 -1 表达，不在此记录敏感输入。
                // English: Preserve the legacy interface contract: expected write failures return -1 and do not log sensitive input here.
                return -1;
            }

            return item.UserId;
        }

        /// <summary>
        /// 中文：添加自主注册用户，并在同一事务写入注册审核元数据和邀请码使用次数。
        ///
        /// English: Adds a self-registered user and writes registration-review metadata and invite usage in one transaction.
        /// </summary>
        /// <param name="fullName">中文：用户登录名称。English: User sign-in name.</param>
        /// <param name="email">中文：用户邮箱地址。English: User email address.</param>
        /// <param name="password">中文：既有格式密码摘要。English: Legacy-format password digest.</param>
        /// <param name="employeeCode">中文：可为空的员工号。English: Optional employee code.</param>
        /// <param name="inviteCode">中文：可为空的邀请码；空值代表当前允许的非邀请注册。English: Optional invite code; an empty value represents currently allowed non-invite registration.</param>
        /// <param name="requiresApproval">中文：是否以待审核状态创建用户。English: Whether to create the user in pending-approval status.</param>
        /// <returns>中文：成功时返回新用户标识；审核表不可用或邀请码无效时返回 <c>-1</c>。English: New user identifier on success; <c>-1</c> when the review table is unavailable or the invite is invalid.</returns>
        public int AddSelfRegisteredUser(
            string fullName,
            string email,
            string password,
            string employeeCode,
            string inviteCode,
            bool requiresApproval)
        {
            if (!HasRegistrationTable())
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
                        Password = password
                    };

                    _context.Users.Add(item);
                    _context.SaveChanges();

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

            // 中文：批准时间和操作人仅保留审核事实，不记录密码或会话信息。
            // English: Approval time and operator retain only review facts, never password or session information.
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
        /// 中文：更新指定用户的邮箱和密码摘要。
        ///
        /// English: Updates the specified user's email and password digest.
        /// </summary>
        /// <param name="userId">中文：要更新的用户数值标识。English: Numeric identifier of the user to update.</param>
        /// <param name="email">中文：新的邮箱地址。English: New email address.</param>
        /// <param name="password">中文：新的既有格式密码摘要，不得写入审计正文或诊断日志。English: New legacy-format password digest; it must not be written to audit details or diagnostic logs.</param>
        public void UpdateUser(int userId, string email, string password)
        {
            var item = _context.Users.Single(i => i.UserId == userId);

            item.Email = email;
            item.Password = password;
            _context.SaveChanges();
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
            // 中文：待审核用户可能没有角色，返回空集合以保持后台可查看。
            // English: Pending users may have no roles; return an empty collection so administration UI remains viewable.
            var item = _context.Users.Single(i => i.Name == name);
            return item.Roles == null
                ? Enumerable.Empty<IRoleItem>()
                : item.Roles.ToList<IRoleItem>();
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
            // 中文：角色 Cookie 缺失或过期时由此集合重建请求级身份。
            // English: This collection rebuilds the request-level identity when the role cookie is missing or expired.
            var item = _context.Users.Single(i => i.Name == name);
            return item.Roles == null
                ? Enumerable.Empty<string>()
                : item.Roles.Select(i => i.RoleName);
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
        /// 中文：校验登录输入的摘要和注册审核状态。
        ///
        /// English: Validates the submitted sign-in digest and registration-review status.
        /// </summary>
        /// <param name="emailOrName">中文：用户输入的邮箱或登录名称。English: Email or sign-in name entered by the user.</param>
        /// <param name="password">中文：既有用户表兼容的密码摘要。English: Password digest compatible with the legacy user table.</param>
        /// <returns>中文：登录成功且允许访问时返回用户名；否则为空字符串。English: User name when sign-in succeeds and access is allowed; otherwise an empty string.</returns>
        public string Login(string emailOrName, string password)
        {
            // 中文：当前查询比较历史摘要；强哈希迁移应增加兼容验证层，而不能直接替换此条件。
            // English: The current query compares legacy digests; a strong-hash migration needs a compatibility verifier instead of directly replacing this condition.
            var item = _context.Users.SingleOrDefault(i => (i.Email == emailOrName || i.Name == emailOrName) && i.Password == password);

            // 中文：审核元数据可用时，只有已批准状态可登录。
            // English: When review metadata is available, only approved status may sign in.
            if (item != null && IsLoginAllowed(item.UserId))
            {
                return item.Name.Trim();
            }
            return string.Empty;
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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string NullIfEmpty(string value)
        {
            string normalized = Normalize(value);
            return string.IsNullOrEmpty(normalized) ? null : normalized;
        }
    }
}
