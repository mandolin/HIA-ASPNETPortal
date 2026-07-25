using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>旧模块定义表的数据访问实现。</zh-CN>
    ///   <en>Data-access implementation for the legacy module-definition table.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该类仍服务于旧 Admin 模块定义入口和运行时模块装配；P3.2 模块目录机制会优先约束新模块来源，但旧定义表仍需要保持可读、可维护和可回归。写入前的可信部署路径校验由调用页面完成。</zh-CN>
    ///   <en>This class still serves the legacy Admin module-definition entry and runtime module assembly; the P3.2 module catalog constrains new module sources first, but the legacy definition table must remain readable, maintainable, and regression-testable. Trusted deployment path validation is performed by the calling page before writes.</en>
    /// </lang>
    /// </remarks>
    public class ModuleDefsDb : IModuleDefsDb
    {
        private readonly PortalCfgDbContext _context;
        private readonly IModulesDb _modulesDb;
        private List<ModuleDefinitionItem> _items;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化旧模块定义数据访问实现，并加载当前模块定义快照。</zh-CN>
        ///   <en>Initializes the legacy module-definition data-access implementation and loads the current module-definition snapshot.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>门户配置数据库上下文。</zh-CN>
        ///   <en>The portal configuration database context.</en>
        /// </l>
        /// </param>
        /// <param name="modulesDb">
        /// <l>
        ///   <zh-CN>模块实例数据访问服务；删除定义时用于清理引用该定义的旧模块实例。</zh-CN>
        ///   <en>The module-instance data-access service, used to clean up legacy module instances that reference a definition being deleted.</en>
        /// </l>
        /// </param>
        public ModuleDefsDb(PortalCfgDbContext context, IModulesDb modulesDb)
        {
            _context = context;
            _modulesDb = modulesDb;
            // <lang>
            //   <zh-CN>旧页面频繁枚举模块定义，这里保留原有的轻量内存快照模式；每次写入后会主动刷新。</zh-CN>
            //   <en>Legacy pages enumerate module definitions frequently, so keep the existing lightweight in-memory snapshot and refresh it after each write.</en>
            // </lang>
            _items = _context.ModuleDefinitions.ToList();
        }

        #region IModuleDefsDb Members

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取当前快照中的全部模块定义。</zh-CN>
        ///   <en>Gets all module definitions from the current snapshot.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>面向旧管理页和运行时模块装配的模块定义集合。</zh-CN>
        ///   <en>The module-definition collection used by legacy administration pages and runtime module assembly.</en>
        /// </l>
        /// </returns>
        public IEnumerable<IModuleDefinitionItem> GetModuleDefinitions()
        {
            return _items;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>添加新的旧模块定义。</zh-CN>
        ///   <en>Adds a new legacy module definition.</en>
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
        ///   <zh-CN>已由调用方校验的桌面端 <c>.ascx</c> 虚拟路径。</zh-CN>
        ///   <en>The desktop <c>.ascx</c> virtual path already validated by the caller.</en>
        /// </l>
        /// </param>
        /// <param name="mobileSrc">
        /// <l>
        ///   <zh-CN>已由调用方校验的可选旧移动端 <c>.ascx</c> 虚拟路径。</zh-CN>
        ///   <en>The optional legacy mobile <c>.ascx</c> virtual path already validated by the caller.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>新模块定义的数据库标识。</zh-CN>
        ///   <en>The database identifier of the new module definition.</en>
        /// </l>
        /// </returns>
        public int AddModuleDefinition(string name, string desktopSrc, string mobileSrc)
        {
            // <lang>
            //   <zh-CN>这里仅把已经完成页面级校验的契约值落到旧定义表，不在数据层重新解释路径语义。</zh-CN>
            //   <en>This layer only persists contract values that have already passed page-level validation and does not reinterpret path semantics.</en>
            // </lang>
            var newModuleDef = new ModuleDefinitionItem
            {
                FriendlyName = name,
                DesktopSourceFile = desktopSrc,
                MobileSourceFile = mobileSrc
            };

            _context.ModuleDefinitions.Add(newModuleDef);
            _context.SaveChanges();

            // <lang>
            //   <zh-CN>保存后立即刷新快照，避免稍后的页面仍读取旧列表。</zh-CN>
            //   <en>Refresh the snapshot immediately after saving so later pages do not keep reading a stale list.</en>
            // </lang>
            _items = _context.ModuleDefinitions.ToList();
            return newModuleDef.ModuleDefId;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除指定旧模块定义，并清理引用它的旧模块实例。</zh-CN>
        ///   <en>Deletes the specified legacy module definition and cleans up legacy module instances that reference it.</en>
        /// </lang>
        /// </summary>
        /// <param name="defId">
        /// <l>
        ///   <zh-CN>要删除的模块定义标识。</zh-CN>
        ///   <en>The module-definition identifier to delete.</en>
        /// </l>
        /// </param>
        public void DeleteModuleDefinition(int defId)
        {
            // <lang>
            //   <zh-CN>沿用旧行为：删除定义前先删除所有引用该定义的模块实例。该操作会影响页面内容，调用方必须先确认可以执行破坏性清理。</zh-CN>
            //   <en>Keep the legacy behavior: delete every module instance that references this definition before deleting the definition itself. This affects page content, so callers must confirm that destructive cleanup is allowed.</en>
            // </lang>
            var moduleIds = _modulesDb.GetModulesByModuleDefId(defId);

            foreach (var moduleId in moduleIds)
            {
                _modulesDb.DeleteModule(moduleId);
            }

            var moduleDef = _context.ModuleDefinitions.Single(i => i.ModuleDefId == defId);
            _context.ModuleDefinitions.Remove(moduleDef);
            _context.SaveChanges();

            // <lang>
            //   <zh-CN>删除完成后刷新快照，确保旧管理页和运行时读取一致状态。</zh-CN>
            //   <en>Refresh the snapshot after deletion so legacy administration pages and runtime reads observe the same state.</en>
            // </lang>
            _items = _context.ModuleDefinitions.ToList();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新指定旧模块定义。</zh-CN>
        ///   <en>Updates the specified legacy module definition.</en>
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
        ///   <zh-CN>已由调用方校验的桌面端 <c>.ascx</c> 虚拟路径。</zh-CN>
        ///   <en>The desktop <c>.ascx</c> virtual path already validated by the caller.</en>
        /// </l>
        /// </param>
        /// <param name="mobileSrc">
        /// <l>
        ///   <zh-CN>已由调用方校验的可选旧移动端 <c>.ascx</c> 虚拟路径。</zh-CN>
        ///   <en>The optional legacy mobile <c>.ascx</c> virtual path already validated by the caller.</en>
        /// </l>
        /// </param>
        public void UpdateModuleDefinition(int defId, string name, string desktopSrc, string mobileSrc)
        {
            // <lang>
            //   <zh-CN>使用严格查询让缺失或重复的定义立即暴露，避免运行时悄悄保留错误路径。</zh-CN>
            //   <en>Use a strict lookup so missing or duplicate definitions surface immediately instead of silently keeping incorrect paths at runtime.</en>
            // </lang>
            var modDefRow = _context.ModuleDefinitions.Single(i => i.ModuleDefId == defId);

            modDefRow.FriendlyName = name;
            modDefRow.DesktopSourceFile = desktopSrc;
            modDefRow.MobileSourceFile = mobileSrc;

            _context.SaveChanges();

            // <lang>
            //   <zh-CN>保存后刷新快照，使之后的模块装配立即使用更新后的定义。</zh-CN>
            //   <en>Refresh the snapshot after saving so later module assembly uses the updated definition immediately.</en>
            // </lang>
            _items = _context.ModuleDefinitions.ToList();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>严格获取单个模块定义。</zh-CN>
        ///   <en>Strictly gets one module definition.</en>
        /// </lang>
        /// </summary>
        /// <param name="defId">
        /// <l>
        ///   <zh-CN>模块定义标识。</zh-CN>
        ///   <en>Module-definition identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>唯一的模块定义对象。</zh-CN>
        ///   <en>The unique module-definition item.</en>
        /// </l>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <l>
        ///   <zh-CN>找不到记录或出现重复记录时抛出；模块定义是运行时配置的一部分，调用方不得将其视为普通可选数据。</zh-CN>
        ///   <en>Thrown when no record or duplicate records exist; a module definition is runtime configuration rather than ordinary optional data.</en>
        /// </l>
        /// </exception>
        public IModuleDefinitionItem GetSingleModuleDefinition(int defId)
        {
            // <lang>
            //   <zh-CN>模块定义参与动态加载；保持严格查询以暴露部署或配置损坏。</zh-CN>
            //   <en>Module definitions participate in dynamic loading; keep this lookup strict to expose deployment or configuration corruption.</en>
            // </lang>
            return _items.Single(i => i.ModuleDefId == defId);
        }

        #endregion
    }
}
