using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>组织单元后台保存请求。</zh-CN>
    ///   <en>Administration save request for an organization unit.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>`OrganizationUnitId` 为零表示新增。更新时必须携带页面读取到的 `OriginalUpdatedUtc`，由数据层执行轻量并发保护。父级存在性、自引用、循环关系和名称必填仍在数据层统一校验。</zh-CN>
    ///   <en>An `OrganizationUnitId` of zero means creation. Updates must carry the `OriginalUpdatedUtc` read by the page so the data layer can perform lightweight concurrency protection. Parent existence, self-parenting, cycles and required names are still validated centrally by the data layer.</en>
    /// </lang>
    /// </remarks>
    public sealed class OrganizationUnitSaveRequest
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>组织单元标识；零表示新增，正数表示编辑既有组织。</zh-CN>
        ///   <en>Organization-unit id; zero means create and a positive value means editing an existing organization.</en>
        /// </lang>
        /// </summary>
        public int OrganizationUnitId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>父级组织单元标识；为空表示根组织。</zh-CN>
        ///   <en>Parent organization-unit id; null means a root organization.</en>
        /// </lang>
        /// </summary>
        public int? ParentOrganizationUnitId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选组织编码；目前不作为登录凭据，仅用于业务识别和外部同步映射。</zh-CN>
        ///   <en>Optional organization code; it is not a login credential and is used only for business identification and external synchronization mapping.</en>
        /// </lang>
        /// </summary>
        public string OrganizationCode { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>组织显示名称；保存前必须是非空文本。</zh-CN>
        ///   <en>Organization display name; it must be non-empty before saving.</en>
        /// </lang>
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>同级排序值；数据层会按整数值持久化，不负责自动重排兄弟节点。</zh-CN>
        ///   <en>Sibling sort order; the data layer persists the integer value and does not automatically reorder sibling nodes.</en>
        /// </lang>
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>组织是否启用；停用不等同于删除，历史员工仍可保留引用。</zh-CN>
        ///   <en>Whether the organization unit is active; disabling is not deletion and historical employees may keep references.</en>
        /// </lang>
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新前读取到的 UTC 更新时间；编辑路径缺失时会被视为并发保护失败。</zh-CN>
        ///   <en>UTC update time read before editing; missing values on edit paths are treated as concurrency-protection failures.</en>
        /// </lang>
        /// </summary>
        public DateTime? OriginalUpdatedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前操作者标识；用于审计字段，不参与权限判定。</zh-CN>
        ///   <en>Current actor identifier used by audit fields and not by permission decisions.</en>
        /// </lang>
        /// </summary>
        public string ActorName { get; set; }
    }
}
