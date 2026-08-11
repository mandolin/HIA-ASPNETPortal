using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>公告模块条目的 Entity Framework 投影。</zh-CN>
    ///     <en>Entity Framework projection for an announcement module item.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该类型映射旧表 <c>Portal_Announcements</c>。它只承载数据库字段语义，不执行 URL
    ///       白名单、HTML 编码或过期过滤；这些规则由数据访问实现和 Web Forms 展示层分别处理。
    ///     </zh-CN>
    ///     <en>
    ///       This type maps the legacy <c>Portal_Announcements</c> table. It only carries database field
    ///       semantics and does not enforce URL allow-listing, HTML encoding, or expiry filtering; those rules
    ///       are applied by the data access implementation and Web Forms presentation layer.
    ///     </en>
    ///   </lang>
    /// </remarks>
    [Table("Portal_Announcements")]
    public class AnnouncementItem : IAnnouncementItem
    {
        #region IAnnouncementItem Members

        /// <summary>
        ///   <l>
        ///     <zh-CN>公告条目的数据库主键。</zh-CN>
        ///     <en>Database primary key for the announcement item.</en>
        ///   </l>
        /// </summary>
        [Key]
        public int ItemId { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>拥有该公告条目的模块实例标识。</zh-CN>
        ///     <en>Module instance identifier that owns this announcement item.</en>
        ///   </l>
        /// </summary>
        public int ModuleId { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>创建人显示名称；来自旧内容模块保存流程，不作为授权依据。</zh-CN>
        ///     <en>Display name of the creator from the legacy content save flow; not an authorization source.</en>
        ///   </l>
        /// </summary>
        public string CreatedByUser { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>公告创建时间；旧数据可能为空。</zh-CN>
        ///     <en>Announcement creation time; legacy rows may be null.</en>
        ///   </l>
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>公告标题，展示层输出前必须编码。</zh-CN>
        ///     <en>Announcement title; presentation code must encode it before output.</en>
        ///   </l>
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>更多链接地址；站内/受信任导航策略由调用层复核。</zh-CN>
        ///     <en>More-link URL; callers re-check it with the in-site or trusted navigation policy.</en>
        ///   </l>
        /// </summary>
        public string MoreLink { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>历史移动端更多链接地址，当前仅作为兼容字段保留。</zh-CN>
        ///     <en>Legacy mobile more-link URL retained only as a compatibility field.</en>
        ///   </l>
        /// </summary>
        public string MobileMoreLink { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>公告过期时间；过期过滤由查询或展示层决定。</zh-CN>
        ///     <en>Announcement expiry time; query or presentation code decides expiry filtering.</en>
        ///   </l>
        /// </summary>
        public DateTime? ExpireDate { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>公告正文描述，展示层输出前必须编码。</zh-CN>
        ///     <en>Announcement body description; presentation code must encode it before output.</en>
        ///   </l>
        /// </summary>
        public string Description { get; set; }

        #endregion
    }
}
