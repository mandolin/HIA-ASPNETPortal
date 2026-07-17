using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：员工主数据只读视图，表示门户业务所需的最小员工信息。
    ///
    /// English: Read-only employee master-data view that represents the minimal employee information required by Portal business flows.
    /// </summary>
    public interface IEmployeeInfo
    {
        /// <summary>
        /// 中文：员工数值标识。
        ///
        /// English: Numeric employee identifier.
        /// </summary>
        int EmployeeId { get; }

        /// <summary>
        /// 中文：全局唯一员工号；可作为后续可选登录标识，但不是认证凭据。
        ///
        /// English: Globally unique employee code; it may become an optional sign-in identifier later but is not a credential.
        /// </summary>
        string EmployeeCode { get; }

        /// <summary>
        /// 中文：员工正式显示名。
        ///
        /// English: Formal employee display name.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 中文：偏好称呼或昵称。
        ///
        /// English: Preferred name or nickname.
        /// </summary>
        string PreferredName { get; }

        /// <summary>
        /// 中文：工作邮箱，可为空且第一版不保证唯一。
        ///
        /// English: Work email address; optional and not guaranteed to be unique in the first version.
        /// </summary>
        string WorkEmail { get; }

        /// <summary>
        /// 中文：所属组织单元标识。
        ///
        /// English: Owning organization-unit identifier.
        /// </summary>
        int? OrganizationUnitId { get; }

        /// <summary>
        /// 中文：所属组织显示名称，缺少组织或未部署组织表时为空。
        ///
        /// English: Owning organization display name, empty when the organization is absent or the organization table is not deployed.
        /// </summary>
        string OrganizationDisplayName { get; }

        /// <summary>
        /// 中文：员工生命周期状态。
        ///
        /// English: Employee lifecycle status.
        /// </summary>
        string EmploymentStatus { get; }

        /// <summary>
        /// 中文：入职时间 UTC，可为空。
        ///
        /// English: Joined time in UTC, when known.
        /// </summary>
        DateTime? JoinedUtc { get; }

        /// <summary>
        /// 中文：离职时间 UTC，可为空。
        ///
        /// English: Left time in UTC, when known.
        /// </summary>
        DateTime? LeftUtc { get; }

        /// <summary>
        /// 中文：数据来源系统标识。
        ///
        /// English: Source-system identifier.
        /// </summary>
        string SourceSystem { get; }
    }
}
