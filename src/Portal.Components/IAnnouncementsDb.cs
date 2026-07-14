using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：公告模块的数据访问契约。
    ///
    /// English: Data-access contract for the announcements module.
    /// </summary>
    public interface IAnnouncementsDb
    {
        /// <summary>
        /// 中文：读取模块的有效公告。English: Reads active announcements for a module.
        /// </summary>
        IEnumerable<IAnnouncementItem> GetAnnouncements(int moduleId);

        /// <summary>
        /// 中文：按标识读取公告；不存在时返回 <c>null</c>。English: Reads an announcement by identifier, returning <c>null</c> when it does not exist.
        /// </summary>
        IAnnouncementItem GetSingleAnnouncement(int itemId);

        /// <summary>
        /// 中文：删除已验证归属的公告。English: Deletes an announcement whose ownership has already been verified.
        /// </summary>
        void DeleteAnnouncement(int itemId);

        /// <summary>
        /// 中文：新增公告。English: Creates an announcement.
        /// </summary>
        int AddAnnouncement(int moduleId, string userName, string title, DateTime expireDate,
                            string description, string moreLink, string mobileMoreLink);

        /// <summary>
        /// 中文：更新已验证归属的公告。English: Updates an announcement whose ownership has already been verified.
        /// </summary>
        void UpdateAnnouncement(int itemId, string userName, string title, DateTime expireDate,
                                string description, string moreLink, string mobileMoreLink);
    }
}
