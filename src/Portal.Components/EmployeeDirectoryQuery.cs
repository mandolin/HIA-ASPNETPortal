namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>员工组织目录只读查询条件。</zh-CN>
    ///   <en>Read-only query options for the employee and organization directory.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本类型只承载分页、关键字和状态过滤；写入、导入和绑定变更将在 P6.3 后续切片单独设计。</zh-CN>
    ///   <en>This type carries only paging, keyword, and status filters; writes, imports, and binding changes are designed in later P6.3 slices.</en>
    /// </lang>
    /// </remarks>
    public sealed class EmployeeDirectoryQuery
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>关键字，可匹配员工号、员工名、邮箱、组织编码或组织名。</zh-CN>
        ///   <en>Keyword that may match employee code, employee name, email, organization code, or organization name.</en>
        /// </lang>
        /// </summary>
        public string Keyword { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>目标状态；员工查询使用员工状态，绑定查询使用绑定状态。</zh-CN>
        ///   <en>Target status; employee queries use employee status while binding queries use binding status.</en>
        /// </lang>
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>跳过的记录数，小于零时由实现按零处理。</zh-CN>
        ///   <en>Number of rows to skip; implementations treat negative values as zero.</en>
        /// </lang>
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最多返回记录数；实现会限制过大的值。</zh-CN>
        ///   <en>Maximum rows to return; implementations cap excessive values.</en>
        /// </lang>
        /// </summary>
        public int Take { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否包含非启用组织。</zh-CN>
        ///   <en>Whether inactive organization units should be included.</en>
        /// </lang>
        /// </summary>
        public bool IncludeInactiveOrganizations { get; set; }
    }
}
