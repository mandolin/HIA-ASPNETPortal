using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>门户账号与员工绑定只读视图的默认实现。</zh-CN>
    ///     <en>Default implementation of the Portal-user to employee binding read-only view.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该对象用于后台绑定列表、绑定详情和审计辅助展示。它只表达已经查询出的绑定状态，不负责判断当前操作者
    ///       是否可修改绑定，也不触发用户安全版本递增；这些动作属于后台写入服务或数据访问实现。
    ///     </zh-CN>
    ///     <en>
    ///       This object is used by binding lists, binding details, and audit-friendly administration displays. It
    ///       only represents the binding state that has already been queried and does not decide whether the current
    ///       operator can modify the binding or increment a user's security version; those actions belong to write
    ///       services or data access implementations.
    ///     </en>
    ///   </lang>
    /// </remarks>
    public sealed class UserEmployeeBindingInfo : IUserEmployeeBindingInfo
    {
        /// <summary>
        ///   <lang>
        ///     <zh-CN>创建绑定只读视图。</zh-CN>
        ///     <en>Creates a binding read-only view.</en>
        ///   </lang>
        /// </summary>
        /// <param name="bindingId">
        ///   <l>
        ///     <zh-CN>绑定记录主键。</zh-CN>
        ///     <en>Binding record primary key.</en>
        ///   </l>
        /// </param>
        /// <param name="userId">
        ///   <l>
        ///     <zh-CN>门户用户主键。</zh-CN>
        ///     <en>Portal user primary key.</en>
        ///   </l>
        /// </param>
        /// <param name="userName">
        ///   <l>
        ///     <zh-CN>门户用户名或登录名；仅用于展示和人工核对。</zh-CN>
        ///     <en>Portal user name or login name; used only for display and manual review.</en>
        ///   </l>
        /// </param>
        /// <param name="employeeId">
        ///   <l>
        ///     <zh-CN>员工主数据主键。</zh-CN>
        ///     <en>Employee master-data primary key.</en>
        ///   </l>
        /// </param>
        /// <param name="employeeCode">
        ///   <l>
        ///     <zh-CN>员工工号，可用于登录标识解析，但这里不作为凭据处理。</zh-CN>
        ///     <en>Employee code, which may be used by login identifier resolution but is not treated as a credential here.</en>
        ///   </l>
        /// </param>
        /// <param name="employeeDisplayName">
        ///   <l>
        ///     <zh-CN>员工显示名称，页面输出前仍需编码。</zh-CN>
        ///     <en>Employee display name; presentation code must still encode it before output.</en>
        ///   </l>
        /// </param>
        /// <param name="bindingStatus">
        ///   <l>
        ///     <zh-CN>绑定状态稳定字符串，如 Active 或 Ended。</zh-CN>
        ///     <en>Stable binding status string, such as Active or Ended.</en>
        ///   </l>
        /// </param>
        /// <param name="boundUtc">
        ///   <l>
        ///     <zh-CN>绑定建立时间，统一使用 UTC。</zh-CN>
        ///     <en>Binding creation time in UTC.</en>
        ///   </l>
        /// </param>
        /// <param name="boundBy">
        ///   <l>
        ///     <zh-CN>建立绑定的操作者显示值；用于审计展示，不作为授权依据。</zh-CN>
        ///     <en>Display value of the operator who created the binding; used for audit display, not authorization.</en>
        ///   </l>
        /// </param>
        /// <param name="endedUtc">
        ///   <l>
        ///     <zh-CN>绑定结束时间；仍处于有效绑定时为空。</zh-CN>
        ///     <en>Binding end time; null while the binding is still active.</en>
        ///   </l>
        /// </param>
        /// <param name="endedBy">
        ///   <l>
        ///     <zh-CN>结束绑定的操作者显示值；用于审计展示。</zh-CN>
        ///     <en>Display value of the operator who ended the binding; used for audit display.</en>
        ///   </l>
        /// </param>
        /// <param name="reason">
        ///   <l>
        ///     <zh-CN>绑定变更原因或备注；显示前需编码，且不应包含密码等敏感信息。</zh-CN>
        ///     <en>Binding change reason or note; encode before display and do not include passwords or similar secrets.</en>
        ///   </l>
        /// </param>
        /// <remarks>
        ///   <lang>
        ///     <zh-CN>构造函数把可空文本归一化为空字符串，使后台 Grid/DataList 绑定保持旧 Web Forms 的稳定输出习惯。</zh-CN>
        ///     <en>The constructor normalizes nullable text to empty strings so administration Grid/DataList binding preserves legacy Web Forms output behavior.</en>
        ///   </lang>
        /// </remarks>
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
