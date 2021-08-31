using System.Collections;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    public interface IModulesDb
    {
        IEnumerable<int> GetModulesByModuleDefId(int moduleDefId);
        IEnumerable<IModuleItem> GetModulesByTab(int tabId);

        IModuleItem GetSingleModule(int moduleId);
        Hashtable GetModuleSettings(int moduleId);
        void UpdateModuleOrder(int moduleId, int moduleOrder, string pane);

        int AddModule(int tabId, int moduleOrder, string paneName, string title, int moduleDefId, int cacheTime,
                      string editRoles, bool showMobile);

        void UpdateModule(int moduleId, int moduleOrder, string paneName, string title, int cacheTime, string editRoles,
                          bool showMobile);

        void DeleteModule(int moduleId);
        void UpdateModuleSetting(int moduleId, string key, string val);
    }
}