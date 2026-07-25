using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>公告模块条目的跨层只读/可写契约。</zh-CN>
    ///     <en>Cross-layer readable/writable contract for an announcement module item.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该接口用于旧内容模块数据访问和 Web Forms 展示控件之间传递公告字段。实现只表达字段语义；
    ///       模块归属、链接安全、过期过滤和 HTML 编码由调用层各自负责。
    ///     </zh-CN>
    ///     <en>
    ///       This interface passes announcement fields between legacy content-module data access and Web Forms
    ///       display controls. Implementations express field semantics only; module ownership, link safety,
    ///       expiry filtering, and HTML encoding are owned by callers.
    ///     </en>
    ///   </lang>
    /// </remarks>
    public interface IAnnouncementItem
    {
        /// <summary>
        ///   <l zh-CN="公告条目的数据库主键。" en="Database primary key for the announcement item." />
        /// </summary>
        int ItemId { get; set; }

        /// <summary>
        ///   <l zh-CN="拥有该公告条目的模块实例标识。" en="Module instance identifier that owns this announcement item." />
        /// </summary>
        int ModuleId { get; set; }

        /// <summary>
        ///   <l zh-CN="创建人显示名称；来自旧内容模块保存流程，不作为授权依据。" en="Display name of the creator from the legacy content save flow; not an authorization source." />
        /// </summary>
        string CreatedByUser { get; set; }

        /// <summary>
        ///   <l zh-CN="公告创建时间；旧数据可能为空。" en="Announcement creation time; legacy rows may be null." />
        /// </summary>
        DateTime? CreatedDate { get; set; }

        /// <summary>
        ///   <l zh-CN="公告标题，展示层输出前必须编码。" en="Announcement title; presentation code must encode it before output." />
        /// </summary>
        string Title { get; set; }

        /// <summary>
        ///   <l zh-CN="更多链接地址；站内/受信任导航策略由调用层复核。" en="More-link URL; callers re-check it with the in-site or trusted navigation policy." />
        /// </summary>
        string MoreLink { get; set; }

        /// <summary>
        ///   <l zh-CN="历史移动端更多链接地址，当前仅作为兼容字段保留。" en="Legacy mobile more-link URL retained only as a compatibility field." />
        /// </summary>
        string MobileMoreLink { get; set; }

        /// <summary>
        ///   <l zh-CN="公告过期时间；过期过滤由查询或展示层决定。" en="Announcement expiry time; query or presentation code decides expiry filtering." />
        /// </summary>
        DateTime? ExpireDate { get; set; }

        /// <summary>
        ///   <l zh-CN="公告正文描述，展示层输出前必须编码。" en="Announcement body description; presentation code must encode it before output." />
        /// </summary>
        string Description { get; set; }
    }
}
