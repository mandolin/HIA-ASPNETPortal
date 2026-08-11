using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>旧讨论模块条目的跨层契约。</zh-CN>
    ///     <en>Cross-layer contract for legacy discussion module items.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该接口由旧讨论数据访问层和 Web Forms 展示/详情页共享。它只表达旧表投影形状，不负责权限判断、
    ///       HTML 编码或线程路径校验；这些安全与展示规则仍由调用模块、详情页和数据访问实现分别承担。
    ///     </zh-CN>
    ///     <en>
    ///       This interface is shared by the legacy discussion data access layer and Web Forms list/detail pages. It only
    ///       describes the legacy table projection shape and does not own authorization checks, HTML encoding, or thread-path
    ///       validation; those safety and display rules remain with the calling module, detail page, and data access implementation.
    ///     </en>
    ///   </lang>
    /// </remarks>
    public interface IDiscussionItem
    {
        /// <summary>
        ///   <l>
        ///     <zh-CN>讨论条目的旧数据库主键。</zh-CN>
        ///     <en>Legacy database primary key of the discussion item.</en>
        ///   </l>
        /// </summary>
        int ItemID { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>拥有该讨论条目的模块实例标识。</zh-CN>
        ///     <en>Module instance identifier that owns this discussion item.</en>
        ///   </l>
        /// </summary>
        int ModuleID { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>历史线程父路径字符串，用于查找父消息和生成讨论树。</zh-CN>
        ///     <en>Legacy parent-thread path string used to locate the parent message and build the discussion tree.</en>
        ///   </l>
        /// </summary>
        string Parent { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>讨论标题；展示层输出前必须按页面上下文编码。</zh-CN>
        ///     <en>Discussion title; the presentation layer must encode it according to page context before output.</en>
        ///   </l>
        /// </summary>
        string Title { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>讨论条目创建时间；历史数据可能为空。</zh-CN>
        ///     <en>Creation time of the discussion item; historical rows may be empty.</en>
        ///   </l>
        /// </summary>
        DateTime? CreatedDate { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>讨论正文；不能直接作为受信任 HTML 输出。</zh-CN>
        ///     <en>Discussion body; it must not be emitted directly as trusted HTML.</en>
        ///   </l>
        /// </summary>
        string Body { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>历史排序路径，用于按树形顺序显示讨论线程。</zh-CN>
        ///     <en>Legacy sort path used to display discussion threads in tree order.</en>
        ///   </l>
        /// </summary>
        string DisplayOrder { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>创建人显示名快照；它只是展示文本，不是授权依据。</zh-CN>
        ///     <en>Creator display-name snapshot; this is display text only and not an authorization source.</en>
        ///   </l>
        /// </summary>
        string CreatedByUser { get; set; }
    }
}
