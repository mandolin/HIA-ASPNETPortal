using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：受控模块定义的数据访问契约。
    ///
    /// English: Data-access contract for controlled module definitions.
    /// </summary>
    public interface IModuleDefsDb
    {
        /// <summary>中文：读取全部模块定义。English: Reads all module definitions.</summary>
        IEnumerable<IModuleDefinitionItem> GetModuleDefinitions();

        /// <summary>中文：创建已验证路径的模块定义。English: Creates a module definition with validated paths.</summary>
        int AddModuleDefinition(string name, string desktopSrc, string mobileSrc);

        /// <summary>中文：删除已验证且允许删除的模块定义。English: Deletes a verified module definition that is allowed to be removed.</summary>
        void DeleteModuleDefinition(int defId);

        /// <summary>中文：更新已验证模块定义。English: Updates a verified module definition.</summary>
        void UpdateModuleDefinition(int defId, string name, string desktopSrc, string mobileSrc);

        /// <summary>
        /// 中文：严格读取模块定义，供运行时模块装配使用。
        /// English: Strictly reads a module definition for runtime module assembly.
        /// </summary>
        /// <remarks>
        /// 中文：缺失或重复记录代表部署或数据库配置损坏，不应静默返回空值。
        /// English: A missing or duplicate record represents deployment or database configuration damage and must not silently return null.
        /// </remarks>
        IModuleDefinitionItem GetSingleModuleDefinition(int defId);
    }
}
