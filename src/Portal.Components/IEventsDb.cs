using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>事件内容模块的数据访问契约。</zh-CN>
    ///   <en>Data-access contract for event content modules.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该契约沿用旧事件模块的存储语义：过期时间用于列表过滤，地点/时间说明作为展示文本保存。调用方负责模块编辑权限、条目归属、日期解析和低敏校验提示。</zh-CN>
    ///   <en>This contract keeps the legacy events module storage semantics: expiration time is used for list filtering, while the where/when value is stored as display text. Callers own module-edit authorization, item ownership checks, date parsing, and low-sensitivity validation messages.</en>
    /// </lang>
    /// </remarks>
    public interface IEventsDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定模块当前未过期的事件列表。</zh-CN>
        ///   <en>Reads currently non-expired events for the specified module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>事件模块实例标识。</zh-CN>
        ///   <en>The event module instance identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>未过期事件集合；展示层负责文本编码和日期显示格式。</zh-CN>
        ///   <en>The non-expired event collection; the presentation layer owns text encoding and date display formatting.</en>
        /// </l>
        /// </returns>
        IEnumerable<IEventItem> GetEvents(int moduleId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>按事件标识读取单条事件。</zh-CN>
        ///   <en>Reads a single event by identifier.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>事件条目标识。</zh-CN>
        ///   <en>Event item identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配事件；不存在时返回 <c>null</c>，调用方随后必须完成模块归属校验。</zh-CN>
        ///   <en>The matching event, or <c>null</c> when it does not exist; callers must then complete module ownership validation.</en>
        /// </l>
        /// </returns>
        IEventItem GetSingleEvent(int itemId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除一条已通过外层归属校验的事件。</zh-CN>
        ///   <en>Deletes an event whose ownership has already been verified by the outer layer.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>要删除的事件条目标识。</zh-CN>
        ///   <en>The event item identifier to delete.</en>
        /// </l>
        /// </param>
        void DeleteEvent(int itemId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>在指定模块下新增事件。</zh-CN>
        ///   <en>Creates an event under the specified module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>所属事件模块实例标识。</zh-CN>
        ///   <en>The owning event module instance identifier.</en>
        /// </l>
        /// </param>
        /// <param name="userName">
        /// <l>
        ///   <zh-CN>服务器端确认的操作者名称；用于历史创建人字段。</zh-CN>
        ///   <en>The server-confirmed operator name used for the legacy created-by field.</en>
        /// </l>
        /// </param>
        /// <param name="title">
        /// <l>
        ///   <zh-CN>事件标题。</zh-CN>
        ///   <en>The event title.</en>
        /// </l>
        /// </param>
        /// <param name="expireDate">
        /// <l>
        ///   <zh-CN>事件过期时间；用于旧列表查询的有效性过滤。</zh-CN>
        ///   <en>The event expiration time used by legacy list queries for active filtering.</en>
        /// </l>
        /// </param>
        /// <param name="description">
        /// <l>
        ///   <zh-CN>事件描述文本；数据层不执行 HTML 净化。</zh-CN>
        ///   <en>The event description text; the data layer does not perform HTML sanitization.</en>
        /// </l>
        /// </param>
        /// <param name="wherewhen">
        /// <l>
        ///   <zh-CN>旧模块中的地点/时间说明文本。</zh-CN>
        ///   <en>The legacy module where/when display text.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>新建事件的数据库标识。</zh-CN>
        ///   <en>The database identifier of the newly created event.</en>
        /// </l>
        /// </returns>
        int AddEvent(int moduleId, string userName, string title, DateTime expireDate, string description,
                     string wherewhen);

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新一条已通过外层归属校验的事件。</zh-CN>
        ///   <en>Updates an event whose ownership has already been verified by the outer layer.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>要更新的事件条目标识。</zh-CN>
        ///   <en>The event item identifier to update.</en>
        /// </l>
        /// </param>
        /// <param name="userName">
        /// <l>
        ///   <zh-CN>服务器端确认的操作者名称；用于历史更新元数据。</zh-CN>
        ///   <en>The server-confirmed operator name used for legacy update metadata.</en>
        /// </l>
        /// </param>
        /// <param name="title">
        /// <l>
        ///   <zh-CN>事件标题。</zh-CN>
        ///   <en>The event title.</en>
        /// </l>
        /// </param>
        /// <param name="expireDate">
        /// <l>
        ///   <zh-CN>事件过期时间。</zh-CN>
        ///   <en>The event expiration time.</en>
        /// </l>
        /// </param>
        /// <param name="description">
        /// <l>
        ///   <zh-CN>事件描述文本。</zh-CN>
        ///   <en>The event description text.</en>
        /// </l>
        /// </param>
        /// <param name="wherewhen">
        /// <l>
        ///   <zh-CN>旧模块中的地点/时间说明文本。</zh-CN>
        ///   <en>The legacy module where/when display text.</en>
        /// </l>
        /// </param>
        void UpdateEvent(int itemId, string userName, string title, DateTime expireDate,
                         string description, string wherewhen);
    }
}
