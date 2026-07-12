using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 用户注册审核元数据的只读视图。
    /// Read-only view of user registration review metadata.
    /// </summary>
    public interface IUserRegistrationInfo
    {
        /// <summary>
        /// 用户ID。
        /// User id.
        /// </summary>
        int UserId { get; }

        /// <summary>
        /// 审核状态。
        /// Review status.
        /// </summary>
        string Status { get; }

        /// <summary>
        /// 是否需要审核。
        /// Whether approval is required.
        /// </summary>
        bool RequiresApproval { get; }

        /// <summary>
        /// 员工号。
        /// Employee code.
        /// </summary>
        string EmployeeCode { get; }

        /// <summary>
        /// 注册邀请代码。
        /// Registration invitation code.
        /// </summary>
        string InviteCode { get; }

        /// <summary>
        /// 注册提交 UTC 时间。
        /// Registration submission UTC time.
        /// </summary>
        DateTime RegisteredUtc { get; }

        /// <summary>
        /// 批准 UTC 时间。
        /// Approval UTC time.
        /// </summary>
        DateTime? ApprovedUtc { get; }

        /// <summary>
        /// 批准人。
        /// Approval operator.
        /// </summary>
        string ApprovedBy { get; }

        /// <summary>
        /// 拒绝 UTC 时间。
        /// Rejection UTC time.
        /// </summary>
        DateTime? RejectedUtc { get; }

        /// <summary>
        /// 拒绝人。
        /// Rejection operator.
        /// </summary>
        string RejectedBy { get; }

        /// <summary>
        /// 审核备注。
        /// Review note.
        /// </summary>
        string ReviewNote { get; }

        /// <summary>
        /// 信息来源。
        /// Information source.
        /// </summary>
        string Source { get; }
    }
}
