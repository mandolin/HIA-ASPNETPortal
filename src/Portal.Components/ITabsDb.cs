using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    public interface ITabsDb
    {
        IEnumerable<ITabItem> GetTabs();
        IEnumerable<ITabItem> GetMobileTabs();

        ITabItem GetSingleTab(int tabId);

        int AddTab(int portalId, string tabName, int tabOrder);

        void UpdateTab(int portalId, int tabId, string tabName, int tabOrder, string authorizedRoles,
                       string mobileTabName, bool showMobile);

        void UpdateTabOrder(int tabId, int tabOrder);
        void DeleteTab(int tabId);
    }
}