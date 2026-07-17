namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：员工和组织后台最小维护写入契约。
    ///
    /// English: Minimal administration write contract for employees and organization units.
    /// </summary>
    /// <remarks>
    /// 中文：本契约与 <see cref="IEmployeeDirectoryDb"/> 分离，保持只读查询和写入维护的边界清晰。
    /// P6.3-S4 不负责账号员工绑定、员工工号登录或安全版本递增。
    ///
    /// English: This contract is separate from <see cref="IEmployeeDirectoryDb"/> so read-only queries and
    /// administration writes remain clearly separated. P6.3-S4 does not handle user-employee binding, employee-code
    /// sign-in, or security-version increments.
    /// </remarks>
    public interface IEmployeeDirectoryAdminDb
    {
        /// <summary>
        /// 中文：检查 P6.3 员工组织目录表是否可用于维护。
        ///
        /// English: Checks whether the P6.3 employee-directory tables are available for maintenance.
        /// </summary>
        bool IsSchemaAvailable();

        /// <summary>
        /// 中文：按标识读取一个组织单元。
        ///
        /// English: Reads one organization unit by id.
        /// </summary>
        IOrganizationUnitInfo GetOrganizationUnitById(int organizationUnitId);

        /// <summary>
        /// 中文：按标识读取一个员工。
        ///
        /// English: Reads one employee by id.
        /// </summary>
        IEmployeeInfo GetEmployeeById(int employeeId);

        /// <summary>
        /// 中文：新增或更新组织单元。
        ///
        /// English: Creates or updates an organization unit.
        /// </summary>
        EmployeeDirectoryWriteResult SaveOrganizationUnit(OrganizationUnitSaveRequest request);

        /// <summary>
        /// 中文：新增或更新员工主数据。
        ///
        /// English: Creates or updates employee master data.
        /// </summary>
        EmployeeDirectoryWriteResult SaveEmployee(EmployeeSaveRequest request);
    }
}
