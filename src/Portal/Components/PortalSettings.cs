using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   This class encapsulates all of the settings for the Portal, as well
    ///   as the configuration settings required to execute the current tab
    ///   view within the portal.
    /// </summary>
    public class PortalSettings
    {
        public Tab ActiveTab { get; private set; }
        public readonly List<ITabItem> DesktopTabs = new List<ITabItem>();
        public readonly List<ITabItem> MobileTabs = new List<ITabItem>();

        ///<summary>
        ///  The PortalSettings Constructor encapsulates all of the logic
        ///  necessary to obtain configuration settings necessary to render
        ///  a Portal Tab view for a given request.
        ///
        ///  These Portal Settings are stored within PortalCFG.xml, and are
        ///  fetched below by calling config.GetSiteSettings().
        ///  The method config.GetSiteSettings() fills the SiteConfiguration
        ///  class, derived from a DataSet, which PortalSettings accesses.
        ///</summary>
        public PortalSettings(int tabIndex, int tabId,
                              IGlobalsDb portalConfig, ITabsDb tabsConfig, IModulesDb modulesConfig,
                              IModuleDefsDb moduleDefConfig)
        {

            // Get the first row in the Global table
            IGlobalItem globalSettings = portalConfig.GetSinglePortal(1);

            // Read Portal global settings 
            PortalId = globalSettings.PortalId;
            PortalName = globalSettings.PortalName;
            AlwaysShowEditButton = globalSettings.AlwaysShowEditButton.Value;            

            // Read the Desktop Tab Information, and sort by Tab Order
            DesktopTabs.AddRange(tabsConfig.GetTabs());

            // Read the Mobile Tab Information, and sort by Tab Order
            MobileTabs.AddRange(tabsConfig.GetMobileTabs());

            ActiveTab = new Tab(tabIndex, tabsConfig.GetSingleTab(tabId));                                                    

            // Read the Module Information for the current (Active) tab
            //TabItem activeTab = tabsConfig.GetSingleTab(tabId);

            // Get Modules for this Tab based on the Data Relation
            foreach (IModuleItem module in modulesConfig.GetModulesByTab(tabId))
            {
                var moduleSettings = new ModuleSettings(module, moduleDefConfig);

                ActiveTab.Modules.Add(moduleSettings);
            }

            // Sort the modules in order of ModuleOrder
            ActiveTab.Modules.Sort();
        }

        public bool AlwaysShowEditButton { get; set; }

        public int PortalId { get; set; }
        public string PortalName { get; set; }
    }
}