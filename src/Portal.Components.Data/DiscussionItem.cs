using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>讨论模块条目的 Entity Framework 投影。</zh-CN>
    ///     <en>Entity Framework projection for a discussion module item.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该类型映射旧表 <c>Portal_Discussion</c>，同时实现 <see cref="IDiscussionItem"/> 供旧 Web Forms
    ///       讨论模块和数据访问契约共享。<c>Parent</c> 与 <c>DisplayOrder</c> 沿用历史线程路径字符串，
    ///       展示层仍需在输出标题、正文和创建人时执行 HTML 编码。
    ///     </zh-CN>
    ///     <en>
    ///       This type maps the legacy <c>Portal_Discussion</c> table and implements <see cref="IDiscussionItem"/>
    ///       so the Web Forms discussion module and data access contract can share one shape. <c>Parent</c> and
    ///       <c>DisplayOrder</c> keep the legacy thread-path strings, and the presentation layer remains responsible
    ///       for HTML-encoding titles, bodies, and author names.
    ///     </en>
    ///   </lang>
    /// </remarks>
    [Table("Portal_Discussion")]
    public class DiscussionItem : IDiscussionItem
    {
        #region IDiscussionItem Members

        /// <summary>
        ///   <l zh-CN="讨论条目的数据库主键。" en="Database primary key for the discussion item." />
        /// </summary>
        [Key]
        public int ItemID { get; set; }

        /// <summary>
        ///   <l zh-CN="拥有该讨论条目的模块实例标识。" en="Module instance identifier that owns this discussion item." />
        /// </summary>
        public int ModuleID { get; set; }

        /// <summary>
        ///   <l zh-CN="当前讨论节点的直接子回复数量。" en="Number of direct child replies below this discussion node." />
        /// </summary>
        public int ChildCount { get; set; } = 0;

        /// <summary>
        ///   <l zh-CN="讨论标题；展示层输出前必须编码。" en="Discussion title; the presentation layer must encode it before output." />
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        ///   <l zh-CN="历史线程父路径字符串，用于定位父级消息。" en="Legacy parent-thread path string used to locate the parent message." />
        /// </summary>
        public string Parent { get; set; } = string.Empty;

        /// <summary>
        ///   <l zh-CN="讨论条目创建时间；旧数据可能为空。" en="Creation time for the discussion item; legacy rows may be null." />
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        ///   <l zh-CN="讨论正文；展示层输出前必须编码或按受信任策略处理。" en="Discussion body; encode it before output or process it through a trusted policy." />
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        ///   <l zh-CN="历史排序路径，用于按讨论树顺序显示线程。" en="Legacy sort path used to display threads in discussion-tree order." />
        /// </summary>
        public string DisplayOrder { get; set; } = string.Empty;

        /// <summary>
        ///   <l zh-CN="创建人显示名称；不是授权依据。" en="Display name of the creator; this is not an authorization source." />
        /// </summary>
        public string CreatedByUser { get; set; } = string.Empty;

        /// <summary>
        ///   <lang>
        ///     <zh-CN>返回便于调试查看的讨论条目摘要。</zh-CN>
        ///     <en>Returns a compact discussion item summary for diagnostics.</en>
        ///   </lang>
        /// </summary>
        /// <returns>
        ///   <lang>
        ///     <zh-CN>包含主键、标题、创建人和创建时间的非安全摘要字符串。</zh-CN>
        ///     <en>A non-security summary string containing the key, title, creator, and creation time.</en>
        ///   </lang>
        /// </returns>
        /// <remarks>
        ///   <lang>
        ///     <zh-CN>该方法只用于调试或日志摘要，不应作为用户可见 HTML 直接输出。</zh-CN>
        ///     <en>This method is intended for diagnostics or log summaries and should not be written directly as user-visible HTML.</en>
        ///   </lang>
        /// </remarks>
        public override string ToString()
        {
            return $"[{ItemID}] {Title} - {CreatedByUser} ({CreatedDate:yyyy-MM-dd HH:mm})";
        }

        #endregion
    }
}
