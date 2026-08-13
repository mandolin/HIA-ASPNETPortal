using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>组织单元只读视图的默认实现。</zh-CN>
    ///     <en>Default implementation of the organization-unit read-only view.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该对象用于后台组织树、员工目录筛选和账号员工绑定页面展示组织信息。它是查询结果投影，
    ///       不负责判断组织编辑权限，也不表达多租户边界；权限和范围过滤应在调用它的数据访问或页面层完成。
    ///     </zh-CN>
    ///     <en>
    ///       This object is used by organization-tree administration, employee-directory filters, and user-employee
    ///       binding pages to display organization data. It is a query-result projection and does not decide
    ///       organization edit permission or tenant boundaries; permission and scope filtering must happen in the
    ///       data access or page layer that creates it.
    ///     </en>
    ///   </lang>
    /// </remarks>
    public sealed class OrganizationUnitInfo : IOrganizationUnitInfo
    {
        /// <summary>
        ///   <lang>
        ///     <zh-CN>创建组织单元只读视图。</zh-CN>
        ///     <en>Creates an organization-unit read-only view.</en>
        ///   </lang>
        /// </summary>
        /// <param name="organizationUnitId">
        ///   <l>
        ///     <zh-CN>组织单元主键。</zh-CN>
        ///     <en>Organization unit primary key.</en>
        ///   </l>
        /// </param>
        /// <param name="parentOrganizationUnitId">
        ///   <l>
        ///     <zh-CN>父级组织单元主键；为空表示根组织。</zh-CN>
        ///     <en>Parent organization unit key; null means this is a root unit.</en>
        ///   </l>
        /// </param>
        /// <param name="organizationCode">
        ///   <l>
        ///     <zh-CN>企业组织编码，用于业务识别和人工核对，不作为安全凭据。</zh-CN>
        ///     <en>Business organization code used for identification and manual review, not as a security credential.</en>
        ///   </l>
        /// </param>
        /// <param name="displayName">
        ///   <l>
        ///     <zh-CN>组织显示名称，输出到页面前仍需由展示层编码。</zh-CN>
        ///     <en>Organization display name; presentation code must still encode it before output.</en>
        ///   </l>
        /// </param>
        /// <param name="sortOrder">
        ///   <l>
        ///     <zh-CN>同级组织排序值。</zh-CN>
        ///     <en>Sort value among sibling organization units.</en>
        ///   </l>
        /// </param>
        /// <param name="isActive">
        ///   <l>
        ///     <zh-CN>是否启用；停用组织通常不应作为新增绑定的默认选择。</zh-CN>
        ///     <en>Whether the unit is active; inactive units should usually not be default choices for new bindings.</en>
        ///   </l>
        /// </param>
        /// <param name="createdUtc">
        ///   <l>
        ///     <zh-CN>创建时间，统一使用 UTC。</zh-CN>
        ///     <en>Creation time in UTC.</en>
        ///   </l>
        /// </param>
        /// <param name="updatedUtc">
        ///   <l>
        ///     <zh-CN>最后更新时间，统一使用 UTC。</zh-CN>
        ///     <en>Last update time in UTC.</en>
        ///   </l>
        /// </param>
        /// <remarks>
        ///   <lang>
        ///     <zh-CN>文本参数在构造时归一化为空字符串，避免旧 Web Forms 绑定控件遇到 <c>null</c> 后分散处理。</zh-CN>
        ///     <en>Text arguments are normalized to empty strings so legacy Web Forms binding controls do not need scattered null handling.</en>
        ///   </lang>
        /// </remarks>
        public OrganizationUnitInfo(
            int organizationUnitId,
            int? parentOrganizationUnitId,
            string organizationCode,
            string displayName,
            int sortOrder,
            bool isActive,
            DateTime createdUtc,
            DateTime updatedUtc)
        {
            OrganizationUnitId = organizationUnitId;
            ParentOrganizationUnitId = parentOrganizationUnitId;
            OrganizationCode = organizationCode ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            SortOrder = sortOrder;
            IsActive = isActive;
            CreatedUtc = createdUtc;
            UpdatedUtc = updatedUtc;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>组织单元数值标识。</zh-CN>
        ///   <en>Numeric organization-unit identifier.</en>
        /// </lang>
        /// </summary>
        public int OrganizationUnitId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>父级组织单元标识；顶级组织为空。</zh-CN>
        ///   <en>Parent organization-unit identifier; null for top-level units.</en>
        /// </lang>
        /// </summary>
        public int? ParentOrganizationUnitId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选组织编码。</zh-CN>
        ///   <en>Optional organization code.</en>
        /// </lang>
        /// </summary>
        public string OrganizationCode { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>组织显示名称。</zh-CN>
        ///   <en>Display name of the organization unit.</en>
        /// </lang>
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>同级排序值。</zh-CN>
        ///   <en>Sibling sort order.</en>
        /// </lang>
        /// </summary>
        public int SortOrder { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>组织单元是否启用。</zh-CN>
        ///   <en>Whether the organization unit is active.</en>
        /// </lang>
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建时间 UTC。</zh-CN>
        ///   <en>Creation time in UTC.</en>
        /// </lang>
        /// </summary>
        public DateTime CreatedUtc { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最近更新时间 UTC。</zh-CN>
        ///   <en>Last update time in UTC.</en>
        /// </lang>
        /// </summary>
        public DateTime UpdatedUtc { get; private set; }
    }
}
