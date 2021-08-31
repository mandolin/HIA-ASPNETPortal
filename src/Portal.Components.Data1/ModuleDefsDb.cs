using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    public class ModuleDefsDb : IModuleDefsDb
    {
        private readonly PortalCfgDbContext _context;
        private readonly IModulesDb _modulesDb;
        private List<ModuleDefinitionItem> _items;

        public ModuleDefsDb(PortalCfgDbContext context, IModulesDb modulesDb)
        {
            _modulesDb = modulesDb;
            _context = context;
            _items = context.ModuleDefinitions.ToList();
        }

        #region IModuleDefsDb Members

        public IEnumerable<IModuleDefinitionItem> GetModuleDefinitions()
        {
            return _items;
        }

        public int AddModuleDefinition(string name, string desktopSrc, string mobileSrc)
        {
            // Create new ModuleDefinitionRow
            var newModuleDef = new ModuleDefinitionItem();

            // Set the parameter values
            newModuleDef.FriendlyName = name;
            newModuleDef.DesktopSourceFile = desktopSrc;
            newModuleDef.MobileSourceFile = mobileSrc;

            // Add the new ModuleDefinitionRow to the ModuleDefinition table
            _context.ModuleDefinitions.Add(newModuleDef);

            _context.SaveChanges();
            _items = _context.ModuleDefinitions.ToList();


            // Return the new ModuleDefID
            return newModuleDef.ModuleDefId;
        }


        public void DeleteModuleDefinition(int defId)
        {
            //
            // Delete information in the Database relating to each Module being deleted
            //
            foreach (int moduleId in _modulesDb.GetModulesByModuleDefId(defId))
            {
                // Delete the xml module associated with the ModuleDef
                // in the configuration file
                _modulesDb.DeleteModule(moduleId);
            }

            // Finish removing Module Definition
            ModuleDefinitionItem row = _context.ModuleDefinitions.Single(i => i.ModuleDefId == defId);
            _context.ModuleDefinitions.Remove(row);

            _context.SaveChanges();
            _items = _context.ModuleDefinitions.ToList();
        }


        public void UpdateModuleDefinition(int defId, string name, string desktopSrc, string mobileSrc)
        {
            // Find the appropriate Module in the Module table and update the properties
            ModuleDefinitionItem modDefRow = _context.ModuleDefinitions.Single(i => i.ModuleDefId == defId);

            modDefRow.FriendlyName = name;
            modDefRow.DesktopSourceFile = desktopSrc;
            modDefRow.MobileSourceFile = mobileSrc;

            _context.SaveChanges();
            _items = _context.ModuleDefinitions.ToList();
        }

        public IModuleDefinitionItem GetSingleModuleDefinition(int defId)
        {
            return _items.Single(i => i.ModuleDefId == defId);
        }

        #endregion
    }
}