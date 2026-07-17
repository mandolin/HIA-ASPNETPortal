using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户账号与员工绑定只读视图的默认实现。
    ///
    /// English: Default implementation of the Portal-user to employee binding read-only view.
    /// </summary>
    public sealed class UserEmployeeBindingInfo : IUserEmployeeBindingInfo
    {
        /// <summary>
        /// 中文：创建绑定只读视图。
        ///
        /// English: Creates a binding read-only view.
        /// </summary>
        public UserEmployeeBindingInfo(
            int bindingId,
            int userId,
            string userName,
            int employeeId,
            string employeeCode,
            string employeeDisplayName,
            string bindingStatus,
            DateTime boundUtc,
            string boundBy,
            DateTime? endedUtc,
            string endedBy,
            string reason)
        {
            BindingId = bindingId;
            UserId = userId;
            UserName = userName ?? string.Empty;
            EmployeeId = employeeId;
            EmployeeCode = employeeCode ?? string.Empty;
            EmployeeDisplayName = employeeDisplayName ?? string.Empty;
            BindingStatus = bindingStatus ?? string.Empty;
            BoundUtc = boundUtc;
            BoundBy = boundBy ?? string.Empty;
            EndedUtc = endedUtc;
            EndedBy = endedBy ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        /// <inheritdoc />
        public int BindingId { get; private set; }

        /// <inheritdoc />
        public int UserId { get; private set; }

        /// <inheritdoc />
        public string UserName { get; private set; }

        /// <inheritdoc />
        public int EmployeeId { get; private set; }

        /// <inheritdoc />
        public string EmployeeCode { get; private set; }

        /// <inheritdoc />
        public string EmployeeDisplayName { get; private set; }

        /// <inheritdoc />
        public string BindingStatus { get; private set; }

        /// <inheritdoc />
        public DateTime BoundUtc { get; private set; }

        /// <inheritdoc />
        public string BoundBy { get; private set; }

        /// <inheritdoc />
        public DateTime? EndedUtc { get; private set; }

        /// <inheritdoc />
        public string EndedBy { get; private set; }

        /// <inheritdoc />
        public string Reason { get; private set; }
    }
}
