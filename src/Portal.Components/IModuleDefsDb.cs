using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>受控模块定义的数据访问契约。</zh-CN>
    ///   <en>Data-access contract for controlled module definitions.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该契约继续支撑旧模块定义表；P3.2 之后的新模块来源应优先经过已验证的模块包目录。调用方在写入路径前必须完成受信任部署路径校验，删除操作也只应出现在 Legacy 管理入口中。</zh-CN>
    ///   <en>This contract still supports the legacy module-definition table; after P3.2, new module sources should preferably come from verified module package directories. Callers must validate trusted deployment paths before writing them, and delete operations should remain limited to the Legacy administration entry.</en>
    /// </lang>
    /// </remarks>
    public interface IModuleDefsDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前缓存中的全部模块定义。</zh-CN>
        ///   <en>Reads all module definitions from the current cache.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>用于模块定义列表和模块运行时装配的模块定义集合。</zh-CN>
        ///   <en>The module-definition collection used by definition lists and runtime module assembly.</en>
        /// </l>
        /// </returns>
        IEnumerable<IModuleDefinitionItem> GetModuleDefinitions();

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建已验证路径的模块定义。</zh-CN>
        ///   <en>Creates a module definition with validated paths.</en>
        /// </lang>
        /// </summary>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>面向管理员展示的模块友好名称。</zh-CN>
        ///   <en>The friendly module name displayed to administrators.</en>
        /// </l>
        /// </param>
        /// <param name="desktopSrc">
        /// <l>
        ///   <zh-CN>受信任部署的桌面端 <c>.ascx</c> 虚拟路径。</zh-CN>
        ///   <en>The trusted deployed desktop <c>.ascx</c> virtual path.</en>
        /// </l>
        /// </param>
        /// <param name="mobileSrc">
        /// <l>
        ///   <zh-CN>可选的旧移动端 <c>.ascx</c> 虚拟路径；为空表示不启用旧移动模块。</zh-CN>
        ///   <en>The optional legacy mobile <c>.ascx</c> virtual path; empty means the legacy mobile module is not enabled.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>新建模块定义的数据库标识。</zh-CN>
        ///   <en>The database identifier of the newly created module definition.</en>
        /// </l>
        /// </returns>
        int AddModuleDefinition(string name, string desktopSrc, string mobileSrc);

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除已验证且允许删除的模块定义。</zh-CN>
        ///   <en>Deletes a verified module definition that is allowed to be removed.</en>
        /// </lang>
        /// </summary>
        /// <param name="defId">
        /// <l>
        ///   <zh-CN>要删除的模块定义标识。</zh-CN>
        ///   <en>The module-definition identifier to delete.</en>
        /// </l>
        /// </param>
        void DeleteModuleDefinition(int defId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新已验证模块定义。</zh-CN>
        ///   <en>Updates a verified module definition.</en>
        /// </lang>
        /// </summary>
        /// <param name="defId">
        /// <l>
        ///   <zh-CN>要更新的模块定义标识。</zh-CN>
        ///   <en>The module-definition identifier to update.</en>
        /// </l>
        /// </param>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>面向管理员展示的模块友好名称。</zh-CN>
        ///   <en>The friendly module name displayed to administrators.</en>
        /// </l>
        /// </param>
        /// <param name="desktopSrc">
        /// <l>
        ///   <zh-CN>受信任部署的桌面端 <c>.ascx</c> 虚拟路径。</zh-CN>
        ///   <en>The trusted deployed desktop <c>.ascx</c> virtual path.</en>
        /// </l>
        /// </param>
        /// <param name="mobileSrc">
        /// <l>
        ///   <zh-CN>可选的旧移动端 <c>.ascx</c> 虚拟路径；为空表示不启用旧移动模块。</zh-CN>
        ///   <en>The optional legacy mobile <c>.ascx</c> virtual path; empty means the legacy mobile module is not enabled.</en>
        /// </l>
        /// </param>
        void UpdateModuleDefinition(int defId, string name, string desktopSrc, string mobileSrc);

        /// <summary>
        /// <lang>
        ///   <zh-CN>严格读取模块定义，供运行时模块装配使用。</zh-CN>
        ///   <en>Strictly reads a module definition for runtime module assembly.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>缺失或重复记录代表部署或数据库配置损坏，不应静默返回空值。</zh-CN>
        ///   <en>A missing or duplicate record represents deployment or database configuration damage and must not silently return null.</en>
        /// </lang>
        /// </remarks>
        /// <param name="defId">
        /// <l>
        ///   <zh-CN>要读取的模块定义标识。</zh-CN>
        ///   <en>The module-definition identifier to read.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>唯一匹配的模块定义。</zh-CN>
        ///   <en>The uniquely matched module definition.</en>
        /// </l>
        /// </returns>
        IModuleDefinitionItem GetSingleModuleDefinition(int defId);
    }
}
