using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户账号与员工绑定的只读视图。
    ///
    /// English: Read-only view of a Portal-user to employee binding.
    /// </summary>
    public interface IUserEmployeeBindingInfo
    {
        /// <summary>
        /// 中文：绑定记录数值标识。
        ///
        /// English: Numeric binding identifier.
        /// </summary>
        int BindingId { get; }

        /// <summary>
        /// 中文：门户账号标识。
        ///
        /// English: Portal user identifier.
        /// </summary>
        int UserId { get; }

        /// <summary>
        /// 中文：旧门户用户名。
        ///
        /// English: Legacy Portal user name.
        /// </summary>
        string UserName { get; }

        /// <summary>
        /// 中文：员工标识。
        ///
        /// English: Employee identifier.
        /// </summary>
        int EmployeeId { get; }

        /// <summary>
        /// 中文：员工号。
        ///
        /// English: Employee code.
        /// </summary>
        string EmployeeCode { get; }

        /// <summary>
        /// 中文：员工显示名。
        ///
        /// English: Employee display name.
        /// </summary>
        string EmployeeDisplayName { get; }

        /// <summary>
        /// 中文：绑定生命周期状态。
        ///
        /// English: Binding lifecycle status.
        /// </summary>
        string BindingStatus { get; }

        /// <summary>
        /// 中文：绑定创建时间 UTC。
        ///
        /// English: Binding creation time in UTC.
        /// </summary>
        DateTime BoundUtc { get; }

        /// <summary>
        /// 中文：绑定创建人标识。
        ///
        /// English: Binding creator identifier.
        /// </summary>
        string BoundBy { get; }

        /// <summary>
        /// 中文：绑定结束时间 UTC，可为空。
        ///
        /// English: Binding end time in UTC, when known.
        /// </summary>
        DateTime? EndedUtc { get; }

        /// <summary>
        /// 中文：绑定结束人标识。
        ///
        /// English: Binding ending-operator identifier.
        /// </summary>
        string EndedBy { get; }

        /// <summary>
        /// 中文：非敏感绑定说明。
        ///
        /// English: Non-sensitive binding reason.
        /// </summary>
        string Reason { get; }
    }
}
