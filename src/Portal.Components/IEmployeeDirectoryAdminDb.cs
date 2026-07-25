namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>员工和组织后台最小维护写入契约。</zh-CN>
    ///   <en>Minimal administration write contract for employees and organization units.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本契约与 <see cref="IEmployeeDirectoryDb"/> 分离，保持只读查询和写入维护的边界清晰。P6.3-S4 不负责账号员工绑定、员工工号登录或安全版本递增。</zh-CN>
    ///   <en>This contract is separate from <see cref="IEmployeeDirectoryDb"/> so read-only queries and administration writes remain clearly separated. P6.3-S4 does not handle user-employee binding, employee-code sign-in, or security-version increments.</en>
    /// </lang>
    /// </remarks>
    public interface IEmployeeDirectoryAdminDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>检查 P6.3 员工组织目录表是否可用于维护。</zh-CN>
        ///   <en>Checks whether the P6.3 employee-directory tables are available for maintenance.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>依赖表存在且可访问时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the required tables exist and can be accessed.</en>
        /// </l>
        /// </returns>
        bool IsSchemaAvailable();

        /// <summary>
        /// <lang>
        ///   <zh-CN>按标识读取一个组织单元。</zh-CN>
        ///   <en>Reads one organization unit by id.</en>
        /// </lang>
        /// </summary>
        /// <param name="organizationUnitId">
        /// <l>
        ///   <zh-CN>组织单元标识。</zh-CN>
        ///   <en>Organization-unit identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>组织单元只读信息；不存在时为空。</zh-CN>
        ///   <en>Read-only organization-unit information, or null when it does not exist.</en>
        /// </l>
        /// </returns>
        IOrganizationUnitInfo GetOrganizationUnitById(int organizationUnitId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>按标识读取一个员工。</zh-CN>
        ///   <en>Reads one employee by id.</en>
        /// </lang>
        /// </summary>
        /// <param name="employeeId">
        /// <l>
        ///   <zh-CN>员工标识。</zh-CN>
        ///   <en>Employee identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>员工只读信息；不存在时为空。</zh-CN>
        ///   <en>Read-only employee information, or null when it does not exist.</en>
        /// </l>
        /// </returns>
        IEmployeeInfo GetEmployeeById(int employeeId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>新增或更新组织单元。</zh-CN>
        ///   <en>Creates or updates an organization unit.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>组织单元保存请求，包含父级、编码、名称、排序和启用状态。</zh-CN>
        ///   <en>Organization-unit save request containing parent, code, name, sort order, and enabled state.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>写入结果，包含成功状态、目标标识和可显示错误。</zh-CN>
        ///   <en>Write result containing success state, target identifier, and displayable errors.</en>
        /// </l>
        /// </returns>
        EmployeeDirectoryWriteResult SaveOrganizationUnit(OrganizationUnitSaveRequest request);

        /// <summary>
        /// <lang>
        ///   <zh-CN>新增或更新员工主数据。</zh-CN>
        ///   <en>Creates or updates employee master data.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>员工保存请求，包含员工号、姓名、邮箱、组织、状态和更新并发信息。</zh-CN>
        ///   <en>Employee save request containing employee code, names, email, organization, status, and update concurrency data.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>写入结果，包含成功状态、目标标识和可显示错误。</zh-CN>
        ///   <en>Write result containing success state, target identifier, and displayable errors.</en>
        /// </l>
        /// </returns>
        EmployeeDirectoryWriteResult SaveEmployee(EmployeeSaveRequest request);
    }
}
