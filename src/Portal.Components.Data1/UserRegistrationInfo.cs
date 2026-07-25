using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>用户注册审核元数据 DTO。</zh-CN>
    ///     <en>User registration review metadata DTO.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该类型面向后台审核、注册状态展示和低敏提示。构造函数会把可空字符串归一化为空字符串，
    ///       避免旧 Web Forms 绑定层反复做空值判断；它不包含口令、Cookie 或异常细节。
    ///     </zh-CN>
    ///     <en>
    ///       This type is used by administration review, registration status display, and low-sensitivity
    ///       messages. The constructor normalizes nullable strings to empty strings so legacy Web Forms binding
    ///       code does not repeat null checks; it does not contain passwords, cookies, or exception details.
    ///     </en>
    ///   </lang>
    /// </remarks>
    public sealed class UserRegistrationInfo : IUserRegistrationInfo
    {
        /// <summary>
        ///   <lang>
        ///     <zh-CN>初始化用户注册审核元数据。</zh-CN>
        ///     <en>Initializes user registration review metadata.</en>
        ///   </lang>
        /// </summary>
        /// <param name="userId">
        ///   <l zh-CN="用户主键。" en="User primary key." />
        /// </param>
        /// <param name="status">
        ///   <l zh-CN="注册审核状态。" en="Registration review status." />
        /// </param>
        /// <param name="requiresApproval">
        ///   <l zh-CN="是否需要管理员审核。" en="Whether administrator approval is required." />
        /// </param>
        /// <param name="employeeCode">
        ///   <l zh-CN="注册时填写或绑定的员工工号。" en="Employee code entered or bound during registration." />
        /// </param>
        /// <param name="inviteCode">
        ///   <l zh-CN="注册邀请码或邀请批次标识。" en="Registration invitation code or invitation batch identifier." />
        /// </param>
        /// <param name="registeredUtc">
        ///   <l zh-CN="注册提交 UTC 时间。" en="UTC time when registration was submitted." />
        /// </param>
        /// <param name="approvedUtc">
        ///   <l zh-CN="批准 UTC 时间；未批准时为空。" en="UTC approval time; null when not approved." />
        /// </param>
        /// <param name="approvedBy">
        ///   <l zh-CN="批准操作者显示名或账号快照。" en="Approving operator display name or account snapshot." />
        /// </param>
        /// <param name="rejectedUtc">
        ///   <l zh-CN="拒绝 UTC 时间；未拒绝时为空。" en="UTC rejection time; null when not rejected." />
        /// </param>
        /// <param name="rejectedBy">
        ///   <l zh-CN="拒绝操作者显示名或账号快照。" en="Rejecting operator display name or account snapshot." />
        /// </param>
        /// <param name="reviewNote">
        ///   <l zh-CN="审核备注，调用方应避免写入敏感信息。" en="Review note; callers should avoid writing sensitive information." />
        /// </param>
        /// <param name="source">
        ///   <l zh-CN="注册信息来源。" en="Registration information source." />
        /// </param>
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
        ///   <l zh-CN="用户主键。" en="User primary key." />
        /// </summary>
        public int UserId { get; private set; }

        /// <summary>
        ///   <l zh-CN="注册审核状态。" en="Registration review status." />
        /// </summary>
        public string Status { get; private set; }

        /// <summary>
        ///   <l zh-CN="是否需要管理员审核。" en="Whether administrator approval is required." />
        /// </summary>
        public bool RequiresApproval { get; private set; }

        /// <summary>
        ///   <l zh-CN="注册时填写或绑定的员工工号。" en="Employee code entered or bound during registration." />
        /// </summary>
        public string EmployeeCode { get; private set; }

        /// <summary>
        ///   <l zh-CN="注册邀请码或邀请批次标识。" en="Registration invitation code or invitation batch identifier." />
        /// </summary>
        public string InviteCode { get; private set; }

        /// <summary>
        ///   <l zh-CN="注册提交 UTC 时间。" en="UTC time when registration was submitted." />
        /// </summary>
        public DateTime RegisteredUtc { get; private set; }

        /// <summary>
        ///   <l zh-CN="批准 UTC 时间；未批准时为空。" en="UTC approval time; null when not approved." />
        /// </summary>
        public DateTime? ApprovedUtc { get; private set; }

        /// <summary>
        ///   <l zh-CN="批准操作者显示名或账号快照。" en="Approving operator display name or account snapshot." />
        /// </summary>
        public string ApprovedBy { get; private set; }

        /// <summary>
        ///   <l zh-CN="拒绝 UTC 时间；未拒绝时为空。" en="UTC rejection time; null when not rejected." />
        /// </summary>
        public DateTime? RejectedUtc { get; private set; }

        /// <summary>
        ///   <l zh-CN="拒绝操作者显示名或账号快照。" en="Rejecting operator display name or account snapshot." />
        /// </summary>
        public string RejectedBy { get; private set; }

        /// <summary>
        ///   <l zh-CN="审核备注，调用方应避免写入敏感信息。" en="Review note; callers should avoid writing sensitive information." />
        /// </summary>
        public string ReviewNote { get; private set; }

        /// <summary>
        ///   <l zh-CN="注册信息来源。" en="Registration information source." />
        /// </summary>
        public string Source { get; private set; }
    }
}
