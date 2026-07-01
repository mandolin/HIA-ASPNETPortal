using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    public class LinksDb : ILinksDb
    {
        private readonly PortalDbContext _context;

        public LinksDb(PortalDbContext context)
        {
            _context = context;
        }

        #region ILinksDb Members

        /// <summary>
        /// 获取指定模块ID的所有链接。
        /// </summary>
        /// <param name="moduleId">模块标识符。</param>
        /// <returns>链接集合。</returns>
        public IEnumerable<ILinkItem> GetLinks(int moduleId)
        {
            // 使用 LINQ 查询获取指定模块ID的所有链接
            return _context.Links.Where(i => i.ModuleId == moduleId).ToList<ILinkItem>();
        }

        /// <summary>
        /// 获取单个链接。
        /// </summary>
        /// <param name="itemId">链接标识符。</param>
        /// <returns>单个链接对象。</returns>
        public ILinkItem GetSingleLink(int itemId)
        {
            // 使用 Single 方法获取指定ID的链接
            return _context.Links.Single(i => i.ItemId == itemId);
        }

        /// <summary>
        /// 删除指定ID的链接。
        /// </summary>
        /// <param name="itemId">链接标识符。</param>
        public void DeleteLink(int itemId)
        {
            // 使用 Single 方法获取指定ID的链接
            var item = _context.Links.Single(i => i.ItemId == itemId);

            // 从上下文中移除链接对象
            _context.Links.Remove(item);
            // 提交更改到数据库
            _context.SaveChanges();
        }

        /// <summary>
        /// 添加新的链接。
        /// </summary>
        /// <param name="moduleId">模块标识符。</param>
        /// <param name="userName">用户名。</param>
        /// <param name="title">链接标题。</param>
        /// <param name="url">链接URL。</param>
        /// <param name="mobileUrl">移动设备上的链接URL。</param>
        /// <param name="viewOrder">显示顺序。</param>
        /// <param name="description">链接描述。</param>
        /// <returns>新链接的ID。</returns>
        public int AddLink(int moduleId, string userName, string title, string url, string mobileUrl,
                           int viewOrder, string description)
        {
            // 如果用户名为空，则设置为 'unknown'
            userName = userName ?? "unknown";

            // 创建新的链接对象
            var item = new LinkItem
            {
                ModuleId = moduleId,
                CreatedByUser = userName,
                CreatedDate = DateTime.Now,
                Title = title,
                Url = url,
                MobileUrl = mobileUrl,
                ViewOrder = viewOrder,
                Description = description
            };

            // 将新链接添加到上下文
            _context.Links.Add(item);
            // 提交更改到数据库
            _context.SaveChanges();

            // 返回新链接的ID
            return item.ItemId;
        }

        /// <summary>
        /// 更新指定ID的链接信息。
        /// </summary>
        /// <param name="itemId">链接标识符。</param>
        /// <param name="userName">用户名。</param>
        /// <param name="title">链接标题。</param>
        /// <param name="url">链接URL。</param>
        /// <param name="mobileUrl">移动设备上的链接URL。</param>
        /// <param name="viewOrder">显示顺序。</param>
        /// <param name="description">链接描述。</param>
        public void UpdateLink(int itemId, string userName, string title, string url, string mobileUrl,
                               int viewOrder, string description)
        {
            // 如果用户名为空，则设置为 'unknown'
            userName = userName ?? "unknown";

            // 使用 Single 方法获取指定ID的链接对象
            var item = _context.Links.Single(i => i.ItemId == itemId);

            // 更新链接的属性
            item.CreatedByUser = userName;
            item.Title = title;
            item.Url = url;
            item.MobileUrl = mobileUrl;
            item.ViewOrder = viewOrder;
            item.Description = description;

            // 提交更改到数据库
            _context.SaveChanges();
        }

        #endregion
    }
}