using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>通过 EF 上下文读写旧链接模块数据。</zh-CN>
    ///   <en>Reads and writes legacy link-module data through the EF context.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本实现只处理链接表持久化和按模块过滤；URL 策略、展示编码、排序输入归一化、编辑权限和条目归属由调用页与模块控件承担。</zh-CN>
    ///   <en>This implementation only handles link persistence and module filtering; URL policy, display encoding, sort-input normalization, edit permission, and item ownership are handled by caller pages and module controls.</en>
    /// </lang>
    /// </remarks>
    public class LinksDb : ILinksDb
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
        ///   <zh-CN>初始化链接数据访问对象。</zh-CN>
        ///   <en>Initializes the link data-access object.</en>
        /// </lang>
        /// </summary>
        /// <param name="context"><l><zh-CN>由 Unity 注入的门户 EF 上下文。</zh-CN><en>Portal EF context injected by Unity.</en></l></param>
        public LinksDb(PortalDbContext context)
        {
            _context = context;
        }

        #region ILinksDb Members

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取指定模块下的链接列表。</zh-CN>
        ///   <en>Gets links for the specified module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId"><l><zh-CN>链接模块实例 ID。</zh-CN><en>Link module instance ID.</en></l></param>
        /// <returns><l><zh-CN>属于该模块的链接集合。</zh-CN><en>Links belonging to the module.</en></l></returns>
        public IEnumerable<ILinkItem> GetLinks(int moduleId)
        {
            // <lang>
            //   <zh-CN>只按模块过滤，不在数据层追加排序规则；需要排序的页面按 ViewOrder 或页面规则处理。</zh-CN>
            //   <en>Filter only by module and do not add a data-layer ordering rule; pages that need ordering should use ViewOrder or page-level rules.</en>
            // </lang>
            return _context.Links.Where(i => i.ModuleId == moduleId).ToList<ILinkItem>();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取单个链接；用户指定的不存在标识返回 <c>null</c>，由调用页决定其授权失败响应。</zh-CN>
        ///   <en>Gets one link. A user-supplied missing identifier returns <c>null</c> so the caller can select its authorization-failure response.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>链接标识符。</zh-CN>
        ///   <en>Link identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>指定链接；不存在时为 <c>null</c>。</zh-CN>
        ///   <en>The requested link, or <c>null</c> when it does not exist.</en>
        /// </l>
        /// </returns>
        public ILinkItem GetSingleLink(int itemId)
        {
            // <lang>
            //   <zh-CN>编辑入口的 ItemId 来自请求，未命中返回空值，让页面层统一输出低敏提示或拒绝访问响应。</zh-CN>
            //   <en>The editor item identifier comes from a request; misses return null so the page layer can emit a low-sensitivity message or access-denied response.</en>
            // </lang>
            return _context.Links.SingleOrDefault(i => i.ItemId == itemId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除指定链接记录。</zh-CN>
        ///   <en>Deletes the specified link record.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该方法使用 <c>Single</c> 保留旧代码的严格语义；调用页应先确认条目存在且属于当前模块。</zh-CN>
        ///   <en>This method keeps the strict legacy <c>Single</c> semantics; caller pages should first confirm that the item exists and belongs to the current module.</en>
        /// </lang>
        /// </remarks>
        /// <param name="itemId"><l><zh-CN>链接标识符。</zh-CN><en>Link identifier.</en></l></param>
        public void DeleteLink(int itemId)
        {
            // <lang>
            //   <zh-CN>待删除链接必须唯一存在；缺失或重复说明调用页归属校验前提或数据完整性已失效。</zh-CN>
            //   <en>The link to delete must exist uniquely; a miss or duplicate means caller-side ownership validation assumptions or data integrity have failed.</en>
            // </lang>
            var item = _context.Links.Single(i => i.ItemId == itemId);

            // <lang>
            //   <zh-CN>删除动作直接提交到旧表；审计、权限和站内回跳由编辑页完成。</zh-CN>
            //   <en>The delete action is committed directly to the legacy table; audit, permission, and safe return navigation are completed by the editor page.</en>
            // </lang>
            _context.Links.Remove(item);

            // <lang>
            //   <zh-CN>提交当前删除批次；本方法不触碰外部 URL，也不刷新导航或页面缓存。</zh-CN>
            //   <en>Commit the current deletion batch; this method does not touch external URLs or refresh navigation/page caches.</en>
            // </lang>
            _context.SaveChanges();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>新增一个链接记录。</zh-CN>
        ///   <en>Adds a new link record.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>URL 安全策略和 ViewOrder 输入归一化由编辑页完成；展示控件继续决定是否渲染为可点击链接。</zh-CN>
        ///   <en>URL safety policy and ViewOrder normalization are completed by the editor page; display controls continue to decide whether a value is rendered as a clickable link.</en>
        /// </lang>
        /// </remarks>
        /// <param name="moduleId"><l><zh-CN>链接模块实例 ID。</zh-CN><en>Link module instance ID.</en></l></param>
        /// <param name="userName"><l><zh-CN>用于历史显示的创建人名称；空值会降级为旧占位值。</zh-CN><en>Creator name used for historical display; a blank value falls back to the legacy placeholder.</en></l></param>
        /// <param name="title"><l><zh-CN>链接标题。</zh-CN><en>Link title.</en></l></param>
        /// <param name="url"><l><zh-CN>桌面端链接地址。</zh-CN><en>Desktop link URL.</en></l></param>
        /// <param name="mobileUrl"><l><zh-CN>历史移动端链接地址。</zh-CN><en>Legacy mobile link URL.</en></l></param>
        /// <param name="viewOrder"><l><zh-CN>显示顺序。</zh-CN><en>Display order.</en></l></param>
        /// <param name="description"><l><zh-CN>链接描述文本。</zh-CN><en>Link description text.</en></l></param>
        /// <returns><l><zh-CN>新增链接的数据库标识符。</zh-CN><en>Database identifier of the new link.</en></l></returns>
        public int AddLink(int moduleId, string userName, string title, string url, string mobileUrl,
                           int viewOrder, string description)
        {
            // <lang>
            //   <zh-CN>旧内容表只有显示用创建人字段；缺失认证名称时使用占位值，不把它作为权限依据。</zh-CN>
            //   <en>The legacy content table only has a display creator field; when the authenticated name is missing, use a placeholder and do not treat it as an authorization source.</en>
            // </lang>
            userName = userName ?? "unknown";

            // <lang>
            //   <zh-CN>新链接实体承载一次新增投影；URL、移动端 URL 和排序值均来自编辑页的前置策略处理。</zh-CN>
            //   <en>The new link entity carries one create projection; URL, mobile URL, and order value all come from the editor page's prior policy handling.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>把新实体加入 EF 跟踪集，数据库标识会在保存后回填到同一对象。</zh-CN>
            //   <en>Add the new entity to the EF tracking set; the database identifier is populated back onto the same object after saving.</en>
            // </lang>
            _context.Links.Add(item);

            // <lang>
            //   <zh-CN>提交新增批次；本层不重判权限、不访问目标 URL，也不决定展示时是否可点击。</zh-CN>
            //   <en>Commit the create batch; this layer does not re-authorize, access the target URL, or decide whether it is clickable at display time.</en>
            // </lang>
            _context.SaveChanges();

            // <lang>
            //   <zh-CN>返回 EF 保存后生成的旧表主键，供编辑页继续导航或反馈。</zh-CN>
            //   <en>Return the legacy-table primary key generated after EF saving, allowing the editor page to continue navigation or feedback.</en>
            // </lang>
            return item.ItemId;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新指定链接记录的可编辑字段。</zh-CN>
        ///   <en>Updates editable fields of the specified link record.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>更新不会改变链接所属模块和创建时间；调用页负责确认当前用户可编辑该条目。</zh-CN>
        ///   <en>Updating does not change the link's owning module or creation time; the caller page is responsible for confirming that the current user may edit the item.</en>
        /// </lang>
        /// </remarks>
        /// <param name="itemId"><l><zh-CN>链接标识符。</zh-CN><en>Link identifier.</en></l></param>
        /// <param name="userName"><l><zh-CN>用于历史显示的最后编辑人名称。</zh-CN><en>Last editor name used for historical display.</en></l></param>
        /// <param name="title"><l><zh-CN>链接标题。</zh-CN><en>Link title.</en></l></param>
        /// <param name="url"><l><zh-CN>桌面端链接地址。</zh-CN><en>Desktop link URL.</en></l></param>
        /// <param name="mobileUrl"><l><zh-CN>历史移动端链接地址。</zh-CN><en>Legacy mobile link URL.</en></l></param>
        /// <param name="viewOrder"><l><zh-CN>显示顺序。</zh-CN><en>Display order.</en></l></param>
        /// <param name="description"><l><zh-CN>链接描述文本。</zh-CN><en>Link description text.</en></l></param>
        public void UpdateLink(int itemId, string userName, string title, string url, string mobileUrl,
                               int viewOrder, string description)
        {
            // <lang>
            //   <zh-CN>保持和新增路径一致的显示名占位策略。</zh-CN>
            //   <en>Keep the same display-name placeholder strategy as the add path.</en>
            // </lang>
            userName = userName ?? "unknown";

            // <lang>
            //   <zh-CN>更新路径要求目标链接唯一存在；调用页已完成模块归属和编辑权限判断。</zh-CN>
            //   <en>The update path requires the target link to exist uniquely; the caller page has already completed module ownership and edit-permission checks.</en>
            // </lang>
            var item = _context.Links.Single(i => i.ItemId == itemId);

            // <lang>
            //   <zh-CN>旧表没有独立“最后编辑人”字段，当前实现沿用 CreatedByUser 保存最近一次编辑显示名。</zh-CN>
            //   <en>The legacy table has no separate last-editor field, so the current implementation reuses CreatedByUser for the latest editor display name.</en>
            // </lang>
            item.CreatedByUser = userName;
            item.Title = title;
            item.Url = url;
            item.MobileUrl = mobileUrl;
            item.ViewOrder = viewOrder;
            item.Description = description;

            // <lang>
            //   <zh-CN>提交当前可编辑字段更新；所属模块、原创建时间和页面级审计不在此处改变。</zh-CN>
            //   <en>Commit the current editable-field update; owning module, original creation time, and page-level audit are not changed here.</en>
            // </lang>
            _context.SaveChanges();
        }

        #endregion
    }
}
