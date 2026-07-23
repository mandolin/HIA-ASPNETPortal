using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>员工、组织和门户账号绑定目录的只读数据访问契约。</zh-CN>
    ///   <en>Read-only data-access contract for employees, organization units, and Portal-user bindings.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P6.3-S2 只提供只读查询和缺表兼容。新增、编辑、绑定、解绑、审计和安全版本递增后续单独实现。</zh-CN>
    ///   <en>P6.3-S2 provides only read queries and missing-schema compatibility. Creation, editing, binding, unbinding, auditing, and security-version increments are implemented later.</en>
    /// </lang>
    /// </remarks>
    public interface IEmployeeDirectoryDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>检查 P6.3 员工组织目录表是否全部可用。</zh-CN>
        ///   <en>Checks whether all P6.3 employee-directory tables are available.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>三张基础表均存在时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when all three foundation tables exist.</en>
        /// </l>
        /// </returns>
        bool IsSchemaAvailable();

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取组织单元扁平列表。</zh-CN>
        ///   <en>Reads a flat organization-unit list.</en>
        /// </lang>
        /// </summary>
        /// <param name="query">
        /// <l>
        ///   <zh-CN>分页和过滤条件。</zh-CN>
        ///   <en>Paging and filtering options.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>组织单元只读集合；缺表时为空集合。</zh-CN>
        ///   <en>Read-only organization-unit collection; empty when schema is missing.</en>
        /// </l>
        /// </returns>
        IEnumerable<IOrganizationUnitInfo> GetOrganizationUnits(EmployeeDirectoryQuery query);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取员工主数据列表。</zh-CN>
        ///   <en>Reads employee master-data rows.</en>
        /// </lang>
        /// </summary>
        /// <param name="query">
        /// <l>
        ///   <zh-CN>分页和过滤条件。</zh-CN>
        ///   <en>Paging and filtering options.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>员工只读集合；缺表时为空集合。</zh-CN>
        ///   <en>Read-only employee collection; empty when schema is missing.</en>
        /// </l>
        /// </returns>
        IEnumerable<IEmployeeInfo> GetEmployees(EmployeeDirectoryQuery query);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取门户账号与员工绑定列表。</zh-CN>
        ///   <en>Reads Portal-user to employee binding rows.</en>
        /// </lang>
        /// </summary>
        /// <param name="query">
        /// <l>
        ///   <zh-CN>分页和过滤条件。</zh-CN>
        ///   <en>Paging and filtering options.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>绑定只读集合；缺表时为空集合。</zh-CN>
        ///   <en>Read-only binding collection; empty when schema is missing.</en>
        /// </l>
        /// </returns>
        IEnumerable<IUserEmployeeBindingInfo> GetUserEmployeeBindings(EmployeeDirectoryQuery query);

        /// <summary>
        /// <lang>
        ///   <zh-CN>按门户账号读取当前有效绑定。</zh-CN>
        ///   <en>Reads the current active binding by Portal user id.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>门户账号数值标识。</zh-CN>
        ///   <en>Numeric Portal user identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前有效绑定；缺表或不存在时为空。</zh-CN>
        ///   <en>Active binding, or null when schema is missing or no binding exists.</en>
        /// </l>
        /// </returns>
        IUserEmployeeBindingInfo GetActiveBindingByUserId(int userId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>按员工号读取当前有效绑定。</zh-CN>
        ///   <en>Reads the current active binding by employee code.</en>
        /// </lang>
        /// </summary>
        /// <param name="employeeCode">
        /// <l>
        ///   <zh-CN>员工号。</zh-CN>
        ///   <en>Employee code.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前有效绑定；缺表或不存在时为空。</zh-CN>
        ///   <en>Active binding, or null when schema is missing or no binding exists.</en>
        /// </l>
        /// </returns>
        IUserEmployeeBindingInfo GetActiveBindingByEmployeeCode(string employeeCode);
    }
}
