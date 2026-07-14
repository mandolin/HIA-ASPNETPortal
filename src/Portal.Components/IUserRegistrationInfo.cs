using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：用户注册审核元数据的只读视图。
    ///
    /// English: Read-only view of user registration-review metadata.
    /// </summary>
    /// <remarks>
    /// 中文：旧库或早期用户没有审核记录时，实现应返回兼容视图，而不是将其误判为待审核用户。
    ///
    /// English: Implementations should return a compatible view for legacy databases or users without review records,
    /// rather than misclassifying them as pending approval.
    /// </remarks>
    public interface IUserRegistrationInfo
    {
        /// <summary>
        /// 中文：用户数值标识。
        ///
        /// English: Numeric user identifier.
        /// </summary>
        int UserId { get; }

        /// <summary>
        /// 中文：审核状态，使用 <see cref="PortalUserRegistrationStatuses"/> 中的稳定值。
        ///
        /// English: Review status using stable values from <see cref="PortalUserRegistrationStatuses"/>.
        /// </summary>
        string Status { get; }

        /// <summary>
        /// 中文：该注册是否要求管理员审核。
        ///
        /// English: Whether this registration requires administrator approval.
        /// </summary>
        bool RequiresApproval { get; }

        /// <summary>
        /// 中文：注册时提交的员工号，可为空。
        ///
        /// English: Employee code submitted at registration; may be empty.
        /// </summary>
        string EmployeeCode { get; }

        /// <summary>
        /// 中文：注册时使用的邀请码；空值表示当前允许的非邀请注册。
        ///
        /// English: Invite code used at registration; an empty value represents currently allowed non-invite registration.
        /// </summary>
        string InviteCode { get; }

        /// <summary>
        /// 中文：注册提交 UTC 时间；旧兼容记录可使用 <see cref="DateTime.MinValue"/>。
        ///
        /// English: Registration-submission UTC time; legacy-compatible records may use <see cref="DateTime.MinValue"/>.
        /// </summary>
        DateTime RegisteredUtc { get; }

        /// <summary>
        /// 中文：批准 UTC 时间；尚未批准时为空。
        ///
        /// English: Approval UTC time; empty when not approved.
        /// </summary>
        DateTime? ApprovedUtc { get; }

        /// <summary>
        /// 中文：批准操作人标识。
        ///
        /// English: Approving operator identifier.
        /// </summary>
        string ApprovedBy { get; }

        /// <summary>
        /// 中文：拒绝 UTC 时间；尚未拒绝时为空。
        ///
        /// English: Rejection UTC time; empty when not rejected.
        /// </summary>
        DateTime? RejectedUtc { get; }

        /// <summary>
        /// 中文：拒绝操作人标识。
        ///
        /// English: Rejecting operator identifier.
        /// </summary>
        string RejectedBy { get; }

        /// <summary>
        /// 中文：审核备注；当前最小流程尚未提供在线填写入口。
        ///
        /// English: Review note; the current minimal flow does not yet provide online entry.
        /// </summary>
        string ReviewNote { get; }

        /// <summary>
        /// 中文：审核信息来源，例如注册元数据或旧兼容回退。
        ///
        /// English: Review-information source, such as registration metadata or a legacy compatibility fallback.
        /// </summary>
        string Source { get; }
    }
}
