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
        /// 获取单个模块。
        /// </summary>
        /// <param name="moduleId">模块ID。</param>
        /// <returns>模块对象。</returns>
        public IModuleItem GetSingleModule(int moduleId)
        {
            // 从内存列表中查找指定ID的模块
            return _items.Single(i => i.ModuleId == moduleId);
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
        /// 获取指定模块的所有设置。
        /// </summary>
        /// <param name="moduleId">模块ID。</param>
        /// <returns>设置的哈希表。</returns>
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