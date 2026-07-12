using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 用户注册审核元数据 DTO。
    /// User registration review metadata DTO.
    /// </summary>
    public sealed class UserRegistrationInfo : IUserRegistrationInfo
    {
        public UserRegistrationInfo(
            int userId,
            string status,
            bool requiresApproval,
            string employeeCode,
            string inviteCode,
            DateTime registeredUtc,
            DateTime? approvedUtc,
            string approvedBy,
            DateTime? rejectedUtc,
            string rejectedBy,
            string reviewNote,
            string source)
        {
            UserId = userId;
            Status = status ?? string.Empty;
            RequiresApproval = requiresApproval;
            EmployeeCode = employeeCode ?? string.Empty;
            InviteCode = inviteCode ?? string.Empty;
            RegisteredUtc = registeredUtc;
            ApprovedUtc = approvedUtc;
            ApprovedBy = approvedBy ?? string.Empty;
            RejectedUtc = rejectedUtc;
            RejectedBy = rejectedBy ?? string.Empty;
            ReviewNote = reviewNote ?? string.Empty;
            Source = source ?? string.Empty;
        }

        /// <summary>
        /// 用户ID。
        /// User id.
        /// </summary>
        public int UserId { get; private set; }

        /// <summary>
        /// 审核状态。
        /// Review status.
        /// </summary>
        public string Status { get; private set; }

        /// <summary>
        /// 是否需要审核。
        /// Whether approval is required.
        /// </summary>
        public bool RequiresApproval { get; private set; }

        /// <summary>
        /// 员工号。
        /// Employee code.
        /// </summary>
        public string EmployeeCode { get; private set; }

        /// <summary>
        /// 注册邀请代码。
        /// Registration invitation code.
        /// </summary>
        public string InviteCode { get; private set; }

        /// <summary>
        /// 注册提交 UTC 时间。
        /// Registration submission UTC time.
        /// </summary>
        public DateTime RegisteredUtc { get; private set; }

        /// <summary>
        /// 批准 UTC 时间。
        /// Approval UTC time.
        /// </summary>
        public DateTime? ApprovedUtc { get; private set; }

        /// <summary>
        /// 批准人。
        /// Approval operator.
        /// </summary>
        public string ApprovedBy { get; private set; }

        /// <summary>
        /// 拒绝 UTC 时间。
        /// Rejection UTC time.
        /// </summary>
        public DateTime? RejectedUtc { get; private set; }

        /// <summary>
        /// 拒绝人。
        /// Rejection operator.
        /// </summary>
        public string RejectedBy { get; private set; }

        /// <summary>
        /// 审核备注。
        /// Review note.
        /// </summary>
        public string ReviewNote { get; private set; }

        /// <summary>
        /// 信息来源。
        /// Information source.
        /// </summary>
        public string Source { get; private set; }
    }
}
