using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>封装一次门户页面请求所需的门户元数据、导航页签和活动页模块设置。</zh-CN>
    ///   <en>Encapsulates Portal metadata, navigation tabs, and active-tab module settings required for one Portal page request.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本类从既有数据访问接口读取门户全局项、桌面/移动页签及活动页模块。它不负责读取 运行级 appSettings 或外置连接串；这些职责由独立的运行时配置组件承担。</zh-CN>
    ///   <en>This class reads Portal global values, desktop/mobile tabs, and active-tab modules through existing data-access interfaces. It does not read runtime appSettings or external connection strings; those responsibilities belong to dedicated runtime-configuration components.</en>
    /// </lang>
    /// </remarks>
    public class PortalSettings
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>当前请求对应的活动页签及其模块集合。</zh-CN>
        ///   <en>Active tab for the current request and its module collection.</en>
        /// </lang>
        /// </summary>
        public Tab ActiveTab { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>桌面版导航页签列表；顺序沿用数据访问层提供的顺序。</zh-CN>
        ///   <en>Desktop navigation-tab list; order follows the data-access layer result.</en>
        /// </lang>
        /// </summary>
        public readonly List<ITabItem> DesktopTabs = new List<ITabItem>();

        /// <summary>
        /// <lang>
        ///   <zh-CN>移动版兼容导航页签列表；其保留范围和未来移动端展现方案由后续规划确定。</zh-CN>
        ///   <en>Mobile-compatibility navigation-tab list; its retention scope and future mobile presentation are decided by later planning.</en>
        /// </lang>
        /// </summary>
        public readonly List<ITabItem> MobileTabs = new List<ITabItem>();

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否始终显示模块编辑入口。</zh-CN>
        ///   <en>Whether the module-edit entry point is always displayed.</en>
        /// </lang>
        /// </summary>
        public bool AlwaysShowEditButton { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前门户的稳定数值标识。</zh-CN>
        ///   <en>Stable numeric identifier of the current Portal.</en>
        /// </lang>
        /// </summary>
        public int PortalId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前门户显示名称。</zh-CN>
        ///   <en>Display name of the current Portal.</en>
        /// </lang>
        /// </summary>
        public string PortalName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定页签的既有门户数据，并组装用于页面渲染的 <see cref="PortalSettings"/> 实例。</zh-CN>
        ///   <en>Reads legacy Portal data for the specified tab and assembles a <see cref="PortalSettings"/> instance for page rendering.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabIndex">
        /// <l>
        ///   <zh-CN>活动页签在当前导航集合中的索引。</zh-CN>
        ///   <en>Index of the active tab in the current navigation collection.</en>
        /// </l>
        /// </param>
        /// <param name="tabId">
        /// <l>
        ///   <zh-CN>要加载的活动页签标识。</zh-CN>
        ///   <en>Identifier of the active tab to load.</en>
        /// </l>
        /// </param>
        /// <param name="portalConfig">
        /// <l>
        ///   <zh-CN>读取门户全局配置的数据访问接口。</zh-CN>
        ///   <en>Data-access interface for Portal global configuration.</en>
        /// </l>
        /// </param>
        /// <param name="tabsConfig">
        /// <l>
        ///   <zh-CN>读取桌面和移动页签的数据访问接口。</zh-CN>
        ///   <en>Data-access interface for desktop and mobile tabs.</en>
        /// </l>
        /// </param>
        /// <param name="modulesConfig">
        /// <l>
        ///   <zh-CN>读取页签模块实例的数据访问接口。</zh-CN>
        ///   <en>Data-access interface for tab module instances.</en>
        /// </l>
        /// </param>
        /// <param name="moduleDefConfig">
        /// <l>
        ///   <zh-CN>读取模块定义的数据访问接口。</zh-CN>
        ///   <en>Data-access interface for module definitions.</en>
        /// </l>
        /// </param>
        /// <exception cref="System.InvalidOperationException">
        /// <l>
        ///   <zh-CN>当门户、活动页签或模块定义的严格查询无法得到唯一结果时抛出。 这些记录构成页面运行时配置，缺失或重复应被视为配置完整性错误，而不是静默回退。</zh-CN>
        ///   <en>Thrown when a strict Portal, active-Tab, or module-definition lookup does not return exactly one result. These records form the page runtime configuration, so a missing or duplicate record is a configuration-integrity error rather than a silent fallback.</en>
        /// </l>
        /// </exception>
        public PortalSettings(int tabIndex, int tabId,
                              IGlobalsDb portalConfig, ITabsDb tabsConfig, IModulesDb modulesConfig,
                              IModuleDefsDb moduleDefConfig)
        {
            // <lang>
            //   <zh-CN>门户全局配置当前沿用既有单门户记录读取方式。</zh-CN>
            //   <en>Portal global configuration currently follows the legacy single-Portal record lookup.</en>
            // </lang>
            IGlobalItem globalSettings = portalConfig.GetSinglePortal(1);

            // <lang>
            //   <zh-CN>读取门户基本元数据和编辑入口策略。</zh-CN>
            //   <en>Read Portal base metadata and the edit-entry policy.</en>
            // </lang>
            PortalId = globalSettings.PortalId;
            PortalName = globalSettings.PortalName;
            AlwaysShowEditButton = globalSettings.AlwaysShowEditButton.Value;

            // <lang>
            //   <zh-CN>读取桌面版导航页签，保留数据访问层返回的顺序。</zh-CN>
            //   <en>Read desktop navigation tabs and retain the order returned by the data-access layer.</en>
            // </lang>
            DesktopTabs.AddRange(tabsConfig.GetTabs());

            // <lang>
            //   <zh-CN>读取移动版兼容页签，保留数据访问层返回的顺序。</zh-CN>
            //   <en>Read mobile-compatibility tabs and retain the order returned by the data-access layer.</en>
            // </lang>
            MobileTabs.AddRange(tabsConfig.GetMobileTabs());

            // <lang>
            //   <zh-CN>创建活动页签容器，随后填入该页的模块设置。</zh-CN>
            //   <en>Create the active-tab container, then populate its module settings.</en>
            // </lang>
            ActiveTab = new Tab(tabIndex, tabsConfig.GetSingleTab(tabId));

            // <lang>
            //   <zh-CN>按既有数据关系读取活动页签的模块实例。</zh-CN>
            //   <en>Read module instances for the active tab through the legacy data relationship.</en>
            // </lang>
            foreach (IModuleItem module in modulesConfig.GetModulesByTab(tabId))
            {
                var moduleSettings = new ModuleSettings(module, moduleDefConfig);

                ActiveTab.Modules.Add(moduleSettings);
            }

            // <lang>
            //   <zh-CN>按 ModuleOrder 排序，保持既有页面模块展示顺序。</zh-CN>
            //   <en>Sort by ModuleOrder to preserve legacy page-module display order.</en>
            // </lang>
            ActiveTab.Modules.Sort();
        }

    }
}
