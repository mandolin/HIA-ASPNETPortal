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

        public TabsDb(PortalCfgDbContext context, IPortalDb portalDb)
        {
            _portalDb = portalDb;
            _context = context;
            _tabs = _context.Tabs.ToList();
        }

        #region ITabsDb Members

        public IEnumerable<ITabItem> GetTabs()
        {
            return _tabs.
                OrderBy(i => i.TabId);
        }

        public IEnumerable<ITabItem> GetMobileTabs()
        {
            return _tabs.
                Where(i => i.ShowMobile == true).
                OrderBy(i => i.TabId);
        }

        public ITabItem GetSingleTab(int tabId)
        {
            return _tabs.Single(i => i.TabId == tabId);
        }


        public int AddTab(int portalId, string tabName, int tabOrder)
        {
            var newRow = new TabItem();

            newRow.TabName = tabName;
            newRow.TabOrder = tabOrder;
            newRow.MobileTabName = String.Empty;
            newRow.ShowMobile = true;
            newRow.AccessRoles = "All Users;";

            _context.Tabs.Add(newRow);

            _context.SaveChanges();
            _tabs = _context.Tabs.ToList();

            return newRow.TabId;
        }

        public void UpdateTab(int portalId, int tabId, string tabName, int tabOrder, string authorizedRoles,
                              string mobileTabName, bool showMobile)
        {
            TabItem tabRow = _tabs.Single(i => i.TabId == tabId);

            tabRow.TabName = tabName;
            tabRow.TabOrder = tabOrder;
            tabRow.AccessRoles = authorizedRoles;
            tabRow.MobileTabName = mobileTabName;
            tabRow.ShowMobile = showMobile;

            _context.SaveChanges();
            _tabs = _context.Tabs.ToList();
        }

        public void UpdateTabOrder(int tabId, int tabOrder)
        {
            TabItem tabRow = _tabs.Single(i => i.TabId == tabId);

            tabRow.TabOrder = tabOrder;

            _context.SaveChanges();
            _tabs = _context.Tabs.ToList();
        }

        public void DeleteTab(int tabId)
        {
            TabItem tabRow = _tabs.Single(i => i.TabId == tabId);

            // Delete information in the Database relating to each Module being deleted            
            foreach (int moduleId in tabRow.Modules.Select(i => i.ModuleId))
            {
                _portalDb.DeleteModule(moduleId);
            }

            // Finish removing the Tab row from the Xml file
            _context.Tabs.Remove(tabRow);

            _context.SaveChanges();
            _tabs = _context.Tabs.ToList();
        }

        #endregion
    }
}