using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /* 本文件代码定义了一个 PortalSettings 类，用于封装门户的所有设置以及与当前请求相关的配置信息。构造函数从数据库获取必要的配置信息，并填充门户的相关属性。此外，它还处理了活动页及其相关模块的加载和排序。
     *
     */

    /// <summary>
    ///   此类封装了门户的所有设置，以及在门户中执行当前页面视图所需的配置设置。
    ///   This class encapsulates all of the settings for the Portal, as well
    ///   as the configuration settings required to execute the current tab
    ///   view within the portal.
    /// </summary>
    public class PortalSettings
    {
        /// <summary>
        /// 当前活动页
        /// </summary>
        public Tab ActiveTab { get; private set; }

        /// <summary>
        /// 桌面版页面列表
        /// </summary>
        public readonly List<ITabItem> DesktopTabs = new List<ITabItem>();

        /// <summary>
        /// 移动版页面列表
        /// </summary>
        public readonly List<ITabItem> MobileTabs = new List<ITabItem>();

        public bool AlwaysShowEditButton { get; set; } // 是否总是显示编辑按钮。

        public int PortalId { get; set; } // 门户ID。
        public string PortalName { get; set; } // 门户名称。
        /// <summary>
        ///   PortalSettings 构造函数封装了获取渲染门户页面视图所需配置设置的所有逻辑。
        ///   
        ///   这些门户设置存储在 PortalCFG.xml 文件中，并通过调用 config.GetSiteSettings() 方法获取。方法 config.GetSiteSettings() 填充了 SiteConfiguration 类，该类是从 DataSet 派生的，PortalSettings 访问该类来获取设置。
        /// 
        ///  The PortalSettings Constructor encapsulates all of the logic
        ///  necessary to obtain configuration settings necessary to render
        ///  a Portal Tab view for a given request.
        ///
        ///  These Portal Settings are stored within PortalCFG.xml, and are
        ///  fetched below by calling config.GetSiteSettings().
        ///  The method config.GetSiteSettings() fills the SiteConfiguration
        ///  class, derived from a DataSet, which PortalSettings accesses.
        ///        
        /// </summary>
        public PortalSettings(int tabIndex, int tabId,
                              IGlobalsDb portalConfig, ITabsDb tabsConfig, IModulesDb modulesConfig,
                              IModuleDefsDb moduleDefConfig)
        {
            // Get the first row in the Global table
            // 获取 Global 表的第一行
            IGlobalItem globalSettings = portalConfig.GetSinglePortal(1);

            // Read Portal global settings 
            // 读取门户全局设置
            PortalId = globalSettings.PortalId;
            PortalName = globalSettings.PortalName;
            AlwaysShowEditButton = globalSettings.AlwaysShowEditButton.Value;

            // Read the Desktop Tab Information, and sort by Tab Order
            // 读取桌面版页面信息，并按页面顺序排序
            DesktopTabs.AddRange(tabsConfig.GetTabs());

            // Read the Mobile Tab Information, and sort by Tab Order
            // 读取移动版页面信息，并按页面顺序排序
            MobileTabs.AddRange(tabsConfig.GetMobileTabs());

            // 创建活动页实例
            ActiveTab = new Tab(tabIndex, tabsConfig.GetSingleTab(tabId));

            // 读取当前（活动）页的模块信息
            // TabItem activeTab = tabsConfig.GetSingleTab(tabId);

            // Get Modules for this Tab based on the Data Relation
            // 根据数据关系获取此页的模块
            foreach (IModuleItem module in modulesConfig.GetModulesByTab(tabId))
            {
                var moduleSettings = new ModuleSettings(module, moduleDefConfig);

                ActiveTab.Modules.Add(moduleSettings);
            }

            // Sort the modules in order of ModuleOrder
            // 按 ModuleOrder 排序模块
            ActiveTab.Modules.Sort();
        }

    }
}