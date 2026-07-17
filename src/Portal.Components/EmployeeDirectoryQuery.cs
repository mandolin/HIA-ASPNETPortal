namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：员工组织目录只读查询条件。
    ///
    /// English: Read-only query options for the employee and organization directory.
    /// </summary>
    /// <remarks>
    /// 中文：本类型只承载分页、关键字和状态过滤；写入、导入和绑定变更将在 P6.3 后续切片单独设计。
    ///
    /// English: This type carries only paging, keyword, and status filters; writes, imports, and binding changes are designed in later P6.3 slices.
    /// </remarks>
    public sealed class EmployeeDirectoryQuery
    {
        /// <summary>
        /// 中文：关键字，可匹配员工号、员工名、邮箱、组织编码或组织名。
        ///
        /// English: Keyword that may match employee code, employee name, email, organization code, or organization name.
        /// </summary>
        public string Keyword { get; set; }

        /// <summary>
        /// 中文：目标状态；员工查询使用员工状态，绑定查询使用绑定状态。
        ///
        /// English: Target status; employee queries use employee status while binding queries use binding status.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 中文：跳过的记录数，小于零时由实现按零处理。
        ///
        /// English: Number of rows to skip; implementations treat negative values as zero.
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// 中文：最多返回记录数；实现会限制过大的值。
        ///
        /// English: Maximum rows to return; implementations cap excessive values.
        /// </summary>
        public int Take { get; set; }

        /// <summary>
        /// 中文：是否包含非启用组织。
        ///
        /// English: Whether inactive organization units should be included.
        /// </summary>
        public bool IncludeInactiveOrganizations { get; set; }
    }
}
