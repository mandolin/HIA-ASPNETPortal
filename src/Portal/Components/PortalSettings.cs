using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：封装一次门户页面请求所需的门户元数据、导航页签和活动页模块设置。
    ///
    /// English: Encapsulates Portal metadata, navigation tabs, and active-tab module settings required for one Portal page request.
    /// </summary>
    /// <remarks>
    /// 中文：本类从既有数据访问接口读取门户全局项、桌面/移动页签及活动页模块。它不负责读取
    /// 运行级 appSettings 或外置连接串；这些职责由独立的运行时配置组件承担。
    ///
    /// English: This class reads Portal global values, desktop/mobile tabs, and active-tab modules through
    /// existing data-access interfaces. It does not read runtime appSettings or external connection strings;
    /// those responsibilities belong to dedicated runtime-configuration components.
    /// </remarks>
    public class PortalSettings
    {
        /// <summary>
        /// 中文：当前请求对应的活动页签及其模块集合。
        ///
        /// English: Active tab for the current request and its module collection.
        /// </summary>
        public Tab ActiveTab { get; private set; }

        /// <summary>
        /// 中文：桌面版导航页签列表；顺序沿用数据访问层提供的顺序。
        ///
        /// English: Desktop navigation-tab list; order follows the data-access layer result.
        /// </summary>
        public readonly List<ITabItem> DesktopTabs = new List<ITabItem>();

        /// <summary>
        /// 中文：移动版兼容导航页签列表；其保留范围和未来移动端展现方案由后续规划确定。
        ///
        /// English: Mobile-compatibility navigation-tab list; its retention scope and future mobile presentation are decided by later planning.
        /// </summary>
        public readonly List<ITabItem> MobileTabs = new List<ITabItem>();

        /// <summary>
        /// 中文：是否始终显示模块编辑入口。
        ///
        /// English: Whether the module-edit entry point is always displayed.
        /// </summary>
        public bool AlwaysShowEditButton { get; set; }

        /// <summary>
        /// 中文：当前门户的稳定数值标识。
        ///
        /// English: Stable numeric identifier of the current Portal.
        /// </summary>
        public int PortalId { get; set; }

        /// <summary>
        /// 中文：当前门户显示名称。
        ///
        /// English: Display name of the current Portal.
        /// </summary>
        public string PortalName { get; set; }

        /// <summary>
        /// 中文：读取指定页签的既有门户数据，并组装用于页面渲染的 <see cref="PortalSettings"/> 实例。
        ///
        /// English: Reads legacy Portal data for the specified tab and assembles a <see cref="PortalSettings"/> instance for page rendering.
        /// </summary>
        /// <param name="tabIndex">中文：活动页签在当前导航集合中的索引。English: Index of the active tab in the current navigation collection.</param>
        /// <param name="tabId">中文：要加载的活动页签标识。English: Identifier of the active tab to load.</param>
        /// <param name="portalConfig">中文：读取门户全局配置的数据访问接口。English: Data-access interface for Portal global configuration.</param>
        /// <param name="tabsConfig">中文：读取桌面和移动页签的数据访问接口。English: Data-access interface for desktop and mobile tabs.</param>
        /// <param name="modulesConfig">中文：读取页签模块实例的数据访问接口。English: Data-access interface for tab module instances.</param>
        /// <param name="moduleDefConfig">中文：读取模块定义的数据访问接口。English: Data-access interface for module definitions.</param>
        /// <exception cref="System.InvalidOperationException">
        /// 中文：当门户、活动页签或模块定义的严格查询无法得到唯一结果时抛出。
        /// 这些记录构成页面运行时配置，缺失或重复应被视为配置完整性错误，而不是静默回退。
        /// English: Thrown when a strict Portal, active-Tab, or module-definition lookup does not return exactly one result.
        /// These records form the page runtime configuration, so a missing or duplicate record is a configuration-integrity error rather than a silent fallback.
        /// </exception>
        public PortalSettings(int tabIndex, int tabId,
                              IGlobalsDb portalConfig, ITabsDb tabsConfig, IModulesDb modulesConfig,
                              IModuleDefsDb moduleDefConfig)
        {
            // 中文：门户全局配置当前沿用既有单门户记录读取方式。
            // English: Portal global configuration currently follows the legacy single-Portal record lookup.
            IGlobalItem globalSettings = portalConfig.GetSinglePortal(1);

            // 中文：读取门户基本元数据和编辑入口策略。
            // English: Read Portal base metadata and the edit-entry policy.
            PortalId = globalSettings.PortalId;
            PortalName = globalSettings.PortalName;
            AlwaysShowEditButton = globalSettings.AlwaysShowEditButton.Value;

            // 中文：读取桌面版导航页签，保留数据访问层返回的顺序。
            // English: Read desktop navigation tabs and retain the order returned by the data-access layer.
            DesktopTabs.AddRange(tabsConfig.GetTabs());

            // 中文：读取移动版兼容页签，保留数据访问层返回的顺序。
            // English: Read mobile-compatibility tabs and retain the order returned by the data-access layer.
            MobileTabs.AddRange(tabsConfig.GetMobileTabs());

            // 中文：创建活动页签容器，随后填入该页的模块设置。
            // English: Create the active-tab container, then populate its module settings.
            ActiveTab = new Tab(tabIndex, tabsConfig.GetSingleTab(tabId));

            // 中文：按既有数据关系读取活动页签的模块实例。
            // English: Read module instances for the active tab through the legacy data relationship.
            foreach (IModuleItem module in modulesConfig.GetModulesByTab(tabId))
            {
                var moduleSettings = new ModuleSettings(module, moduleDefConfig);

                ActiveTab.Modules.Add(moduleSettings);
            }

            // 中文：按 ModuleOrder 排序，保持既有页面模块展示顺序。
            // English: Sort by ModuleOrder to preserve legacy page-module display order.
            ActiveTab.Modules.Sort();
        }

    }
}
