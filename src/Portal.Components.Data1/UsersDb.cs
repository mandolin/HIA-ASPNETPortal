using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    public class UsersDb : IUsersDb
    {
        private readonly PortalSecurityDbContext _context;

        /// <summary>
        /// 初始化用户数据库操作类。
        /// </summary>
        /// <param name="context">数据库上下文。</param>
        public UsersDb(PortalSecurityDbContext context)
        {
            _context = context;
        }

        #region IUsersDb Members

        /// <summary>
        /// 添加用户。
        /// </summary>
        /// <param name="fullName">用户全名。</param>
        /// <param name="email">用户邮箱。</param>
        /// <param name="password">用户密码。</param>
        /// <returns>返回添加的用户ID，如果添加失败则返回-1。</returns>
        public int AddUser(string fullName, string email, string password)
        {
            // 创建一个新的用户对象并设置属性。
            var item = new UserItem
            {
                Name = fullName,
                Email = email,
                Password = password
            };

            // 执行命令并捕获可能的异常（例如用户名重复）。
            try
            {
                // 将新的用户对象添加到数据库上下文中。
                _context.Users.Add(item);

                // 保存更改到数据库。
                _context.SaveChanges();

                // 管理员或旧入口创建的用户默认视为已批准；如果注册元数据表尚未部署，则保持 legacy 行为。
                // Admin/legacy-created users are treated as approved; missing metadata table keeps legacy behavior.
                TryEnsureApprovedRegistration(item.UserId, "system-admin");
            }
            catch (Exception)
            {
                // 如果发生错误，则返回-1表示添加失败。
                return -1;
            }

            // 返回成功添加的用户的ID。
            return item.UserId;
        }

        /// <summary>
        /// 添加自主注册用户，并写入注册审核元数据。
        /// </summary>
        /// <param name="fullName">用户名。</param>
        /// <param name="email">邮箱。</param>
        /// <param name="password">加密后的密码。</param>
        /// <param name="employeeCode">员工号，可为空。</param>
        /// <param name="inviteCode">注册链接代码，可为空。</param>
        /// <param name="requiresApproval">是否需要管理员审核。</param>
        /// <returns>返回添加的用户ID；失败返回 -1。</returns>
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
        /// 删除指定ID的用户。
        /// </summary>
        /// <param name="userId">用户ID。</param>
        public void DeleteUser(int userId)
        {
            // 先删除注册审核元数据，避免显式 FK 部署后阻止旧删除流程。
            // Delete review metadata first so explicit FK deployments do not block legacy delete flow.
            TryDeleteRegistration(userId);

            // 通过用户ID获取用户对象。
            var item = _context.Users.Single(i => i.UserId == userId);

            // 从数据库上下文中移除用户对象。
            _context.Users.Remove(item);

            // 保存更改到数据库。
            _context.SaveChanges();
        }

        /// <summary>
        /// 批准用户注册。
        /// </summary>
        /// <param name="userId">用户ID。</param>
        /// <param name="approvedBy">批准人。</param>
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
        }

        /// <summary>
        /// 拒绝处于待审核状态的用户注册；管理员后续仍可重新批准。
        /// Rejects a pending user registration; an administrator may approve it later.
        /// </summary>
        /// <param name="userId">用户编号。User id.</param>
        /// <param name="rejectedBy">拒绝操作人。Rejecting operator.</param>
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
        /// 读取用户注册审核信息；旧库或旧用户按 legacy approved 展示。
        /// </summary>
        /// <param name="userId">用户ID。</param>
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
        /// 校验临时注册链接。
        /// </summary>
        /// <param name="inviteCode">邀请代码。</param>
        /// <param name="message">失败说明。</param>
        public bool ValidateRegistrationInvite(string inviteCode, out string message)
        {
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
        /// 更新指定ID的用户信息。
        /// </summary>
        /// <param name="userId">用户ID。</param>
        /// <param name="email">新邮箱地址。</param>
        /// <param name="password">新密码。</param>
        public void UpdateUser(int userId, string email, string password)
        {
            // 通过用户ID获取用户对象。
            var item = _context.Users.Single(i => i.UserId == userId);

            // 更新用户的邮箱和密码。
            item.Email = email;
            item.Password = password;

            // 保存更改到数据库。
            _context.SaveChanges();
        }

        /// <summary>
        /// 获取指定用户名的所有角色。
        /// </summary>
        /// <param name="name">用户名。</param>
        /// <returns>角色集合。</returns>
        public IEnumerable<IRoleItem> GetRolesByUser(string name) 
        {
            // 自注册待审核用户可能尚未分配任何角色，此时返回空集合保持后台页面可查看。
            var item = _context.Users.Single(i => i.Name == name);
            return item.Roles == null
                ? Enumerable.Empty<IRoleItem>()
                : item.Roles.ToList<IRoleItem>();
        }

        /// <summary>
        /// 获取指定用户名的所有角色名称。
        /// </summary>
        /// <param name="name">用户名。</param>
        /// <returns>角色名称集合。</returns>
        public IEnumerable<string> GetRoleNamesByUser(string name)
        {
            // 通过用户名获取用户对象，并选择其所有角色的名称；无角色用户返回空集合。
            var item = _context.Users.Single(i => i.Name == name);
            return item.Roles == null
                ? Enumerable.Empty<string>()
                : item.Roles.Select(i => i.RoleName);
        }

        /// <summary>
        /// 获取单个用户。
        /// </summary>
        /// <param name="name">用户名。</param>
        /// <returns>用户对象。</returns>
        public IUserItem GetSingleUser(string name)
        {
            // 通过用户名获取用户对象。
            return _context.Users.Single(i => i.Name == name);
        }

        /// <summary>
        /// 登录用户。
        /// </summary>
        /// <param name="emailOrName">邮箱或用户名。</param>
        /// <param name="password">密码。</param>
        /// <returns>登录成功的用户名，或空字符串表示登录失败。</returns>
        public string Login(string emailOrName, string password)
        {
            // 尝试通过邮箱或用户名以及密码查找用户。
            var item = _context.Users.SingleOrDefault(i => (i.Email == emailOrName || i.Name == emailOrName) && i.Password == password);

            // 如果找到了匹配的用户且注册审核允许登录，则返回用户名；否则返回空字符串。
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
                    return true;
                }

                var registration = _context.UserRegistrations.SingleOrDefault(i => i.UserId == userId);
                return registration == null ||
                       registration.Status == PortalUserRegistrationStatuses.Approved;
            }
            catch (Exception)
            {
                // 审核元数据读取失败时保持旧登录行为，避免未完成迁移导致全站锁死。
                // Keep legacy login behavior if review metadata cannot be read, avoiding full lockout during migration.
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
                // 管理员旧入口不能因为扩展元数据失败而中断。
                // Legacy admin add-user flow should not be blocked by optional review metadata.
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
                // 让后续删除流程继续抛出真实失败，避免在这里吞掉主流程错误。
                // Let the main delete flow surface the real failure if metadata cleanup was not enough.
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
