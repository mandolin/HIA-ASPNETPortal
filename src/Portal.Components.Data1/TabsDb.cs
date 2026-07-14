using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    public class TabsDb : ITabsDb
    {
        private readonly PortalCfgDbContext _context;
        private readonly IPortalDb _portalDb;
        private List<TabItem> _tabs;

        /// <summary>
        /// 初始化页数据库操作类。
        /// </summary>
        /// <param name="context">配置数据库上下文。</param>
        /// <param name="portalDb">门户数据库接口。</param>
        public TabsDb(PortalCfgDbContext context, IPortalDb portalDb)
        {
            _portalDb = portalDb;
            _context = context;
            // 加载所有页项到内存列表中。
            _tabs = _context.Tabs.ToList();
        }

        #region ITabsDb Members

        /// <summary>
        /// 获取所有页项。
        /// </summary>
        /// <returns>页项的可枚举集合。</returns>
        public IEnumerable<ITabItem> GetTabs()
        {
            // 按照页排序号升序排序页项，页ID仅作为稳定兜底。
            return _tabs.OrderBy(i => i.TabOrder).ThenBy(i => i.TabId);
        }

        /// <summary>
        /// 获取所有移动设备上显示的页项。
        /// </summary>
        /// <returns>页项的可枚举集合。</returns>
        public IEnumerable<ITabItem> GetMobileTabs()
        {
            // 筛选出需要在移动设备上显示的页项，并按照页排序号升序排序。
            return _tabs.Where(i => i.ShowMobile==true).OrderBy(i => i.TabOrder).ThenBy(i => i.TabId);
        }

        /// <summary>
        /// 中文：严格获取单个 Tab。
        ///
        /// English: Strictly gets one Tab.
        /// </summary>
        /// <param name="tabId">中文：Tab 标识。English: Tab identifier.</param>
        /// <returns>中文：匹配 Tab。English: Matching Tab.</returns>
        /// <remarks>
        /// 中文：此方法用于已验证的门户运行时配置和写入路径；缺失或重复记录应暴露为完整性故障。
        /// English: This method serves verified Portal runtime configuration and write paths; missing or duplicate records
        /// should surface as integrity failures.
        /// </remarks>
        public ITabItem GetSingleTab(int tabId)
        {
            // 通过页ID获取单个页项。
            return _tabs.Single(i => i.TabId == tabId);
        }

        /// <summary>
        /// 中文：按标识查找 Tab；不存在时返回 <c>null</c>。
        ///
        /// English: Finds a Tab by identifier, returning <c>null</c> when it is absent.
        /// </summary>
        /// <param name="tabId">中文：Tab 标识。English: Tab identifier.</param>
        /// <returns>中文：匹配 Tab；不存在时为 <c>null</c>。English: Matching Tab, or <c>null</c> when absent.</returns>
        public ITabItem FindTabById(int tabId)
        {
            // 中文：重复 Tab 仍应作为配置完整性错误暴露，缺失 Tab 则交由授权层安全拒绝。
            // English: Duplicate Tabs still surface as configuration-integrity errors, while a missing Tab is safely denied by authorization.
            return _tabs.SingleOrDefault(i => i.TabId == tabId);
        }

        /// <summary>
        /// 添加新的页项。
        /// </summary>
        /// <param name="portalId">门户ID。</param>
        /// <param name="tabName">页名称。</param>
        /// <param name="tabOrder">页顺序。</param>
        /// <returns>新添加的页的ID。</returns>
        public int AddTab(int portalId, string tabName, int tabOrder)
        {
            // 创建一个新的页项。
            var newRow = new TabItem
            {
                TabName = tabName,
                TabOrder = tabOrder,
                MobileTabName = String.Empty,
                ShowMobile = true,
                AccessRoles = PortalRoleNames.AllUsers + ";"
            };

            // 将新的页项添加到数据库上下文中。
            _context.Tabs.Add(newRow);

            // 保存更改到数据库。
            _context.SaveChanges();

            // 刷新内存中的页列表。
            _tabs = _context.Tabs.ToList();

            // 返回新添加的页的ID。
            return newRow.TabId;
        }

        /// <summary>
        /// 更新现有的页项。
        /// </summary>
        /// <param name="portalId">门户ID。</param>
        /// <param name="tabId">页ID。</param>
        /// <param name="tabName">页名称。</param>
        /// <param name="tabOrder">页顺序。</param>
        /// <param name="authorizedRoles">授权角色。</param>
        /// <param name="mobileTabName">移动设备上的页名称。</param>
        /// <param name="showMobile">是否在移动设备上显示。</param>
        public void UpdateTab(int portalId, int tabId, string tabName, int tabOrder, string authorizedRoles,
                              string mobileTabName, bool showMobile)
        {
            // 通过页ID获取页项。
            TabItem tabRow = _tabs.Single(i => i.TabId == tabId);

            // 更新页项的信息。
            tabRow.TabName = tabName;
            tabRow.TabOrder = tabOrder;
            tabRow.AccessRoles = authorizedRoles;
            tabRow.MobileTabName = mobileTabName;
            tabRow.ShowMobile = showMobile;

            // 保存更改到数据库。
            _context.SaveChanges();

            // 刷新内存中的页列表。
            _tabs = _context.Tabs.ToList();
        }

        /// <summary>
        /// 更新页项的顺序。
        /// </summary>
        /// <param name="tabId">页ID。</param>
        /// <param name="tabOrder">新的页顺序。</param>
        public void UpdateTabOrder(int tabId, int tabOrder)
        {
            // 通过页ID获取页项。
            TabItem tabRow = _tabs.Single(i => i.TabId == tabId);

            // 更新页项的顺序。
            tabRow.TabOrder = tabOrder;

            // 保存更改到数据库。
            _context.SaveChanges();

            // 刷新内存中的页列表。
            _tabs = _context.Tabs.ToList();
        }

        /// <summary>
        /// 删除指定ID的页项。
        /// </summary>
        /// <param name="tabId">页ID。</param>
        public void DeleteTab(int tabId)
        {
            // 通过页ID获取页项。
            TabItem tabRow = _tabs.Single(i => i.TabId == tabId);

            // 删除页项中关联的模块信息。
            foreach (int moduleId in tabRow.Modules.Select(i => i.ModuleId))
            {
                _portalDb.DeleteModule(moduleId);
            }

            // 从数据库上下文中移除页项。
            _context.Tabs.Remove(tabRow);

            // 保存更改到数据库。
            _context.SaveChanges();

            // 刷新内存中的页列表。
            _tabs = _context.Tabs.ToList();
        }

        #endregion
    }
}
