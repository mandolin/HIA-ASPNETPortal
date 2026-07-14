using System.Collections;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户模块实例及其模块级设置的数据访问契约。
    ///
    /// English: Data-access contract for Portal module instances and module-level settings.
    /// </summary>
    /// <remarks>
    /// 中文：本契约区分可缺失的外部标识查找与严格配置读取。页面授权或请求验证可使用
    /// <see cref="FindModuleById"/>；已验证的运行时装配、更新和删除使用严格方法，以便暴露配置或关系完整性问题。
    ///
    /// English: This contract distinguishes nullable external-identifier lookup from strict configuration reads.
    /// Page authorization or request validation may use <see cref="FindModuleById"/>; verified runtime assembly,
    /// updates, and deletes use strict methods so configuration or relationship-integrity issues remain visible.
    /// </remarks>
    public interface IModulesDb
    {
        /// <summary>中文：读取引用指定模块定义的模块实例标识。English: Reads module-instance identifiers that reference the specified module definition.</summary>
        IEnumerable<int> GetModulesByModuleDefId(int moduleDefId);

        /// <summary>中文：读取指定 Tab 中的模块实例。English: Reads module instances in the specified Tab.</summary>
        IEnumerable<IModuleItem> GetModulesByTab(int tabId);

        /// <summary>
        /// 中文：严格读取模块实例；调用方必须已验证模块标识及其配置关系。
        /// English: Strictly reads a module instance; callers must already validate the module identifier and its configuration relationship.
        /// </summary>
        IModuleItem GetSingleModule(int moduleId);

        /// <summary>
        /// 中文：按标识查找模块实例；不存在时返回 <c>null</c>。
        /// English: Finds a module instance by identifier, returning <c>null</c> when it does not exist.
        /// </summary>
        /// <remarks>
        /// 中文：用于请求参数进入授权或拒绝逻辑的路径；重复记录仍是完整性错误，不应静默隐藏。
        /// English: Intended for paths where a request parameter enters authorization or denial logic; duplicate records
        /// remain an integrity error and must not be silently hidden.
        /// </remarks>
        IModuleItem FindModuleById(int moduleId);

        /// <summary>
        /// 中文：读取模块级设置集合；模块必须存在，缺少单个设置键由使用模块决定默认行为。
        /// English: Reads a module-level settings collection; the module must exist, while missing individual keys use defaults chosen by the consuming module.
        /// </summary>
        Hashtable GetModuleSettings(int moduleId);

        /// <summary>中文：更新模块在其 Tab 内的顺序与窗格。English: Updates a module's order and pane within its Tab.</summary>
        void UpdateModuleOrder(int moduleId, int moduleOrder, string pane);

        /// <summary>中文：向指定 Tab 添加模块实例。English: Adds a module instance to the specified Tab.</summary>
        int AddModule(int tabId, int moduleOrder, string paneName, string title, int moduleDefId, int cacheTime,
                      string editRoles, bool showMobile);

        /// <summary>中文：更新已验证模块实例的布局、标题、缓存和编辑角色。English: Updates layout, title, cache, and edit roles for a verified module instance.</summary>
        void UpdateModule(int moduleId, int moduleOrder, string paneName, string title, int cacheTime, string editRoles,
                          bool showMobile);

        /// <summary>中文：删除已验证模块实例及其业务数据。English: Deletes a verified module instance and its business data.</summary>
        void DeleteModule(int moduleId);

        /// <summary>中文：新增或更新已验证模块实例的一项设置。English: Adds or updates one setting for a verified module instance.</summary>
        void UpdateModuleSetting(int moduleId, string key, string val);
    }
}
