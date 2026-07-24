using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>员工主数据后台保存请求。</zh-CN>
    ///   <en>Administration save request for employee master data.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本请求只覆盖 P6.3 第一版最小字段，不承载手机号、身份证号、住址等高敏个人资料。</zh-CN>
    ///   <en>This request covers only the first-version P6.3 minimal fields and does not carry highly sensitive personal data such as phone numbers, government identifiers, or addresses.</en>
    /// </lang>
    /// </remarks>
    public sealed class EmployeeSaveRequest
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>员工标识；零表示新增。</zh-CN>
        ///   <en>Employee id; zero means create.</en>
        /// </lang>
        /// </summary>
        public int EmployeeId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>全局唯一员工号。</zh-CN>
        ///   <en>Globally unique employee code.</en>
        /// </lang>
        /// </summary>
        public string EmployeeCode { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>正式显示名。</zh-CN>
        ///   <en>Formal display name.</en>
        /// </lang>
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>偏好称呼或昵称。</zh-CN>
        ///   <en>Preferred name or nickname.</en>
        /// </lang>
        /// </summary>
        public string PreferredName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>工作邮箱。</zh-CN>
        ///   <en>Work email address.</en>
        /// </lang>
        /// </summary>
        public string WorkEmail { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>所属组织单元标识。</zh-CN>
        ///   <en>Owning organization-unit id.</en>
        /// </lang>
        /// </summary>
        public int? OrganizationUnitId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工生命周期状态。</zh-CN>
        ///   <en>Employee lifecycle status.</en>
        /// </lang>
        /// </summary>
        public string EmploymentStatus { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>入职时间 UTC。</zh-CN>
        ///   <en>Joined time in UTC.</en>
        /// </lang>
        /// </summary>
        public DateTime? JoinedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>离职时间 UTC。</zh-CN>
        ///   <en>Left time in UTC.</en>
        /// </lang>
        /// </summary>
        public DateTime? LeftUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>数据来源系统。</zh-CN>
        ///   <en>Source system.</en>
        /// </lang>
        /// </summary>
        public string SourceSystem { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新前读取到的 UTC 更新时间。</zh-CN>
        ///   <en>UTC update time read before editing.</en>
        /// </lang>
        /// </summary>
        public DateTime? OriginalUpdatedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前操作者标识。</zh-CN>
        ///   <en>Current actor identifier.</en>
        /// </lang>
        /// </summary>
        public string ActorName { get; set; }
    }
}
