using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    public class EventsDb : IEventsDb
    {
        private readonly PortalDbContext _context;

        public EventsDb(PortalDbContext context)
        {
            _context = context;
        }

        #region IEventsDb Members

        /// <summary>
        /// 获取指定模块ID下的所有未过期事件。
        /// </summary>
        /// <param name="moduleId">模块标识符。</param>
        /// <returns>符合条件的事件集合。</returns>
        public IEnumerable<IEventItem> GetEvents(int moduleId)
        {
            // 使用 LINQ 查询获取指定模块ID下的所有未过期事件
            return _context.Events.Where(i => i.ModuleId == moduleId && i.ExpireDate > DateTime.Now).ToList<IEventItem>();
        }

        /// <summary>
        /// 获取单个事件。
        /// </summary>
        /// <param name="itemId">事件标识符。</param>
        /// <returns>指定ID的事件对象。</returns>
        public IEventItem GetSingleEvent(int itemId)
        {
            // 使用 Single 方法获取指定ID的事件
            return _context.Events.Single(i => i.ItemId == itemId);
        }

        /// <summary>
        /// 删除指定ID的事件。
        /// </summary>
        /// <param name="itemId">事件标识符。</param>
        public void DeleteEvent(int itemId)
        {
            // 获取指定ID的事件对象
            var item = _context.Events.Single(i => i.ItemId == itemId);
            // 从上下文中移除事件对象
            _context.Events.Remove(item);
            // 提交更改到数据库
            _context.SaveChanges();
        }

        /// <summary>
        /// 添加一个新的事件。
        /// </summary>
        /// <param name="moduleId">模块标识符。</param>
        /// <param name="userName">用户名。</param>
        /// <param name="title">事件标题。</param>
        /// <param name="expireDate">事件到期日期。</param>
        /// <param name="description">事件描述。</param>
        /// <param name="wherewhen">事件的时间地点。</param>
        /// <returns>新事件的ID。</returns>
        public int AddEvent(int moduleId, string userName, string title, DateTime expireDate,
                            string description, string wherewhen)
        {
            // 如果用户名为空，则设置为 'unknown'
            userName = string.IsNullOrEmpty(userName) ? "unknown" : userName;

            // 创建新的事件对象
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

            // 将新事件添加到上下文
            _context.Events.Add(item);
            // 提交更改到数据库
            _context.SaveChanges();

            // 返回新事件的ID
            return item.ItemId;
        }

        /// <summary>
        /// 更新指定ID的事件信息。
        /// </summary>
        /// <param name="itemId">事件标识符。</param>
        /// <param name="userName">用户名。</param>
        /// <param name="title">事件标题。</param>
        /// <param name="expireDate">事件到期日期。</param>
        /// <param name="description">事件描述。</param>
        /// <param name="wherewhen">事件的时间地点。</param>
        public void UpdateEvent(int itemId, string userName, string title, DateTime expireDate,
                                string description, string wherewhen)
        {
            // 如果用户名为空，则设置为 'unknown'
            userName = string.IsNullOrEmpty(userName) ? "unknown" : userName;

            // 获取指定ID的事件对象
            var item = _context.Events.Single(i => i.ItemId == itemId);

            // 更新事件的属性
            item.CreatedByUser = userName;
            item.Title = title;
            item.ExpireDate = expireDate;
            item.Description = description;
            item.WhereWhen = wherewhen;

            // 提交更改到数据库
            _context.SaveChanges();
        }

        #endregion
    }
}