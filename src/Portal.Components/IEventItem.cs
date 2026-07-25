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
        ///   <l zh-CN="事件条目的旧数据库主键。" en="Legacy database primary key of the event item." />
        /// </summary>
        int ItemId { get; set; }

        /// <summary>
        ///   <l zh-CN="拥有该事件条目的模块实例标识。" en="Module instance identifier that owns this event item." />
        /// </summary>
        int ModuleId { get; set; }

        /// <summary>
        ///   <l zh-CN="事件标题；展示层负责输出编码。" en="Event title; the presentation layer is responsible for output encoding." />
        /// </summary>
        string Title { get; set; }

        /// <summary>
        ///   <l zh-CN="创建人显示名或历史用户名快照；不作为权限依据。" en="Creator display name or historical user-name snapshot; not used as an authorization source." />
        /// </summary>
        string CreatedByUser { get; set; }

        /// <summary>
        ///   <l zh-CN="旧模块合并保存的地点与时间说明文本。" en="Legacy free-form text combining event location and time description." />
        /// </summary>
        string WhereWhen { get; set; }

        /// <summary>
        ///   <l zh-CN="事件创建时间；旧数据可能为空。" en="Event creation time; legacy rows may be empty." />
        /// </summary>
        DateTime? CreatedDate { get; set; }

        /// <summary>
        ///   <l zh-CN="事件过期时间；列表过滤规则由调用方决定。" en="Event expiration time; list filtering rules are decided by callers." />
        /// </summary>
        DateTime? ExpireDate { get; set; }

        /// <summary>
        ///   <l zh-CN="事件正文描述，可能包含旧模块允许的富文本内容。" en="Event body description, which may contain rich text accepted by the legacy module." />
        /// </summary>
        string Description { get; set; }
    }
}
