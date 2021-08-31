using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   Class that encapsulates the detailed settings for a specific Tab 
    ///   in the Portal. ModuleSettings implements 
    ///   the IComparable interface so that a List of ModuleItems may be sorted
    ///   by ModuleOrder, List's Sort() method.
    /// </summary>
    public class ModuleSettings : IComparable<ModuleSettings>
    {
        public ModuleSettings(IModuleItem module, IModuleDefsDb moduleDefConfig)
        {
            ModuleTitle = module.ModuleTitle;
            ModuleId = module.ModuleId;            
            ModuleOrder = module.ModuleOrder.Value;            
            PaneName = module.PaneName;
            AuthorizedEditRoles = module.EditRoles;
            CacheTime = module.CacheTimeout.Value;
            ShowMobile = module.ShowMobile.Value;

            // ModuleDefinition data
            IModuleDefinitionItem moduleDefinitionItem = moduleDefConfig.GetSingleModuleDefinition(module.ModuleDefId.Value);

            DesktopSrc = moduleDefinitionItem.DesktopSourceFile;            
        }

        #region IComparable<ModuleItem> Members

        public int CompareTo(ModuleSettings value)
        {
            if (value == null)
            {
                return 1;
            }

            int compareOrder = value.ModuleOrder;

            if (ModuleOrder == compareOrder)
            {
                return 0;
            }
            if (ModuleOrder < compareOrder)
            {
                return -1;
            }
            if (ModuleOrder > compareOrder)
            {
                return 1;
            }
            return 0;
        }

        #endregion

        public int ModuleOrder { get; set; }
        public string ModuleTitle { get; private set; }
        public string PaneName { get; private set; }
        public int ModuleId { get; private set; }
        public string AuthorizedEditRoles { get; private set; }
        public int CacheTime { get; private set; }
        public bool ShowMobile { get; private set; }
        public string DesktopSrc { get; private set; }        
    }
}