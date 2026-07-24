using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>基于配置数据库上下文的门户模块实例数据访问实现。</zh-CN>
    ///   <en>Portal module-instance data-access implementation backed by the configuration database context.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该实现维持旧门户的内存快照模式：写入后重新加载模块集合，读取路径使用快照查询。调用方仍负责控制管理员权限、受信任模块定义边界和页面模块排序策略。</zh-CN>
    ///   <en>This implementation keeps the legacy Portal in-memory snapshot pattern: writes reload the module collection and reads query the snapshot. Callers remain responsible for administrator authorization, trusted module-definition boundaries, and page module ordering policy.</en>
    /// </lang>
    /// </remarks>
    public class ModulesDb : IModulesDb
    {
        private readonly PortalCfgDbContext _context;
        private readonly IPortalDb _portalDb;
        private List<ModuleItem> _items;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化模块数据访问对象并加载当前模块快照。</zh-CN>
        ///   <en>Initializes the module data-access object and loads the current module snapshot.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>配置数据库上下文。</zh-CN>
        ///   <en>Configuration database context.</en>
        /// </l>
        /// </param>
        /// <param name="portalDb">
        /// <l>
        ///   <zh-CN>旧门户模块级联删除依赖。</zh-CN>
        ///   <en>Legacy Portal dependency used for cascading module cleanup.</en>
        /// </l>
        /// </param>
        public ModulesDb(PortalCfgDbContext context, IPortalDb portalDb)
        {
            _context = context;
            _portalDb = portalDb;

            // <lang>
            //   <zh-CN>保留旧门户的快照读取模型，避免每次模块渲染都重新访问数据库。</zh-CN>
            //   <en>Keep the legacy Portal snapshot read model so each module render does not re-query the database.</en>
            // </lang>
            _items = _context.Modules.ToList();
        }

        #region IModulesDb Members

        /// <summary>
        /// <lang>
        ///   <zh-CN>严格获取单个模块实例。</zh-CN>
        ///   <en>Strictly gets one module instance.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>模块实例标识。</zh-CN>
        ///   <en>Module-instance identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配模块实例。</zh-CN>
        ///   <en>Matching module instance.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>此方法用于已验证的运行时配置和写入路径；缺失或重复记录应暴露为完整性故障。</zh-CN>
        ///   <en>This method serves verified runtime configuration and write paths; missing or duplicate records should surface as integrity failures.</en>
        /// </lang>
        /// </remarks>
        public IModuleItem GetSingleModule(int moduleId)
        {
            // <lang>
            //   <zh-CN>这里故意使用 Single；重复记录和缺失记录都应被视为配置完整性错误。</zh-CN>
            //   <en>This intentionally uses Single; duplicate and missing records should both surface as configuration-integrity errors.</en>
            // </lang>
            return _items.Single(i => i.ModuleId == moduleId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按标识查找模块实例；不存在时返回 <c>null</c>。</zh-CN>
        ///   <en>Finds a module instance by identifier, returning <c>null</c> when it is absent.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>模块实例标识。</zh-CN>
        ///   <en>Module-instance identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配模块实例；不存在时为 <c>null</c>。</zh-CN>
        ///   <en>Matching module instance, or <c>null</c> when absent.</en>
        /// </l>
        /// </returns>
        public IModuleItem FindModuleById(int moduleId)
        {
            // <lang>
            //   <zh-CN>SingleOrDefault 保留重复记录的完整性异常，同时允许授权层对缺失模块安全拒绝。</zh-CN>
            //   <en>SingleOrDefault preserves duplicate-record integrity errors while allowing authorization to deny a missing module safely.</en>
            // </lang>
            return _items.SingleOrDefault(i => i.ModuleId == moduleId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取引用指定模块定义的所有模块实例标识。</zh-CN>
        ///   <en>Gets all module-instance identifiers that reference the specified module definition.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleDefId">
        /// <l>
        ///   <zh-CN>模块定义标识。</zh-CN>
        ///   <en>Module-definition identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>模块实例标识集合。</zh-CN>
        ///   <en>Collection of module-instance identifiers.</en>
        /// </l>
        /// </returns>
        public IEnumerable<int> GetModulesByModuleDefId(int moduleDefId)
        {
            // <lang>
            //   <zh-CN>模块定义删除前会调用该查询判断是否仍有实例引用。</zh-CN>
            //   <en>This query is used before deleting module definitions to detect remaining instance references.</en>
            // </lang>
            return _items.Where(i => i.ModuleDefId == moduleDefId).Select(i => i.ModuleId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取指定 Tab 页面上的所有模块实例。</zh-CN>
        ///   <en>Gets all module instances placed on the specified Tab page.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabId">
        /// <l>
        ///   <zh-CN>Tab 页面标识。</zh-CN>
        ///   <en>Tab page identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>属于该 Tab 的模块实例集合。</zh-CN>
        ///   <en>Module instances that belong to the Tab.</en>
        /// </l>
        /// </returns>
        public IEnumerable<IModuleItem> GetModulesByTab(int tabId)
        {
            // <lang>
            //   <zh-CN>排序和 Pane 分组由上层布局渲染逻辑处理，此处只按 Tab 归集。</zh-CN>
            //   <en>Ordering and pane grouping are handled by the higher-level layout renderer; this method only groups by Tab.</en>
            // </lang>
            return _items.Where(i => i.TabId == tabId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新模块在 Tab 布局中的排序值和窗格名称。</zh-CN>
        ///   <en>Updates a module's ordering value and pane name in the Tab layout.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>模块实例标识。</zh-CN>
        ///   <en>Module-instance identifier.</en>
        /// </l>
        /// </param>
        /// <param name="moduleOrder">
        /// <l>
        ///   <zh-CN>新的模块排序值。</zh-CN>
        ///   <en>New module ordering value.</en>
        /// </l>
        /// </param>
        /// <param name="pane">
        /// <l>
        ///   <zh-CN>新的窗格名称。</zh-CN>
        ///   <en>New pane name.</en>
        /// </l>
        /// </param>
        public void UpdateModuleOrder(int moduleId, int moduleOrder, string pane)
        {
            // <lang>
            //   <zh-CN>更新前要求模块已存在；缺失模块表示后台布局状态已损坏。</zh-CN>
            //   <en>The module must already exist before updating; a missing module means the administration layout state is corrupted.</en>
            // </lang>
            var moduleRow = _items.Single(i => i.ModuleId == moduleId);

            moduleRow.ModuleOrder = moduleOrder;
            moduleRow.PaneName = pane;

            _context.SaveChanges();

            // <lang>
            //   <zh-CN>写入后刷新快照，避免后续同一请求链读取到旧排序。</zh-CN>
            //   <en>Refresh the snapshot after the write so later reads in the same request chain do not see stale ordering.</en>
            // </lang>
            _items = _context.Modules.ToList();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>向指定 Tab 添加一个新的模块实例。</zh-CN>
        ///   <en>Adds a new module instance to the specified Tab.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabId">
        /// <l>
        ///   <zh-CN>目标 Tab 页面标识。</zh-CN>
        ///   <en>Target Tab page identifier.</en>
        /// </l>
        /// </param>
        /// <param name="moduleOrder">
        /// <l>
        ///   <zh-CN>模块排序值。</zh-CN>
        ///   <en>Module ordering value.</en>
        /// </l>
        /// </param>
        /// <param name="paneName">
        /// <l>
        ///   <zh-CN>模块所在窗格名称。</zh-CN>
        ///   <en>Name of the pane that hosts the module.</en>
        /// </l>
        /// </param>
        /// <param name="title">
        /// <l>
        ///   <zh-CN>模块标题。</zh-CN>
        ///   <en>Module title.</en>
        /// </l>
        /// </param>
        /// <param name="moduleDefId">
        /// <l>
        ///   <zh-CN>受信任模块定义标识。</zh-CN>
        ///   <en>Trusted module-definition identifier.</en>
        /// </l>
        /// </param>
        /// <param name="cacheTime">
        /// <l>
        ///   <zh-CN>模块缓存超时秒数。</zh-CN>
        ///   <en>Module cache timeout in seconds.</en>
        /// </l>
        /// </param>
        /// <param name="editRoles">
        /// <l>
        ///   <zh-CN>允许编辑该模块的分号角色字符串。</zh-CN>
        ///   <en>Semicolon role string allowed to edit the module.</en>
        /// </l>
        /// </param>
        /// <param name="showMobile">
        /// <l>
        ///   <zh-CN>旧移动端显示标记。</zh-CN>
        ///   <en>Legacy mobile visibility flag.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>新模块实例标识。</zh-CN>
        ///   <en>New module-instance identifier.</en>
        /// </l>
        /// </returns>
        public int AddModule(int tabId, int moduleOrder, string paneName, string title, int moduleDefId, int cacheTime,
                             string editRoles, bool showMobile)
        {
            // <lang>
            //   <zh-CN>仅创建实例记录；模块定义的来源可信性应已由上层 Module Catalog 或旧后台入口保证。</zh-CN>
            //   <en>This creates only the instance row; trust in the module definition must already be guaranteed by Module Catalog or the legacy administration entry.</en>
            // </lang>
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

            _context.Modules.Add(newModule);
            _context.SaveChanges();

            // <lang>
            //   <zh-CN>保存后刷新快照，并返回数据库生成的模块标识供后台继续编辑或跳转。</zh-CN>
            //   <en>After saving, refresh the snapshot and return the database-generated module identifier for continued administration or navigation.</en>
            // </lang>
            _items = _context.Modules.ToList();
            return newModule.ModuleId;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新一个既有模块实例的布局、标题、缓存和编辑授权信息。</zh-CN>
        ///   <en>Updates layout, title, cache, and edit-authorization metadata for an existing module instance.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>模块实例标识。</zh-CN>
        ///   <en>Module-instance identifier.</en>
        /// </l>
        /// </param>
        /// <param name="moduleOrder">
        /// <l>
        ///   <zh-CN>模块排序值。</zh-CN>
        ///   <en>Module ordering value.</en>
        /// </l>
        /// </param>
        /// <param name="paneName">
        /// <l>
        ///   <zh-CN>模块所在窗格名称。</zh-CN>
        ///   <en>Name of the pane that hosts the module.</en>
        /// </l>
        /// </param>
        /// <param name="title">
        /// <l>
        ///   <zh-CN>模块标题。</zh-CN>
        ///   <en>Module title.</en>
        /// </l>
        /// </param>
        /// <param name="cacheTime">
        /// <l>
        ///   <zh-CN>模块缓存超时秒数。</zh-CN>
        ///   <en>Module cache timeout in seconds.</en>
        /// </l>
        /// </param>
        /// <param name="editRoles">
        /// <l>
        ///   <zh-CN>允许编辑该模块的分号角色字符串。</zh-CN>
        ///   <en>Semicolon role string allowed to edit the module.</en>
        /// </l>
        /// </param>
        /// <param name="showMobile">
        /// <l>
        ///   <zh-CN>旧移动端显示标记。</zh-CN>
        ///   <en>Legacy mobile visibility flag.</en>
        /// </l>
        /// </param>
        public void UpdateModule(int moduleId, int moduleOrder, string paneName, string title, int cacheTime,
                                 string editRoles, bool showMobile)
        {
            // <lang>
            //   <zh-CN>更新路径保持严格查找；管理员页面不应悄悄忽略不存在的模块实例。</zh-CN>
            //   <en>The update path keeps strict lookup; administration pages should not silently ignore missing module instances.</en>
            // </lang>
            var moduleRow = _items.Single(i => i.ModuleId == moduleId);

            moduleRow.ModuleOrder = moduleOrder;
            moduleRow.ModuleTitle = title;
            moduleRow.PaneName = paneName;
            moduleRow.CacheTimeout = cacheTime;
            moduleRow.EditRoles = editRoles;
            moduleRow.ShowMobile = showMobile;

            _context.SaveChanges();

            // <lang>
            //   <zh-CN>刷新快照以同步后续模块渲染和角色引用复核。</zh-CN>
            //   <en>Refresh the snapshot to align later module rendering and role-reference checks.</en>
            // </lang>
            _items = _context.Modules.ToList();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除指定模块实例及其旧门户级联数据。</zh-CN>
        ///   <en>Deletes the specified module instance and its legacy Portal cascading data.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>模块实例标识。</zh-CN>
        ///   <en>Module-instance identifier.</en>
        /// </l>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>调用方必须在进入此方法前确认业务层允许删除实例；本方法只执行旧模块设置、权限等附属数据清理和模块行删除。</zh-CN>
        ///   <en>Callers must verify business-level deletion permission before entering this method; it only performs cleanup of legacy module settings, permissions, and the module row.</en>
        /// </lang>
        /// </remarks>
        public void DeleteModule(int moduleId)
        {
            // <lang>
            //   <zh-CN>先删除旧门户附属数据，避免模块行删除后遗留孤儿设置或权限记录。</zh-CN>
            //   <en>Delete legacy Portal auxiliary data first to avoid orphaned settings or permission rows after the module row is removed.</en>
            // </lang>
            _portalDb.DeleteModule(moduleId);

            var moduleRow = _items.Single(i => i.ModuleId == moduleId);
            _context.Modules.Remove(moduleRow);

            _context.SaveChanges();

            // <lang>
            //   <zh-CN>删除后刷新快照，保证同一请求链不再看到已删除模块。</zh-CN>
            //   <en>Refresh the snapshot after deletion so the same request chain no longer sees the removed module.</en>
            // </lang>
            _items = _context.Modules.ToList();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>新增或更新指定模块的单个设置项。</zh-CN>
        ///   <en>Adds or updates a single setting for the specified module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>模块实例标识。</zh-CN>
        ///   <en>Module-instance identifier.</en>
        /// </l>
        /// </param>
        /// <param name="key">
        /// <l>
        ///   <zh-CN>设置键。</zh-CN>
        ///   <en>Setting key.</en>
        /// </l>
        /// </param>
        /// <param name="val">
        /// <l>
        ///   <zh-CN>设置值。</zh-CN>
        ///   <en>Setting value.</en>
        /// </l>
        /// </param>
        public void UpdateModuleSetting(int moduleId, string key, string val)
        {
            // <lang>
            //   <zh-CN>先通过严格模块查找取得现有设置；模块不存在时应暴露为配置错误。</zh-CN>
            //   <en>Resolve existing settings through strict module lookup first; a missing module should surface as a configuration error.</en>
            // </lang>
            var settings = GetModuleSettings(moduleId);

            if (settings.ContainsKey(key))
            {
                // <lang>
                //   <zh-CN>已有键只更新值，保留原有设置行和模块关联。</zh-CN>
                //   <en>For an existing key, update only the value while preserving the setting row and module association.</en>
                // </lang>
                var setting = _items.Single(i => i.ModuleId == moduleId).Settings.Single(s => s.SettingName == key);
                setting.SettingText = val;
            }
            else
            {
                // <lang>
                //   <zh-CN>缺失键按旧门户约定创建新设置行。</zh-CN>
                //   <en>A missing key creates a new setting row following the legacy Portal convention.</en>
                // </lang>
                var setting = new ModuleSettingItem
                {
                    ModuleId = moduleId,
                    SettingName = key,
                    SettingText = val
                };
                _context.Settings.Add(setting);
            }

            _context.SaveChanges();

            // <lang>
            //   <zh-CN>设置写入后刷新快照，保证模块初始化读取到最新配置。</zh-CN>
            //   <en>Refresh the snapshot after saving settings so module initialization reads the latest configuration.</en>
            // </lang>
            _items = _context.Modules.ToList();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取指定已存在模块的全部设置。</zh-CN>
        ///   <en>Gets all settings for a specified existing module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>模块实例标识。</zh-CN>
        ///   <en>Module-instance identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>设置哈希表；缺少单个键由调用模块处理默认值。</zh-CN>
        ///   <en>Settings hashtable; consuming modules handle defaults for missing individual keys.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>模块缺失仍为严格配置错误；此方法不把不存在模块伪装成空设置集合。</zh-CN>
        ///   <en>A missing module remains a strict configuration error; this method does not disguise it as an empty settings collection.</en>
        /// </lang>
        /// </remarks>
        public Hashtable GetModuleSettings(int moduleId)
        {
            // <lang>
            //   <zh-CN>从模块快照导航属性读取设置，保持与其他模块读取路径一致。</zh-CN>
            //   <en>Read settings from the module snapshot navigation property to stay consistent with other module read paths.</en>
            // </lang>
            var settings = _items.Single(i => i.ModuleId == moduleId).Settings.Select(i => new { i.SettingName, i.SettingText });

            // <lang>
            //   <zh-CN>返回旧 Web Forms 模块期望的 Hashtable 结构，避免破坏既有模块契约。</zh-CN>
            //   <en>Return the Hashtable shape expected by legacy Web Forms modules to avoid breaking existing module contracts.</en>
            // </lang>
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
