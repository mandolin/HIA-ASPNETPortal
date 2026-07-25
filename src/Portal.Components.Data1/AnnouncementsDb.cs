using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>通过 EF 上下文读写旧公告模块数据。</zh-CN>
    ///   <en>Reads and writes legacy announcement-module data through the EF context.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本实现只处理公告表的持久化和按模块过滤；模块编辑权限、条目归属校验、链接策略和展示层 HTML 编码由调用页与模块控件承担。</zh-CN>
    ///   <en>This implementation only handles announcement persistence and module filtering; module-edit permission, item ownership checks, link policy, and display-time HTML encoding are handled by caller pages and module controls.</en>
    /// </lang>
    /// </remarks>
    public class AnnouncementsDb : IAnnouncementsDb
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
        ///   <zh-CN>初始化公告数据访问对象。</zh-CN>
        ///   <en>Initializes the announcement data-access object.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>由 Unity 注入的门户 EF 上下文。</zh-CN>
        ///   <en>Portal EF context injected by Unity.</en>
        /// </l>
        /// </param>
        public AnnouncementsDb(PortalDbContext context)
        {
            _context = context;
        }

        #region IAnnouncementsDb Members

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取指定模块下尚未过期的公告列表。</zh-CN>
        ///   <en>Gets announcements that have not expired for the specified module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>公告模块实例 ID。</zh-CN>
        ///   <en>Announcement module instance ID.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前服务器时间之后仍有效的公告集合。</zh-CN>
        ///   <en>Announcement collection whose expiration time is later than the current server time.</en>
        /// </l>
        /// </returns>
        public IEnumerable<IAnnouncementItem> GetAnnouncements(int moduleId)
        {
            // <lang>
            //   <zh-CN>沿用旧门户语义，公告过期判断使用服务器本地时间；时区统一治理由更高层运行环境负责。</zh-CN>
            //   <en>Keep the legacy portal semantics where announcement expiration uses server local time; timezone unification is handled by the higher-level runtime environment.</en>
            // </lang>
            return _context.Announcements
                .Where(i => i.ModuleId == moduleId)
                .Where(i => i.ExpireDate > DateTime.Now)
                .ToList<IAnnouncementItem>();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取单个公告；用户指定的不存在标识返回 <c>null</c>，由调用页决定其授权失败响应。</zh-CN>
        ///   <en>Gets one announcement. A user-supplied missing identifier returns <c>null</c> so the caller can select its authorization-failure response.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>公告标识符。</zh-CN>
        ///   <en>Announcement identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>指定公告；不存在时为 <c>null</c>。</zh-CN>
        ///   <en>The requested announcement, or <c>null</c> when it does not exist.</en>
        /// </l>
        /// </returns>
        public IAnnouncementItem GetSingleAnnouncement(int itemId)
        {
            // <lang>
            //   <zh-CN>编辑入口的 ItemId 来自请求，未命中返回空值，让页面层统一输出低敏提示或拒绝访问响应。</zh-CN>
            //   <en>The editor item identifier comes from a request; misses return null so the page layer can emit a low-sensitivity message or access-denied response.</en>
            // </lang>
            return _context.Announcements.SingleOrDefault(i => i.ItemId == itemId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除指定公告记录。</zh-CN>
        ///   <en>Deletes the specified announcement record.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该方法使用 <c>Single</c> 保留旧代码的严格语义；调用页应先确认条目存在且属于当前模块。</zh-CN>
        ///   <en>This method keeps the strict legacy <c>Single</c> semantics; caller pages should first confirm that the item exists and belongs to the current module.</en>
        /// </lang>
        /// </remarks>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>公告标识符。</zh-CN>
        ///   <en>Announcement identifier.</en>
        /// </l>
        /// </param>
        public void DeleteAnnouncement(int itemId)
        {
            var item = _context.Announcements.Single(i => i.ItemId == itemId);

            // <lang>
            //   <zh-CN>删除动作直接提交到旧表；审计、权限和站内回跳由编辑页完成。</zh-CN>
            //   <en>The delete action is committed directly to the legacy table; audit, permission, and safe return navigation are completed by the editor page.</en>
            // </lang>
            _context.Announcements.Remove(item);
            _context.SaveChanges();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>新增一条公告记录。</zh-CN>
        ///   <en>Adds a new announcement record.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>标题、描述和链接策略由编辑页做前置校验；展示层仍负责输出编码和链接可点击性判断。</zh-CN>
        ///   <en>Title, description, and link policy are validated by the editor page before this call; the display layer remains responsible for output encoding and link-clickability decisions.</en>
        /// </lang>
        /// </remarks>
        /// <param name="moduleId"><l><zh-CN>公告模块实例 ID。</zh-CN><en>Announcement module instance ID.</en></l></param>
        /// <param name="userName"><l><zh-CN>用于历史显示的创建人名称；空值会降级为旧占位值。</zh-CN><en>Creator name used for historical display; a blank value falls back to the legacy placeholder.</en></l></param>
        /// <param name="title"><l><zh-CN>公告标题。</zh-CN><en>Announcement title.</en></l></param>
        /// <param name="expireDate"><l><zh-CN>公告过期时间，按旧门户本地时间语义保存。</zh-CN><en>Announcement expiration time, stored with the legacy portal's local-time semantics.</en></l></param>
        /// <param name="description"><l><zh-CN>公告正文或摘要文本。</zh-CN><en>Announcement body or summary text.</en></l></param>
        /// <param name="moreLink"><l><zh-CN>桌面端更多链接。</zh-CN><en>Desktop More link.</en></l></param>
        /// <param name="mobileMoreLink"><l><zh-CN>历史移动端更多链接。</zh-CN><en>Legacy mobile More link.</en></l></param>
        /// <returns><l><zh-CN>新增公告的数据库标识符。</zh-CN><en>Database identifier of the new announcement.</en></l></returns>
        public int AddAnnouncement(int moduleId, string userName, string title, DateTime expireDate,
                                  string description, string moreLink, string mobileMoreLink)
        {
            // <lang>
            //   <zh-CN>旧内容表只有显示用创建人字段；缺失认证名称时使用占位值，不把它作为权限依据。</zh-CN>
            //   <en>The legacy content table only has a display creator field; when the authenticated name is missing, use a placeholder and do not treat it as an authorization source.</en>
            // </lang>
            userName = string.IsNullOrEmpty(userName) ? "unknown" : userName;

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

            _context.Announcements.Add(item);
            _context.SaveChanges();

            return item.ItemId;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新指定公告记录的可编辑字段。</zh-CN>
        ///   <en>Updates editable fields of the specified announcement record.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>更新不会改变公告所属模块和创建时间；调用页负责确认当前用户可编辑该条目。</zh-CN>
        ///   <en>Updating does not change the announcement's owning module or creation time; the caller page is responsible for confirming that the current user may edit the item.</en>
        /// </lang>
        /// </remarks>
        /// <param name="itemId"><l><zh-CN>公告标识符。</zh-CN><en>Announcement identifier.</en></l></param>
        /// <param name="userName"><l><zh-CN>用于历史显示的最后编辑人名称。</zh-CN><en>Last editor name used for historical display.</en></l></param>
        /// <param name="title"><l><zh-CN>公告标题。</zh-CN><en>Announcement title.</en></l></param>
        /// <param name="expireDate"><l><zh-CN>公告过期时间。</zh-CN><en>Announcement expiration time.</en></l></param>
        /// <param name="description"><l><zh-CN>公告正文或摘要文本。</zh-CN><en>Announcement body or summary text.</en></l></param>
        /// <param name="moreLink"><l><zh-CN>桌面端更多链接。</zh-CN><en>Desktop More link.</en></l></param>
        /// <param name="mobileMoreLink"><l><zh-CN>历史移动端更多链接。</zh-CN><en>Legacy mobile More link.</en></l></param>
        public void UpdateAnnouncement(int itemId, string userName, string title, DateTime expireDate,
                                      string description, string moreLink, string mobileMoreLink)
        {
            // <lang>
            //   <zh-CN>保持和新增路径一致的显示名占位策略。</zh-CN>
            //   <en>Keep the same display-name placeholder strategy as the add path.</en>
            // </lang>
            userName = string.IsNullOrEmpty(userName) ? "unknown" : userName;

            var item = _context.Announcements.Single(i => i.ItemId == itemId);

            // <lang>
            //   <zh-CN>旧表没有独立“最后编辑人”字段，当前实现沿用 CreatedByUser 保存最近一次编辑显示名；所属模块和创建时间保持原值。</zh-CN>
            //   <en>The legacy table has no separate last-editor field, so the current implementation reuses CreatedByUser for the latest editor display name; owning module and creation time keep their original values.</en>
            // </lang>
            item.CreatedByUser = userName;
            item.Description = description;
            item.ExpireDate = expireDate;
            item.MoreLink = moreLink;
            item.MobileMoreLink = mobileMoreLink;
            item.Title = title;

            _context.SaveChanges();
        }

        #endregion
    }
}
