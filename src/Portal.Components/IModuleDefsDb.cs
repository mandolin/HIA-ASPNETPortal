using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    public interface IModuleDefsDb
    {
        IEnumerable<IModuleDefinitionItem> GetModuleDefinitions();
        int AddModuleDefinition(string name, string desktopSrc, string mobileSrc);
        void DeleteModuleDefinition(int defId);
        void UpdateModuleDefinition(int defId, string name, string desktopSrc, string mobileSrc);
        IModuleDefinitionItem GetSingleModuleDefinition(int defId);
    }
}