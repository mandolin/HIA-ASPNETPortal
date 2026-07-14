using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
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
            // 加载所有模块到内存
            _items = _context.Modules.ToList();
        }

        #region IModulesDb Members

        /// <summary>
        /// 中文：严格获取单个模块实例。
        ///
        /// English: Strictly gets one module instance.
        /// </summary>
        /// <param name="moduleId">中文：模块实例标识。English: Module-instance identifier.</param>
        /// <returns>中文：匹配模块实例。English: Matching module instance.</returns>
        /// <remarks>
        /// 中文：此方法用于已验证的运行时配置和写入路径；缺失或重复记录应暴露为完整性故障。
        /// English: This method serves verified runtime configuration and write paths; missing or duplicate records
        /// should surface as integrity failures.
        /// </remarks>
        public IModuleItem GetSingleModule(int moduleId)
        {
            // 从内存列表中查找指定ID的模块
            return _items.Single(i => i.ModuleId == moduleId);
        }

        /// <summary>
        /// 中文：按标识查找模块实例；不存在时返回 <c>null</c>。
        ///
        /// English: Finds a module instance by identifier, returning <c>null</c> when it is absent.
        /// </summary>
        /// <param name="moduleId">中文：模块实例标识。English: Module-instance identifier.</param>
        /// <returns>中文：匹配模块实例；不存在时为 <c>null</c>。English: Matching module instance, or <c>null</c> when absent.</returns>
        public IModuleItem FindModuleById(int moduleId)
        {
            // 中文：SingleOrDefault 保留重复记录的完整性异常，同时允许授权层对缺失模块安全拒绝。
            // English: SingleOrDefault preserves duplicate-record integrity errors while allowing authorization to deny a missing module safely.
            return _items.SingleOrDefault(i => i.ModuleId == moduleId);
        }

        /// <summary>
        /// 获取指定模块定义ID的所有模块ID。
        /// </summary>
        /// <param name="moduleDefId">模块定义ID。</param>
        /// <returns>模块ID的集合。</returns>
        public IEnumerable<int> GetModulesByModuleDefId(int moduleDefId)
        {
            // 使用LINQ查询，筛选具有相同模块定义ID的模块，并选择它们的ID
            return _items.Where(i => i.ModuleDefId == moduleDefId).Select(i => i.ModuleId);
        }

        /// <summary>
        /// 获取指定页面ID的所有模块。
        /// </summary>
        /// <param name="tabId">页面ID。</param>
        /// <returns>模块集合。</returns>
        public IEnumerable<IModuleItem> GetModulesByTab(int tabId)
        {
            // 使用LINQ查询，筛选具有相同页面ID的模块
            return _items.Where(i => i.TabId == tabId);
        }

        /// <summary>
        /// 更新模块排序。
        /// </summary>
        /// <param name="moduleId">模块ID。</param>
        /// <param name="moduleOrder">模块排序值。</param>
        /// <param name="pane">模块所在的窗格名。</param>
        public void UpdateModuleOrder(int moduleId, int moduleOrder, string pane)
        {
            // 查找指定ID的模块
            var moduleRow = _items.Single(i => i.ModuleId == moduleId);

            // 更新模块排序值和窗格名
            moduleRow.ModuleOrder = moduleOrder;
            moduleRow.PaneName = pane;

            // 提交更改到数据库
            _context.SaveChanges();

            // 重新加载所有模块到内存
            _items = _context.Modules.ToList();
        }

        /// <summary>
        /// 添加新模块。
        /// </summary>
        /// <param name="tabId">页面ID。</param>
        /// <param name="moduleOrder">模块排序值。</param>
        /// <param name="paneName">模块所在的窗格名。</param>
        /// <param name="title">模块标题。</param>
        /// <param name="moduleDefId">模块定义ID。</param>
        /// <param name="cacheTime">缓存超时时间。</param>
        /// <param name="editRoles">编辑角色。</param>
        /// <param name="showMobile">是否显示在移动端。</param>
        /// <returns>新模块的ID。</returns>
        public int AddModule(int tabId, int moduleOrder, string paneName, string title, int moduleDefId, int cacheTime,
                             string editRoles, bool showMobile)
        {
            // 创建新的模块对象
            var newModule = new ModuleItem
            {
                ModuleDefId = moduleDefId,
                ModuleOrder = moduleOrder,
                ModuleTitle = title,
                PaneName = paneName,
                EditRoles = editRoles,
                CacheTimeout = cacheTime,
                ShowMobile = showMobile,
                TabId = tabId
            };

            // 将新模块添加到上下文
            _context.Modules.Add(newModule);

            // 提交更改到数据库
            _context.SaveChanges();

            // 重新加载所有模块到内存
            _items = _context.Modules.ToList();

            // 返回新模块的ID
            return newModule.ModuleId;
        }

        /// <summary>
        /// 更新模块信息。
        /// </summary>
        /// <param name="moduleId">模块ID。</param>
        /// <param name="moduleOrder">模块排序值。</param>
        /// <param name="paneName">模块所在的窗格名。</param>
        /// <param name="title">模块标题。</param>
        /// <param name="cacheTime">缓存超时时间。</param>
        /// <param name="editRoles">编辑角色。</param>
        /// <param name="showMobile">是否显示在移动端。</param>
        public void UpdateModule(int moduleId, int moduleOrder, string paneName, string title, int cacheTime,
                                 string editRoles, bool showMobile)
        {
            // 查找指定ID的模块
            var moduleRow = _items.Single(i => i.ModuleId == moduleId);

            // 更新模块信息
            moduleRow.ModuleOrder = moduleOrder;
            moduleRow.ModuleTitle = title;
            moduleRow.PaneName = paneName;
            moduleRow.CacheTimeout = cacheTime;
            moduleRow.EditRoles = editRoles;
            moduleRow.ShowMobile = showMobile;

            // 提交更改到数据库
            _context.SaveChanges();

            // 重新加载所有模块到内存
            _items = _context.Modules.ToList();
        }

        /// <summary>
        /// 删除指定ID的模块。
        /// </summary>
        /// <param name="moduleId">模块ID。</param>
        public void DeleteModule(int moduleId)
        {
            // 删除与模块相关的信息
            _portalDb.DeleteModule(moduleId);

            // 查找并删除模块
            var moduleRow = _items.Single(i => i.ModuleId == moduleId);
            _context.Modules.Remove(moduleRow);

            // 提交更改到数据库
            _context.SaveChanges();

            // 重新加载所有模块到内存
            _items = _context.Modules.ToList();
        }

        /// <summary>
        /// 更新模块设置。
        /// </summary>
        /// <param name="moduleId">模块ID。</param>
        /// <param name="key">设置键。</param>
        /// <param name="val">设置值。</param>
        public void UpdateModuleSetting(int moduleId, string key, string val)
        {
            // 获取模块的所有设置
            var settings = GetModuleSettings(moduleId);

            // 检查设置是否存在
            if (settings.ContainsKey(key))
            {
                // 更新已存在的设置值
                var setting = _items.Single(i => i.ModuleId == moduleId).Settings.Single(s => s.SettingName == key);
                setting.SettingText = val;
            }
            else
            {
                // 创建新的设置并添加到数据库
                var setting = new ModuleSettingItem
                {
                    ModuleId = moduleId,
                    SettingName = key,
                    SettingText = val
                };
                _context.Settings.Add(setting);
            }

            // 提交更改到数据库
            _context.SaveChanges();

            // 重新加载所有模块到内存
            _items = _context.Modules.ToList();
        }

        /// <summary>
        /// 中文：获取指定已存在模块的全部设置。
        ///
        /// English: Gets all settings for a specified existing module.
        /// </summary>
        /// <param name="moduleId">中文：模块实例标识。English: Module-instance identifier.</param>
        /// <returns>中文：设置哈希表；缺少单个键由调用模块处理默认值。English: Settings hashtable; consuming modules handle defaults for missing individual keys.</returns>
        /// <remarks>
        /// 中文：模块缺失仍为严格配置错误；此方法不把不存在模块伪装成空设置集合。
        /// English: A missing module remains a strict configuration error; this method does not disguise it as an empty settings collection.
        /// </remarks>
        public Hashtable GetModuleSettings(int moduleId)
        {
            // 从数据库中获取模块设置
            var settings = _items.Single(i => i.ModuleId == moduleId).Settings.Select(i => new { i.SettingName, i.SettingText });

            // 将设置转换为哈希表
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
