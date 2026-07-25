using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>通过 EF 上下文读写旧事件模块数据。</zh-CN>
    ///   <en>Reads and writes legacy event-module data through the EF context.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本实现只处理事件表持久化和按模块过滤；事件文本编码、日期输入兼容、编辑权限和条目归属由调用页与模块控件承担。</zh-CN>
    ///   <en>This implementation only handles event persistence and module filtering; event-text encoding, date-input compatibility, edit permission, and item ownership are handled by caller pages and module controls.</en>
    /// </lang>
    /// </remarks>
    public class EventsDb : IEventsDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>门户业务 EF 上下文，封装旧内容模块表映射。</zh-CN>
        ///   <en>Portal business EF context that wraps legacy content-module table mappings.</en>
        /// </lang>
        /// </summary>
        private readonly PortalDbContext _context;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化事件数据访问对象。</zh-CN>
        ///   <en>Initializes the event data-access object.</en>
        /// </lang>
        /// </summary>
        /// <param name="context"><l><zh-CN>由 Unity 注入的门户 EF 上下文。</zh-CN><en>Portal EF context injected by Unity.</en></l></param>
        public EventsDb(PortalDbContext context)
        {
            _context = context;
        }

        #region IEventsDb Members

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取指定模块下尚未过期的事件列表。</zh-CN>
        ///   <en>Gets events that have not expired for the specified module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId"><l><zh-CN>事件模块实例 ID。</zh-CN><en>Event module instance ID.</en></l></param>
        /// <returns><l><zh-CN>当前服务器时间之后仍有效的事件集合。</zh-CN><en>Event collection whose expiration time is later than the current server time.</en></l></returns>
        public IEnumerable<IEventItem> GetEvents(int moduleId)
        {
            // <lang>
            //   <zh-CN>沿用旧门户语义，事件过期判断使用服务器本地时间；时区统一治理由更高层运行环境负责。</zh-CN>
            //   <en>Keep the legacy portal semantics where event expiration uses server local time; timezone unification is handled by the higher-level runtime environment.</en>
            // </lang>
            return _context.Events.Where(i => i.ModuleId == moduleId && i.ExpireDate > DateTime.Now).ToList<IEventItem>();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取单个事件；用户指定的不存在标识返回 <c>null</c>，由调用页决定其授权失败响应。</zh-CN>
        ///   <en>Gets one event. A user-supplied missing identifier returns <c>null</c> so the caller can select its authorization-failure response.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>事件标识符。</zh-CN>
        ///   <en>Event identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>指定事件；不存在时为 <c>null</c>。</zh-CN>
        ///   <en>The requested event, or <c>null</c> when it does not exist.</en>
        /// </l>
        /// </returns>
        public IEventItem GetSingleEvent(int itemId)
        {
            // <lang>
            //   <zh-CN>编辑入口的 ItemId 来自请求，未命中返回空值，让页面层统一输出低敏提示或拒绝访问响应。</zh-CN>
            //   <en>The editor item identifier comes from a request; misses return null so the page layer can emit a low-sensitivity message or access-denied response.</en>
            // </lang>
            return _context.Events.SingleOrDefault(i => i.ItemId == itemId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除指定事件记录。</zh-CN>
        ///   <en>Deletes the specified event record.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该方法使用 <c>Single</c> 保留旧代码的严格语义；调用页应先确认条目存在且属于当前模块。</zh-CN>
        ///   <en>This method keeps the strict legacy <c>Single</c> semantics; caller pages should first confirm that the item exists and belongs to the current module.</en>
        /// </lang>
        /// </remarks>
        /// <param name="itemId"><l><zh-CN>事件标识符。</zh-CN><en>Event identifier.</en></l></param>
        public void DeleteEvent(int itemId)
        {
            var item = _context.Events.Single(i => i.ItemId == itemId);

            // <lang>
            //   <zh-CN>删除动作直接提交到旧表；审计、权限和站内回跳由编辑页完成。</zh-CN>
            //   <en>The delete action is committed directly to the legacy table; audit, permission, and safe return navigation are completed by the editor page.</en>
            // </lang>
            _context.Events.Remove(item);
            _context.SaveChanges();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>新增一个事件记录。</zh-CN>
        ///   <en>Adds a new event record.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>事件标题、地点时间描述和正文按旧字段保存；日期解析与低敏错误提示由编辑页完成，展示编码由模块控件完成。</zh-CN>
        ///   <en>Event title, where/when text, and description are stored in legacy fields; date parsing and low-sensitivity error messages are handled by the editor page, and display encoding is handled by the module control.</en>
        /// </lang>
        /// </remarks>
        /// <param name="moduleId"><l><zh-CN>事件模块实例 ID。</zh-CN><en>Event module instance ID.</en></l></param>
        /// <param name="userName"><l><zh-CN>用于历史显示的创建人名称；空值会降级为旧占位值。</zh-CN><en>Creator name used for historical display; a blank value falls back to the legacy placeholder.</en></l></param>
        /// <param name="title"><l><zh-CN>事件标题。</zh-CN><en>Event title.</en></l></param>
        /// <param name="expireDate"><l><zh-CN>事件过期时间，按旧门户本地时间语义保存。</zh-CN><en>Event expiration time, stored with the legacy portal's local-time semantics.</en></l></param>
        /// <param name="description"><l><zh-CN>事件描述。</zh-CN><en>Event description.</en></l></param>
        /// <param name="wherewhen"><l><zh-CN>旧字段中的时间地点描述文本。</zh-CN><en>Where/when description text in the legacy field.</en></l></param>
        /// <returns><l><zh-CN>新增事件的数据库标识符。</zh-CN><en>Database identifier of the new event.</en></l></returns>
        public int AddEvent(int moduleId, string userName, string title, DateTime expireDate,
                            string description, string wherewhen)
        {
            // <lang>
            //   <zh-CN>旧内容表只有显示用创建人字段；缺失认证名称时使用占位值，不把它作为权限依据。</zh-CN>
            //   <en>The legacy content table only has a display creator field; when the authenticated name is missing, use a placeholder and do not treat it as an authorization source.</en>
            // </lang>
            userName = string.IsNullOrEmpty(userName) ? "unknown" : userName;

            var item = new EventItem
            {
                ModuleId = moduleId,
                CreatedByUser = userName,
                CreatedDate = DateTime.Now,
                Description = description,
                ExpireDate = expireDate,
                Title = title,
                WhereWhen = wherewhen
            };

            _context.Events.Add(item);
            _context.SaveChanges();

            return item.ItemId;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新指定事件记录的可编辑字段。</zh-CN>
        ///   <en>Updates editable fields of the specified event record.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>更新不会改变事件所属模块和创建时间；调用页负责确认当前用户可编辑该条目。</zh-CN>
        ///   <en>Updating does not change the event's owning module or creation time; the caller page is responsible for confirming that the current user may edit the item.</en>
        /// </lang>
        /// </remarks>
        /// <param name="itemId"><l><zh-CN>事件标识符。</zh-CN><en>Event identifier.</en></l></param>
        /// <param name="userName"><l><zh-CN>用于历史显示的最后编辑人名称。</zh-CN><en>Last editor name used for historical display.</en></l></param>
        /// <param name="title"><l><zh-CN>事件标题。</zh-CN><en>Event title.</en></l></param>
        /// <param name="expireDate"><l><zh-CN>事件过期时间。</zh-CN><en>Event expiration time.</en></l></param>
        /// <param name="description"><l><zh-CN>事件描述。</zh-CN><en>Event description.</en></l></param>
        /// <param name="wherewhen"><l><zh-CN>旧字段中的时间地点描述文本。</zh-CN><en>Where/when description text in the legacy field.</en></l></param>
        public void UpdateEvent(int itemId, string userName, string title, DateTime expireDate,
                                string description, string wherewhen)
        {
            // <lang>
            //   <zh-CN>保持和新增路径一致的显示名占位策略。</zh-CN>
            //   <en>Keep the same display-name placeholder strategy as the add path.</en>
            // </lang>
            userName = string.IsNullOrEmpty(userName) ? "unknown" : userName;

            var item = _context.Events.Single(i => i.ItemId == itemId);

            // <lang>
            //   <zh-CN>旧表没有独立“最后编辑人”字段，当前实现沿用 CreatedByUser 保存最近一次编辑显示名。</zh-CN>
            //   <en>The legacy table has no separate last-editor field, so the current implementation reuses CreatedByUser for the latest editor display name.</en>
            // </lang>
            item.CreatedByUser = userName;
            item.Title = title;
            item.ExpireDate = expireDate;
            item.Description = description;
            item.WhereWhen = wherewhen;

            _context.SaveChanges();
        }

        #endregion
    }
}
