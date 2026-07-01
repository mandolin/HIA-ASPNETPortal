using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    public class AnnouncementsDb : IAnnouncementsDb
    {
        private readonly PortalDbContext _context;

        public AnnouncementsDb(PortalDbContext context)
        {
            _context = context;
        }

        #region IAnnouncementsDb Members

        /// <summary>
        /// 获取指定模块ID下的所有有效公告。
        /// </summary>
        /// <param name="moduleId">模块标识符。</param>
        /// <returns>有效公告的集合。</returns>
        public IEnumerable<IAnnouncementItem> GetAnnouncements(int moduleId)
        {
            // 使用 LINQ 查询获取当前时间前未过期的公告
            return _context.Announcements
                .Where(i => i.ModuleId == moduleId)
                .Where(i => i.ExpireDate > DateTime.Now)
                .ToList<IAnnouncementItem>();
        }

        /// <summary>
        /// 获取单个公告。
        /// </summary>
        /// <param name="itemId">公告标识符。</param>
        /// <returns>指定ID的公告对象。</returns>
        public IAnnouncementItem GetSingleAnnouncement(int itemId)
        {
            // 使用 Single 方法获取指定ID的公告
            return _context.Announcements.Single(i => i.ItemId == itemId);
        }

        /// <summary>
        /// 删除指定ID的公告。
        /// </summary>
        /// <param name="itemId">公告标识符。</param>
        public void DeleteAnnouncement(int itemId)
        {
            // 获取指定ID的公告对象
            var item = _context.Announcements.Single(i => i.ItemId == itemId);
            // 移除公告对象
            _context.Announcements.Remove(item);
            // 提交更改
            _context.SaveChanges();
        }

        /// <summary>
        /// 添加一条新公告。
        /// </summary>
        /// <param name="moduleId">模块标识符。</param>
        /// <param name="userName">用户名。</param>
        /// <param name="title">标题。</param>
        /// <param name="expireDate">过期日期。</param>
        /// <param name="description">描述。</param>
        /// <param name="moreLink">更多链接。</param>
        /// <param name="mobileMoreLink">移动设备上的更多链接。</param>
        /// <returns>新公告的ID。</returns>
        public int AddAnnouncement(int moduleId, string userName, string title, DateTime expireDate,
                                  string description, string moreLink, string mobileMoreLink)
        {
            // 如果用户名为空，则设置为 'unknown'
            userName = string.IsNullOrEmpty(userName) ? "unknown" : userName;

            // 创建新的公告对象
            var item = new AnnouncementItem
            {
                ModuleId = moduleId,
                CreatedByUser = userName,
                CreatedDate = DateTime.Now,
                Description = description,
                ExpireDate = expireDate,
                MoreLink = moreLink,
                MobileMoreLink = mobileMoreLink,
                Title = title
            };

            // 将新公告添加到上下文
            _context.Announcements.Add(item);
            // 提交更改
            _context.SaveChanges();

            // 返回新公告的ID
            return item.ItemId;
        }

        /// <summary>
        /// 更新指定ID的公告。
        /// </summary>
        /// <param name="itemId">公告标识符。</param>
        /// <param name="userName">用户名。</param>
        /// <param name="title">标题。</param>
        /// <param name="expireDate">过期日期。</param>
        /// <param name="description">描述。</param>
        /// <param name="moreLink">更多链接。</param>
        /// <param name="mobileMoreLink">移动设备上的更多链接。</param>
        public void UpdateAnnouncement(int itemId, string userName, string title, DateTime expireDate,
                                      string description, string moreLink, string mobileMoreLink)
        {
            // 如果用户名为空，则设置为 'unknown'
            userName = string.IsNullOrEmpty(userName) ? "unknown" : userName;

            // 获取指定ID的公告对象
            var item = _context.Announcements.Single(i => i.ItemId == itemId);

            // 更新公告的各项属性
            item.ModuleId = item.ModuleId; // 保持不变
            item.CreatedByUser = userName;
            item.CreatedDate = item.CreatedDate; // 保持不变
            item.Description = description;
            item.ExpireDate = expireDate;
            item.MoreLink = moreLink;
            item.MobileMoreLink = mobileMoreLink;
            item.Title = title;

            // 提交更改
            _context.SaveChanges();
        }

        #endregion
    }
}