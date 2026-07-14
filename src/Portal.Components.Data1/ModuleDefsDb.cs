using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    public class ModuleDefsDb : IModuleDefsDb
    {
        private readonly PortalCfgDbContext _context;
        private readonly IModulesDb _modulesDb;
        private List<ModuleDefinitionItem> _items;

        public ModuleDefsDb(PortalCfgDbContext context, IModulesDb modulesDb)
        {
            _context = context;
            _modulesDb = modulesDb;
            // 加载所有模块定义到内存
            _items = _context.ModuleDefinitions.ToList();
        }

        #region IModuleDefsDb Members

        /// <summary>
        /// 获取所有模块定义。
        /// </summary>
        /// <returns>模块定义集合。</returns>
        public IEnumerable<IModuleDefinitionItem> GetModuleDefinitions()
        {
            return _items;
        }

        /// <summary>
        /// 添加新的模块定义。
        /// </summary>
        /// <param name="name">模块的友好名称。</param>
        /// <param name="desktopSrc">桌面源文件路径。</param>
        /// <param name="mobileSrc">移动源文件路径。</param>
        /// <returns>新模块定义的ID。</returns>
        public int AddModuleDefinition(string name, string desktopSrc, string mobileSrc)
        {
            // 创建新的模块定义对象
            var newModuleDef = new ModuleDefinitionItem
            {
                FriendlyName = name,
                DesktopSourceFile = desktopSrc,
                MobileSourceFile = mobileSrc
            };

            // 将新的模块定义添加到上下文
            _context.ModuleDefinitions.Add(newModuleDef);

            // 提交更改到数据库
            _context.SaveChanges();

            // 重新加载所有模块定义到内存，以反映最新的更改
            _items = _context.ModuleDefinitions.ToList();

            // 返回新模块定义的ID
            return newModuleDef.ModuleDefId;
        }

        /// <summary>
        /// 删除指定ID的模块定义。
        /// </summary>
        /// <param name="defId">模块定义的ID。</param>
        public void DeleteModuleDefinition(int defId)
        {
            // 获取与指定模块定义ID关联的所有模块ID
            var moduleIds = _modulesDb.GetModulesByModuleDefId(defId);

            // 遍历每个模块ID，并删除对应的模块
            foreach (var moduleId in moduleIds)
            {
                _modulesDb.DeleteModule(moduleId);
            }

            // 从上下文中删除模块定义对象
            var moduleDef = _context.ModuleDefinitions.Single(i => i.ModuleDefId == defId);
            _context.ModuleDefinitions.Remove(moduleDef);

            // 提交更改到数据库
            _context.SaveChanges();

            // 重新加载所有模块定义到内存，以反映最新的更改
            _items = _context.ModuleDefinitions.ToList();
        }

        /// <summary>
        /// 更新指定ID的模块定义。
        /// </summary>
        /// <param name="defId">模块定义的ID。</param>
        /// <param name="name">模块的友好名称。</param>
        /// <param name="desktopSrc">桌面源文件路径。</param>
        /// <param name="mobileSrc">移动源文件路径。</param>
        public void UpdateModuleDefinition(int defId, string name, string desktopSrc, string mobileSrc)
        {
            // 查找指定ID的模块定义，并更新其属性
            var modDefRow = _context.ModuleDefinitions.Single(i => i.ModuleDefId == defId);

            modDefRow.FriendlyName = name;
            modDefRow.DesktopSourceFile = desktopSrc;
            modDefRow.MobileSourceFile = mobileSrc;

            // 提交更改到数据库
            _context.SaveChanges();

            // 重新加载所有模块定义到内存，以反映最新的更改
            _items = _context.ModuleDefinitions.ToList();
        }

        /// <summary>
        /// 中文：严格获取单个模块定义。
        /// English: Strictly gets one module definition.
        /// </summary>
        /// <param name="defId">中文：模块定义标识。English: Module-definition identifier.</param>
        /// <returns>中文：唯一的模块定义对象。English: The unique module-definition item.</returns>
        /// <exception cref="InvalidOperationException">
        /// 中文：找不到记录或出现重复记录时抛出；模块定义是运行时配置的一部分，调用方不得将其视为普通可选数据。
        /// English: Thrown when no record or duplicate records exist; a module definition is runtime configuration rather than ordinary optional data.
        /// </exception>
        public IModuleDefinitionItem GetSingleModuleDefinition(int defId)
        {
            // 中文：模块定义参与动态加载；保持严格查询以暴露部署或配置损坏。
            // English: Module definitions participate in dynamic loading; keep this lookup strict to expose deployment or configuration corruption.
            return _items.Single(i => i.ModuleDefId == defId);
        }

        #endregion
    }
}
