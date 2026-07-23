using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>员工主数据只读视图，表示门户业务所需的最小员工信息。</zh-CN>
    ///   <en>Read-only employee master-data view that represents the minimal employee information required by Portal business flows.</en>
    /// </lang>
    /// </summary>
    public interface IEmployeeInfo
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>员工数值标识。</zh-CN>
        ///   <en>Numeric employee identifier.</en>
        /// </lang>
        /// </summary>
        int EmployeeId { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>全局唯一员工号；可作为后续可选登录标识，但不是认证凭据。</zh-CN>
        ///   <en>Globally unique employee code; it may become an optional sign-in identifier later but is not a credential.</en>
        /// </lang>
        /// </summary>
        string EmployeeCode { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工正式显示名。</zh-CN>
        ///   <en>Formal employee display name.</en>
        /// </lang>
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>偏好称呼或昵称。</zh-CN>
        ///   <en>Preferred name or nickname.</en>
        /// </lang>
        /// </summary>
        string PreferredName { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>工作邮箱，可为空且第一版不保证唯一。</zh-CN>
        ///   <en>Work email address; optional and not guaranteed to be unique in the first version.</en>
        /// </lang>
        /// </summary>
        string WorkEmail { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>所属组织单元标识。</zh-CN>
        ///   <en>Owning organization-unit identifier.</en>
        /// </lang>
        /// </summary>
        int? OrganizationUnitId { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>所属组织显示名称，缺少组织或未部署组织表时为空。</zh-CN>
        ///   <en>Owning organization display name, empty when the organization is absent or the organization table is not deployed.</en>
        /// </lang>
        /// </summary>
        string OrganizationDisplayName { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工生命周期状态。</zh-CN>
        ///   <en>Employee lifecycle status.</en>
        /// </lang>
        /// </summary>
        string EmploymentStatus { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>入职时间 UTC，可为空。</zh-CN>
        ///   <en>Joined time in UTC, when known.</en>
        /// </lang>
        /// </summary>
        DateTime? JoinedUtc { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>离职时间 UTC，可为空。</zh-CN>
        ///   <en>Left time in UTC, when known.</en>
        /// </lang>
        /// </summary>
        DateTime? LeftUtc { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>数据来源系统标识。</zh-CN>
        ///   <en>Source-system identifier.</en>
        /// </lang>
        /// </summary>
        string SourceSystem { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最近更新时间 UTC，用于后台编辑页的轻量并发检查。</zh-CN>
        ///   <en>Last update time in UTC, used by administration edit pages for lightweight concurrency checks.</en>
        /// </lang>
        /// </summary>
        DateTime UpdatedUtc { get; }
    }
}
