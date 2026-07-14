using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：事件模块的数据访问契约。
    ///
    /// English: Data-access contract for the events module.
    /// </summary>
    public interface IEventsDb
    {
        /// <summary>
        /// 中文：读取模块的未过期事件。English: Reads non-expired events for a module.
        /// </summary>
        IEnumerable<IEventItem> GetEvents(int moduleId);

        /// <summary>
        /// 中文：按标识读取事件；不存在时返回 <c>null</c>。English: Reads an event by identifier, returning <c>null</c> when it does not exist.
        /// </summary>
        IEventItem GetSingleEvent(int itemId);

        /// <summary>
        /// 中文：删除已验证归属的事件。English: Deletes an event whose ownership has already been verified.
        /// </summary>
        void DeleteEvent(int itemId);

        /// <summary>
        /// 中文：新增事件。English: Creates an event.
        /// </summary>
        int AddEvent(int moduleId, string userName, string title, DateTime expireDate, string description,
                     string wherewhen);

        /// <summary>
        /// 中文：更新已验证归属的事件。English: Updates an event whose ownership has already been verified.
        /// </summary>
        void UpdateEvent(int itemId, string userName, string title, DateTime expireDate,
                         string description, string wherewhen);
    }
}
