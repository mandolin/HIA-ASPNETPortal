using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>旧事件模块条目的跨层契约。</zh-CN>
    ///     <en>Cross-layer contract for legacy event module items.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该接口用于事件编辑页、事件展示模块和 Data1 实体之间传递旧事件表投影。它保持旧字段语义，
    ///       不内置过期过滤、权限检查或富文本净化策略。
    ///     </zh-CN>
    ///     <en>
    ///       This interface passes legacy event table projections between event edit pages, event display modules, and Data1
    ///       entities. It keeps legacy field meanings and does not embed expiration filtering, authorization checks, or rich-text sanitization policy.
    ///     </en>
    ///   </lang>
    /// </remarks>
    public interface IEventItem
    {
        /// <summary>
        ///   <l>
        ///     <zh-CN>事件条目的旧数据库主键。</zh-CN>
        ///     <en>Legacy database primary key of the event item.</en>
        ///   </l>
        /// </summary>
        int ItemId { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>拥有该事件条目的模块实例标识。</zh-CN>
        ///     <en>Module instance identifier that owns this event item.</en>
        ///   </l>
        /// </summary>
        int ModuleId { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>事件标题；展示层负责输出编码。</zh-CN>
        ///     <en>Event title; the presentation layer is responsible for output encoding.</en>
        ///   </l>
        /// </summary>
        string Title { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>创建人显示名或历史用户名快照；不作为权限依据。</zh-CN>
        ///     <en>Creator display name or historical user-name snapshot; not used as an authorization source.</en>
        ///   </l>
        /// </summary>
        string CreatedByUser { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>旧模块合并保存的地点与时间说明文本。</zh-CN>
        ///     <en>Legacy free-form text combining event location and time description.</en>
        ///   </l>
        /// </summary>
        string WhereWhen { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>事件创建时间；旧数据可能为空。</zh-CN>
        ///     <en>Event creation time; legacy rows may be empty.</en>
        ///   </l>
        /// </summary>
        DateTime? CreatedDate { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>事件过期时间；列表过滤规则由调用方决定。</zh-CN>
        ///     <en>Event expiration time; list filtering rules are decided by callers.</en>
        ///   </l>
        /// </summary>
        DateTime? ExpireDate { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>事件正文描述，可能包含旧模块允许的富文本内容。</zh-CN>
        ///     <en>Event body description, which may contain rich text accepted by the legacy module.</en>
        ///   </l>
        /// </summary>
        string Description { get; set; }
    }
}
