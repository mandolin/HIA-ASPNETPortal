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

        /// <summary>
        /// <lang>
        ///   <zh-CN>组织范围根节点的组织单元标识；为空表示不按组织过滤。该值只用于圈定查询范围，不授予任何组织访问权限。</zh-CN>
        ///   <en>Identifier of the organization unit that roots the query scope; null means no organization filtering. The value only bounds the query and grants no access to any organization.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>W68 新增：用于把目录查询从"全量扁平列表"推进到"可按组织圈定范围"。实现须按 fail-closed 处理——标识非正值时返回空集合而不是退化为全量。</zh-CN>
        ///   <en>Added in W68: it advances directory queries from an unbounded flat list to an organization-scoped query. Implementations must treat it as fail-closed: a non-positive identifier yields an empty set instead of degrading to a full scan.</en>
        /// </lang>
        /// </remarks>
        public int? OrganizationUnitId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否把组织范围扩展到根节点的全部后代；仅在与 <see cref="OrganizationUnitId"/> 同时提供时生效。</zh-CN>
        ///   <en>Whether the organization scope expands to all descendants of the root unit; it takes effect only together with <see cref="OrganizationUnitId"/>.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>W68 新增：开启后子树成员由数据层递归展开（Microsoft Learn 层级建模一手结论：邻接表下子树查询需递归 CTE 或层级索引配合），并受实现侧深度上限保护；关闭时只命中该组织本身。</zh-CN>
        ///   <en>Added in W68: when enabled, descendant members are expanded recursively by the data layer (per the Microsoft Learn hierarchy-modeling guidance: subtree queries over an adjacency list need a recursive CTE or hierarchy index), guarded by an implementation-side depth cap; when disabled, only the unit itself matches.</en>
        /// </lang>
        /// </remarks>
        public bool IncludeDescendants { get; set; }
    }
}
