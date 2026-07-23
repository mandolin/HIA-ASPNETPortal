using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>用户注册审核元数据的只读视图。</zh-CN>
    ///   <en>Read-only view of user registration-review metadata.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>旧库或早期用户没有审核记录时，实现应返回兼容视图，而不是将其误判为待审核用户。</zh-CN>
    ///   <en>Implementations should return a compatible view for legacy databases or users without review records, rather than misclassifying them as pending approval.</en>
    /// </lang>
    /// </remarks>
    public interface IUserRegistrationInfo
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>用户数值标识。</zh-CN>
        ///   <en>Numeric user identifier.</en>
        /// </lang>
        /// </summary>
        int UserId { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>审核状态，使用 <see cref="PortalUserRegistrationStatuses"/> 中的稳定值。</zh-CN>
        ///   <en>Review status using stable values from <see cref="PortalUserRegistrationStatuses"/>.</en>
        /// </lang>
        /// </summary>
        string Status { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>该注册是否要求管理员审核。</zh-CN>
        ///   <en>Whether this registration requires administrator approval.</en>
        /// </lang>
        /// </summary>
        bool RequiresApproval { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>注册时提交的员工号，可为空。</zh-CN>
        ///   <en>Employee code submitted at registration; may be empty.</en>
        /// </lang>
        /// </summary>
        string EmployeeCode { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>注册时使用的邀请码；空值表示当前允许的非邀请注册。</zh-CN>
        ///   <en>Invite code used at registration; an empty value represents currently allowed non-invite registration.</en>
        /// </lang>
        /// </summary>
        string InviteCode { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>注册提交 UTC 时间；旧兼容记录可使用 <see cref="DateTime.MinValue"/>。</zh-CN>
        ///   <en>Registration-submission UTC time; legacy-compatible records may use <see cref="DateTime.MinValue"/>.</en>
        /// </lang>
        /// </summary>
        DateTime RegisteredUtc { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>批准 UTC 时间；尚未批准时为空。</zh-CN>
        ///   <en>Approval UTC time; empty when not approved.</en>
        /// </lang>
        /// </summary>
        DateTime? ApprovedUtc { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>批准操作人标识。</zh-CN>
        ///   <en>Approving operator identifier.</en>
        /// </lang>
        /// </summary>
        string ApprovedBy { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>拒绝 UTC 时间；尚未拒绝时为空。</zh-CN>
        ///   <en>Rejection UTC time; empty when not rejected.</en>
        /// </lang>
        /// </summary>
        DateTime? RejectedUtc { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>拒绝操作人标识。</zh-CN>
        ///   <en>Rejecting operator identifier.</en>
        /// </lang>
        /// </summary>
        string RejectedBy { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>审核备注；当前最小流程尚未提供在线填写入口。</zh-CN>
        ///   <en>Review note; the current minimal flow does not yet provide online entry.</en>
        /// </lang>
        /// </summary>
        string ReviewNote { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>审核信息来源，例如注册元数据或旧兼容回退。</zh-CN>
        ///   <en>Review-information source, such as registration metadata or a legacy compatibility fallback.</en>
        /// </lang>
        /// </summary>
        string Source { get; }
    }
}
