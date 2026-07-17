using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：员工、组织和门户账号绑定目录的只读数据访问契约。
    ///
    /// English: Read-only data-access contract for employees, organization units, and Portal-user bindings.
    /// </summary>
    /// <remarks>
    /// 中文：P6.3-S2 只提供只读查询和缺表兼容。新增、编辑、绑定、解绑、审计和安全版本递增后续单独实现。
    ///
    /// English: P6.3-S2 provides only read queries and missing-schema compatibility. Creation, editing, binding,
    /// unbinding, auditing, and security-version increments are implemented later.
    /// </remarks>
    public interface IEmployeeDirectoryDb
    {
        /// <summary>
        /// 中文：检查 P6.3 员工组织目录表是否全部可用。
        ///
        /// English: Checks whether all P6.3 employee-directory tables are available.
        /// </summary>
        /// <returns>中文：三张基础表均存在时为 <c>true</c>。English: <c>true</c> when all three foundation tables exist.</returns>
        bool IsSchemaAvailable();

        /// <summary>
        /// 中文：读取组织单元扁平列表。
        ///
        /// English: Reads a flat organization-unit list.
        /// </summary>
        /// <param name="query">中文：分页和过滤条件。English: Paging and filtering options.</param>
        /// <returns>中文：组织单元只读集合；缺表时为空集合。English: Read-only organization-unit collection; empty when schema is missing.</returns>
        IEnumerable<IOrganizationUnitInfo> GetOrganizationUnits(EmployeeDirectoryQuery query);

        /// <summary>
        /// 中文：读取员工主数据列表。
        ///
        /// English: Reads employee master-data rows.
        /// </summary>
        /// <param name="query">中文：分页和过滤条件。English: Paging and filtering options.</param>
        /// <returns>中文：员工只读集合；缺表时为空集合。English: Read-only employee collection; empty when schema is missing.</returns>
        IEnumerable<IEmployeeInfo> GetEmployees(EmployeeDirectoryQuery query);

        /// <summary>
        /// 中文：读取门户账号与员工绑定列表。
        ///
        /// English: Reads Portal-user to employee binding rows.
        /// </summary>
        /// <param name="query">中文：分页和过滤条件。English: Paging and filtering options.</param>
        /// <returns>中文：绑定只读集合；缺表时为空集合。English: Read-only binding collection; empty when schema is missing.</returns>
        IEnumerable<IUserEmployeeBindingInfo> GetUserEmployeeBindings(EmployeeDirectoryQuery query);

        /// <summary>
        /// 中文：按门户账号读取当前有效绑定。
        ///
        /// English: Reads the current active binding by Portal user id.
        /// </summary>
        /// <param name="userId">中文：门户账号数值标识。English: Numeric Portal user identifier.</param>
        /// <returns>中文：当前有效绑定；缺表或不存在时为空。English: Active binding, or null when schema is missing or no binding exists.</returns>
        IUserEmployeeBindingInfo GetActiveBindingByUserId(int userId);

        /// <summary>
        /// 中文：按员工号读取当前有效绑定。
        ///
        /// English: Reads the current active binding by employee code.
        /// </summary>
        /// <param name="employeeCode">中文：员工号。English: Employee code.</param>
        /// <returns>中文：当前有效绑定；缺表或不存在时为空。English: Active binding, or null when schema is missing or no binding exists.</returns>
        IUserEmployeeBindingInfo GetActiveBindingByEmployeeCode(string employeeCode);
    }
}
