using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>旧链接模块条目的跨层契约。</zh-CN>
    ///     <en>Cross-layer contract for legacy link module items.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该接口用于链接编辑页、链接展示模块和 Data1 实体之间传递链接条目。URL 是否允许外链、
    ///       是否需要站内约束以及展示编码均由调用页面和导航策略处理。
    ///     </zh-CN>
    ///     <en>
    ///       This interface passes link items between link edit pages, link display modules, and Data1 entities. Whether a URL
    ///       may point outside the site, whether in-site restrictions apply, and how values are encoded are handled by calling pages and navigation policy.
    ///     </en>
    ///   </lang>
    /// </remarks>
    public interface ILinkItem
    {
        /// <summary>
        ///   <l zh-CN="链接条目的旧数据库主键。" en="Legacy database primary key of the link item." />
        /// </summary>
        int ItemId { get; set; }

        /// <summary>
        ///   <l zh-CN="拥有该链接条目的模块实例标识。" en="Module instance identifier that owns this link item." />
        /// </summary>
        int ModuleId { get; set; }

        /// <summary>
        ///   <l zh-CN="创建人显示名或历史用户名快照；不作为授权依据。" en="Creator display name or historical user-name snapshot; not used as an authorization source." />
        /// </summary>
        string CreatedByUser { get; set; }

        /// <summary>
        ///   <l zh-CN="链接条目创建时间；历史数据可能为空。" en="Creation time of the link item; legacy rows may be empty." />
        /// </summary>
        DateTime? CreatedDate { get; set; }

        /// <summary>
        ///   <l zh-CN="链接标题；展示层输出前必须编码。" en="Link title; the presentation layer must encode it before output." />
        /// </summary>
        string Title { get; set; }

        /// <summary>
        ///   <l zh-CN="桌面端链接地址；调用方负责验证和导航策略。" en="Desktop link URL; callers own validation and navigation policy." />
        /// </summary>
        string Url { get; set; }

        /// <summary>
        ///   <l zh-CN="历史移动端链接地址，当前作为兼容字段保留。" en="Legacy mobile link URL, currently retained as a compatibility field." />
        /// </summary>
        string MobileUrl { get; set; }

        /// <summary>
        ///   <l zh-CN="模块内显示顺序；空值由列表查询或展示层按旧规则解释。" en="Display order inside the module; null values are interpreted by list queries or presentation code according to legacy rules." />
        /// </summary>
        int? ViewOrder { get; set; }

        /// <summary>
        ///   <l zh-CN="链接说明文本；展示层输出前必须编码。" en="Link description text; the presentation layer must encode it before output." />
        /// </summary>
        string Description { get; set; }
    }
}
