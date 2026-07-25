using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>基于旧门户配置上下文的 Tab 数据访问实现。</zh-CN>
    ///   <en>Tab data-access implementation backed by the legacy Portal configuration context.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本实现维持旧 Web Forms 门户的页签排序、角色串和移动端字段兼容性；写入后会刷新内存快照，避免同一请求生命周期内继续读取旧集合。</zh-CN>
    ///   <en>This implementation preserves the legacy Web Forms portal tab ordering, role-string and mobile-field compatibility; write operations refresh the in-memory snapshot so later reads in the same request do not use stale collections.</en>
    /// </lang>
    /// </remarks>
    public class TabsDb : ITabsDb
    {
        private readonly PortalCfgDbContext _context;
        private readonly IPortalDb _portalDb;
        private List<TabItem> _tabs;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化 Tab 数据访问实现，并加载当前 Tab 快照。</zh-CN>
        ///   <en>Initializes the Tab data-access implementation and loads the current Tab snapshot.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>配置数据库上下文。</zh-CN>
        ///   <en>Configuration database context.</en>
        /// </l>
        /// </param>
        /// <param name="portalDb">
        /// <l>
        ///   <zh-CN>门户模块数据库接口，用于 Tab 删除时清理关联模块。</zh-CN>
        ///   <en>Portal module database contract used to remove linked modules when a Tab is deleted.</en>
        /// </l>
        /// </param>
        public TabsDb(PortalCfgDbContext context, IPortalDb portalDb)
        {
            _portalDb = portalDb;
            _context = context;
            // <lang>
            //   <zh-CN>构造期保留一份 Tab 快照；写入方法会显式刷新，避免旧后台同一请求内读到过期排序或角色串。</zh-CN>
            //   <en>The constructor keeps a Tab snapshot; write methods refresh it explicitly so the legacy admin flow does not read stale ordering or role strings within the same request.</en>
            // </lang>
            _tabs = _context.Tabs.ToList();
        }

        #region ITabsDb Members

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取所有 Tab，并按旧门户显示顺序返回。</zh-CN>
        ///   <en>Gets all Tabs in the legacy portal display order.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>按 `TabOrder` 与 `TabId` 排序的 Tab 集合。</zh-CN>
        ///   <en>Tabs ordered by `TabOrder` and `TabId`.</en>
        /// </l>
        /// </returns>
        public IEnumerable<ITabItem> GetTabs()
        {
            // <lang>
            //   <zh-CN>`TabId` 只作为同序号时的稳定兜底，避免维护界面因为数据库返回顺序变化而抖动。</zh-CN>
            //   <en>`TabId` is only a stable tiebreaker for identical sort values, preventing the maintenance UI from shifting with database return order.</en>
            // </lang>
            return _tabs.OrderBy(i => i.TabOrder).ThenBy(i => i.TabId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取旧移动端标记为可显示的 Tab。</zh-CN>
        ///   <en>Gets Tabs marked as visible for the legacy mobile surface.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>仍保留旧 `ShowMobile` 字段过滤结果；当前新主题移动端方案不依赖它。</zh-CN>
        ///   <en>Filtered results that still honor the legacy `ShowMobile` field; the current new-theme mobile approach does not depend on it.</en>
        /// </l>
        /// </returns>
        public IEnumerable<ITabItem> GetMobileTabs()
        {
            // <lang>
            //   <zh-CN>保持历史字段语义，方便旧模块或迁移脚本读取；排序规则与桌面 Tab 一致。</zh-CN>
            //   <en>The historical field semantics are retained for legacy modules or migration scripts; ordering matches desktop Tabs.</en>
            // </lang>
            return _tabs.Where(i => i.ShowMobile==true).OrderBy(i => i.TabOrder).ThenBy(i => i.TabId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>严格获取单个 Tab。</zh-CN>
        ///   <en>Strictly gets one Tab.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabId">
        /// <l>
        ///   <zh-CN>Tab 标识。</zh-CN>
        ///   <en>Tab identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配 Tab。</zh-CN>
        ///   <en>Matching Tab.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>此方法用于已验证的门户运行时配置和写入路径；缺失或重复记录应暴露为完整性故障。</zh-CN>
        ///   <en>This method serves verified Portal runtime configuration and write paths; missing or duplicate records should surface as integrity failures.</en>
        /// </lang>
        /// </remarks>
        public ITabItem GetSingleTab(int tabId)
        {
            // <lang>
            //   <zh-CN>`Single` 会把缺失和重复都暴露出来；调用方应只在已经确认 Tab 存在的路径使用。</zh-CN>
            //   <en>`Single` exposes both missing and duplicate records; callers should use this only after the Tab is known to exist.</en>
            // </lang>
            return _tabs.Single(i => i.TabId == tabId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按标识查找 Tab；不存在时返回 <c>null</c>。</zh-CN>
        ///   <en>Finds a Tab by identifier, returning <c>null</c> when it is absent.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabId">
        /// <l>
        ///   <zh-CN>Tab 标识。</zh-CN>
        ///   <en>Tab identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配 Tab；不存在时为 <c>null</c>。</zh-CN>
        ///   <en>Matching Tab, or <c>null</c> when absent.</en>
        /// </l>
        /// </returns>
        public ITabItem FindTabById(int tabId)
        {
            // <lang>
            //   <zh-CN>重复 Tab 仍应作为配置完整性错误暴露，缺失 Tab 则交由授权层安全拒绝。</zh-CN>
            //   <en>Duplicate Tabs still surface as configuration-integrity errors, while a missing Tab is safely denied by authorization.</en>
            // </lang>
            return _tabs.SingleOrDefault(i => i.TabId == tabId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>新增 Tab，并使用旧门户默认可见性和移动字段初始值。</zh-CN>
        ///   <en>Adds a Tab with the legacy portal default visibility and mobile-field values.</en>
        /// </lang>
        /// </summary>
        /// <param name="portalId">
        /// <l>
        ///   <zh-CN>门户标识；保留旧接口签名，当前配置表不直接使用该值。</zh-CN>
        ///   <en>Portal identifier retained for the legacy contract; the current configuration table does not use it directly.</en>
        /// </l>
        /// </param>
        /// <param name="tabName">
        /// <l>
        ///   <zh-CN>Tab 显示名称。</zh-CN>
        ///   <en>Tab display name.</en>
        /// </l>
        /// </param>
        /// <param name="tabOrder">
        /// <l>
        ///   <zh-CN>Tab 排序号。</zh-CN>
        ///   <en>Tab ordering value.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>新 Tab 的数据库标识。</zh-CN>
        ///   <en>Database identifier of the new Tab.</en>
        /// </l>
        /// </returns>
        public int AddTab(int portalId, string tabName, int tabOrder)
        {
            // <lang>
            //   <zh-CN>旧门户新增 Tab 默认给 `All Users` 访问，之后由后台页面再调整角色串。</zh-CN>
            //   <en>Legacy portal Tabs are created with `All Users` access by default; the admin page can adjust the role string afterwards.</en>
            // </lang>
            var newRow = new TabItem
            {
                TabName = tabName,
                TabOrder = tabOrder,
                MobileTabName = String.Empty,
                ShowMobile = true,
                AccessRoles = PortalRoleNames.AllUsers + ";"
            };

            _context.Tabs.Add(newRow);

            // <lang>
            //   <zh-CN>保存后立即刷新本地快照，使返回的 `TabId` 和之后的读取来自同一持久化状态。</zh-CN>
            //   <en>The local snapshot is refreshed right after saving so the returned `TabId` and later reads reflect the same persisted state.</en>
            // </lang>
            _context.SaveChanges();
            _tabs = _context.Tabs.ToList();

            return newRow.TabId;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新现有 Tab 的显示名称、排序、角色串和旧移动端字段。</zh-CN>
        ///   <en>Updates an existing Tab's display name, order, role string and legacy mobile fields.</en>
        /// </lang>
        /// </summary>
        /// <param name="portalId">
        /// <l>
        ///   <zh-CN>门户标识；保留旧接口签名，当前配置表不直接使用该值。</zh-CN>
        ///   <en>Portal identifier retained for the legacy contract; the current configuration table does not use it directly.</en>
        /// </l>
        /// </param>
        /// <param name="tabId">
        /// <l>
        ///   <zh-CN>Tab 标识。</zh-CN>
        ///   <en>Tab identifier.</en>
        /// </l>
        /// </param>
        /// <param name="tabName">
        /// <l>
        ///   <zh-CN>Tab 显示名称。</zh-CN>
        ///   <en>Tab display name.</en>
        /// </l>
        /// </param>
        /// <param name="tabOrder">
        /// <l>
        ///   <zh-CN>Tab 排序号。</zh-CN>
        ///   <en>Tab ordering value.</en>
        /// </l>
        /// </param>
        /// <param name="authorizedRoles">
        /// <l>
        ///   <zh-CN>旧门户分号分隔访问角色串。</zh-CN>
        ///   <en>Legacy semicolon-delimited access-role string.</en>
        /// </l>
        /// </param>
        /// <param name="mobileTabName">
        /// <l>
        ///   <zh-CN>旧移动端 Tab 名称；当前新主题移动端可不使用。</zh-CN>
        ///   <en>Legacy mobile Tab name; the current new-theme mobile surface may ignore it.</en>
        /// </l>
        /// </param>
        /// <param name="showMobile">
        /// <l>
        ///   <zh-CN>旧移动端显示标记。</zh-CN>
        ///   <en>Legacy mobile visibility flag.</en>
        /// </l>
        /// </param>
        public void UpdateTab(int portalId, int tabId, string tabName, int tabOrder, string authorizedRoles,
                              string mobileTabName, bool showMobile)
        {
            // <lang>
            //   <zh-CN>更新路径使用 `Single` 保持旧后台的强一致假设：目标 Tab 缺失应暴露为配置问题。</zh-CN>
            //   <en>The update path uses `Single` to keep the legacy admin assumption of strong consistency: a missing target Tab should surface as a configuration problem.</en>
            // </lang>
            TabItem tabRow = _tabs.Single(i => i.TabId == tabId);

            tabRow.TabName = tabName;
            tabRow.TabOrder = tabOrder;
            tabRow.AccessRoles = authorizedRoles;
            tabRow.MobileTabName = mobileTabName;
            tabRow.ShowMobile = showMobile;

            _context.SaveChanges();

            // <lang>
            //   <zh-CN>刷新快照，确保后台连续操作时看到刚保存的排序和角色串。</zh-CN>
            //   <en>The snapshot is refreshed so consecutive admin operations see the just-saved order and role string.</en>
            // </lang>
            _tabs = _context.Tabs.ToList();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>只更新 Tab 排序号。</zh-CN>
        ///   <en>Updates only the Tab ordering value.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabId">
        /// <l>
        ///   <zh-CN>Tab 标识。</zh-CN>
        ///   <en>Tab identifier.</en>
        /// </l>
        /// </param>
        /// <param name="tabOrder">
        /// <l>
        ///   <zh-CN>新的 Tab 排序号。</zh-CN>
        ///   <en>New Tab ordering value.</en>
        /// </l>
        /// </param>
        public void UpdateTabOrder(int tabId, int tabOrder)
        {
            // <lang>
            //   <zh-CN>排序按钮通常连续触发；每次写入后刷新快照，避免下一次移动基于旧顺序计算。</zh-CN>
            //   <en>Ordering buttons are often triggered consecutively; refreshing after each write prevents the next move from being calculated from stale ordering.</en>
            // </lang>
            TabItem tabRow = _tabs.Single(i => i.TabId == tabId);

            tabRow.TabOrder = tabOrder;

            _context.SaveChanges();
            _tabs = _context.Tabs.ToList();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除指定 Tab，并清理其关联模块实例。</zh-CN>
        ///   <en>Deletes the specified Tab and removes its linked module instances.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabId">
        /// <l>
        ///   <zh-CN>Tab 标识。</zh-CN>
        ///   <en>Tab identifier.</en>
        /// </l>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>删除 Tab 会调用模块数据访问层删除实例，但不删除受信任部署的模块物理包；这与 P3.2 的模块包边界一致。</zh-CN>
        ///   <en>Deleting a Tab asks the module data layer to remove instances, but it does not delete trusted deployed module package files; this matches the P3.2 module-package boundary.</en>
        /// </lang>
        /// </remarks>
        public void DeleteTab(int tabId)
        {
            TabItem tabRow = _tabs.Single(i => i.TabId == tabId);

            // <lang>
            //   <zh-CN>先复制并遍历模块标识，避免删除过程中修改 `Modules` 集合导致枚举异常。</zh-CN>
            //   <en>Module identifiers are selected before deletion so modifying the `Modules` collection during removal does not invalidate enumeration.</en>
            // </lang>
            foreach (int moduleId in tabRow.Modules.Select(i => i.ModuleId))
            {
                _portalDb.DeleteModule(moduleId);
            }

            _context.Tabs.Remove(tabRow);

            _context.SaveChanges();

            // <lang>
            //   <zh-CN>删除完成后刷新快照，防止后台返回列表时继续显示已删除 Tab。</zh-CN>
            //   <en>The snapshot is refreshed after deletion so the admin list does not continue showing the removed Tab.</en>
            // </lang>
            _tabs = _context.Tabs.ToList();
        }

        #endregion
    }
}
