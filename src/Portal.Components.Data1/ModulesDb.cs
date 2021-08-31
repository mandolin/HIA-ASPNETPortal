using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    public class ModulesDb : IModulesDb
    {
        private readonly PortalCfgDbContext _context;
        private readonly IPortalDb _portalDb;
        private List<ModuleItem> _items;

        public ModulesDb(PortalCfgDbContext context, IPortalDb portalDb)
        {
            _context = context;
            _portalDb = portalDb;
            _items = context.Modules.ToList();
        }

        #region IModulesDb Members

        public IModuleItem GetSingleModule(int moduleId)
        {
            return _items.
                Single(i => i.ModuleId == moduleId);
        }

        public IEnumerable<int> GetModulesByModuleDefId(int moduleDefId)
        {
            return _items.
                Where(i => i.ModuleDefId == moduleDefId).
                Select(i => i.ModuleId);
        }

        public IEnumerable<IModuleItem> GetModulesByTab(int tabId)
        {
            return _items.
                Where(i => i.TabId == tabId);
        }


        public void UpdateModuleOrder(int moduleId, int moduleOrder, string pane)
        {
            ModuleItem moduleRow = _items.
                Single(i => i.ModuleId == moduleId);

            moduleRow.ModuleOrder = moduleOrder;
            moduleRow.PaneName = pane;

            _context.SaveChanges();
        }

        public int AddModule(int tabId, int moduleOrder, string paneName, string title, int moduleDefId, int cacheTime,
                             string editRoles, bool showMobile)
        {
            var newModule = new ModuleItem();

            newModule.ModuleDefId = moduleDefId;
            newModule.ModuleOrder = moduleOrder;
            newModule.ModuleTitle = title;
            newModule.PaneName = paneName;
            newModule.EditRoles = editRoles;
            newModule.CacheTimeout = cacheTime;
            newModule.ShowMobile = showMobile;
            newModule.TabId = tabId;

            _context.Modules.Add(newModule);

            _context.SaveChanges();
            _items = _context.Modules.ToList();

            return newModule.ModuleId;
        }

        public void UpdateModule(int moduleId, int moduleOrder, string paneName, string title, int cacheTime,
                                 string editRoles, bool showMobile)
        {
            ModuleItem moduleRow = _items.
                Single(i => i.ModuleId == moduleId);

            moduleRow.ModuleOrder = moduleOrder;
            moduleRow.ModuleTitle = title;
            moduleRow.PaneName = paneName;
            moduleRow.CacheTimeout = cacheTime;
            moduleRow.EditRoles = editRoles;
            moduleRow.ShowMobile = showMobile;

            _context.SaveChanges();
        }

        public void DeleteModule(int moduleId)
        {
            // Delete information in the Database relating to Module being deleted
            _portalDb.DeleteModule(moduleId);

            // Finish removing Module
            ModuleItem moduleRow = _items.
                Single(i => i.ModuleId == moduleId);
            _context.Modules.Remove(moduleRow);

            _context.SaveChanges();
            _items = _context.Modules.ToList();
        }


        public void UpdateModuleSetting(int moduleId, string key, string val)
        {
            Hashtable settings = GetModuleSettings(moduleId);

            if (settings.ContainsKey(key))
            {
                ModuleSettingItem setting = _items.
                    Single(i => i.ModuleId == moduleId).Settings.Single(i => i.SettingName == key);
                setting.SettingText = val;
            }
            else
            {
                var setting = new ModuleSettingItem {ModuleId = moduleId, SettingName = key, SettingText = val};
                _context.Settings.Add(setting);
            }

            _context.SaveChanges();
            _items = _context.Modules.ToList();
        }

        public Hashtable GetModuleSettings(int moduleId)
        {
            var settings = _items.
                Single(i => i.ModuleId == moduleId).Settings.
                Select(i => new {i.SettingName, i.SettingText});

            var settingsHt = new Hashtable();

            foreach (var row in settings)
            {
                settingsHt[row.SettingName] = row.SettingText;
            }

            return settingsHt;
        }

        #endregion
    }
}