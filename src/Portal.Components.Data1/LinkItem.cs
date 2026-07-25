using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>链接模块条目的 Entity Framework 投影。</zh-CN>
    ///     <en>Entity Framework projection for a link module item.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该类型映射旧表 <c>Portal_Links</c>。它只表达数据库中的链接条目，不自行判断 URL
    ///       是否站内、是否允许跳转或是否需要回退；这些安全策略由编辑页、数据访问层和展示控件共同执行。
    ///     </zh-CN>
    ///     <en>
    ///       This type maps the legacy <c>Portal_Links</c> table. It only represents the persisted link item
    ///       and does not decide whether a URL is in-site, allowed, or should fall back; those safety policies
    ///       are enforced by edit pages, data access, and display controls.
    ///     </en>
    ///   </lang>
    /// </remarks>
    [Table("Portal_Links")]
    public class LinkItem : ILinkItem
    {
        #region ILinkItem Members

        /// <summary>
        ///   <l zh-CN="链接条目的数据库主键。" en="Database primary key for the link item." />
        /// </summary>
        [Key]
        public int ItemId { get; set; }

        /// <summary>
        ///   <l zh-CN="拥有该链接条目的模块实例标识。" en="Module instance identifier that owns this link item." />
        /// </summary>
        public int ModuleId { get; set; }

        /// <summary>
        ///   <l zh-CN="创建人显示名称；不是授权依据。" en="Display name of the creator; this is not an authorization source." />
        /// </summary>
        public string CreatedByUser { get; set; }

        /// <summary>
        ///   <l zh-CN="链接条目创建时间；旧数据可能为空。" en="Creation time for the link item; legacy rows may be null." />
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        ///   <l zh-CN="链接标题，展示层输出前必须编码。" en="Link title; presentation code must encode it before output." />
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        ///   <l zh-CN="桌面端链接地址；调用层仍需执行站内或受信任导航策略。" en="Desktop link URL; callers must still apply in-site or trusted navigation policy." />
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        ///   <l zh-CN="历史移动端链接地址，当前仅作为兼容字段保留。" en="Legacy mobile link URL retained only as a compatibility field." />
        /// </summary>
        public string MobileUrl { get; set; }

        /// <summary>
        ///   <l zh-CN="链接显示顺序；为空时由查询或展示层决定默认顺序。" en="Link display order; query or presentation code chooses the fallback order when null." />
        /// </summary>
        public int? ViewOrder { get; set; }

        /// <summary>
        ///   <l zh-CN="链接描述文本，展示层输出前必须编码。" en="Link description text; presentation code must encode it before output." />
        /// </summary>
        public string Description { get; set; }

        #endregion
    }
}
