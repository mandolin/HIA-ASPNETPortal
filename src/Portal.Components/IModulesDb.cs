using System.Collections;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户模块实例及其模块级设置的数据访问契约。</zh-CN>
    ///   <en>Data-access contract for Portal module instances and module-level settings.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本契约区分可缺失的外部标识查找与严格配置读取。页面授权或请求验证可使用 <see cref="FindModuleById"/>；已验证的运行时装配、更新和删除使用严格方法，以便暴露配置或关系完整性问题。</zh-CN>
    ///   <en>This contract distinguishes nullable external-identifier lookup from strict configuration reads. Page authorization or request validation may use <see cref="FindModuleById"/>; verified runtime assembly, updates, and deletes use strict methods so configuration or relationship-integrity issues remain visible.</en>
    /// </lang>
    /// </remarks>
    public interface IModulesDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>读取引用指定模块定义的模块实例标识。</zh-CN>
        ///   <en>Reads module-instance identifiers that reference the specified module definition.</en>
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
        ///   <zh-CN>引用该模块定义的模块实例标识集合。</zh-CN>
        ///   <en>Module-instance identifiers that reference the module definition.</en>
        /// </l>
        /// </returns>
        IEnumerable<int> GetModulesByModuleDefId(int moduleDefId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定 Tab 中的模块实例。</zh-CN>
        ///   <en>Reads module instances in the specified Tab.</en>
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
        IEnumerable<IModuleItem> GetModulesByTab(int tabId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>严格读取模块实例；调用方必须已验证模块标识及其配置关系。</zh-CN>
        ///   <en>Strictly reads a module instance; callers must already validate the module identifier and its configuration relationship.</en>
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
        ///   <zh-CN>匹配的模块实例。</zh-CN>
        ///   <en>Matching module instance.</en>
        /// </l>
        /// </returns>
        IModuleItem GetSingleModule(int moduleId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>按标识查找模块实例；不存在时返回 <c>null</c>。</zh-CN>
        ///   <en>Finds a module instance by identifier, returning <c>null</c> when it does not exist.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>用于请求参数进入授权或拒绝逻辑的路径；重复记录仍是完整性错误，不应静默隐藏。</zh-CN>
        ///   <en>Intended for paths where a request parameter enters authorization or denial logic; duplicate records remain an integrity error and must not be silently hidden.</en>
        /// </lang>
        /// </remarks>
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
        IModuleItem FindModuleById(int moduleId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取模块级设置集合；模块必须存在，缺少单个设置键由使用模块决定默认行为。</zh-CN>
        ///   <en>Reads a module-level settings collection; the module must exist, while missing individual keys use defaults chosen by the consuming module.</en>
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
        ///   <zh-CN>旧 Web Forms 模块使用的设置哈希表。</zh-CN>
        ///   <en>Settings hashtable consumed by legacy Web Forms modules.</en>
        /// </l>
        /// </returns>
        Hashtable GetModuleSettings(int moduleId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新模块在其 Tab 内的顺序与窗格。</zh-CN>
        ///   <en>Updates a module's order and pane within its Tab.</en>
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
        void UpdateModuleOrder(int moduleId, int moduleOrder, string pane);

        /// <summary>
        /// <lang>
        ///   <zh-CN>向指定 Tab 添加模块实例。</zh-CN>
        ///   <en>Adds a module instance to the specified Tab.</en>
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
        ///   <zh-CN>窗格名称。</zh-CN>
        ///   <en>Pane name.</en>
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
        ///   <zh-CN>模块编辑角色字符串。</zh-CN>
        ///   <en>Module edit-role string.</en>
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
        int AddModule(int tabId, int moduleOrder, string paneName, string title, int moduleDefId, int cacheTime,
                      string editRoles, bool showMobile);

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新已验证模块实例的布局、标题、缓存和编辑角色。</zh-CN>
        ///   <en>Updates layout, title, cache, and edit roles for a verified module instance.</en>
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
        ///   <zh-CN>窗格名称。</zh-CN>
        ///   <en>Pane name.</en>
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
        ///   <zh-CN>模块编辑角色字符串。</zh-CN>
        ///   <en>Module edit-role string.</en>
        /// </l>
        /// </param>
        /// <param name="showMobile">
        /// <l>
        ///   <zh-CN>旧移动端显示标记。</zh-CN>
        ///   <en>Legacy mobile visibility flag.</en>
        /// </l>
        /// </param>
        void UpdateModule(int moduleId, int moduleOrder, string paneName, string title, int cacheTime, string editRoles,
                          bool showMobile);

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除已验证模块实例及其业务数据。</zh-CN>
        ///   <en>Deletes a verified module instance and its business data.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>模块实例标识。</zh-CN>
        ///   <en>Module-instance identifier.</en>
        /// </l>
        /// </param>
        void DeleteModule(int moduleId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>新增或更新已验证模块实例的一项设置。</zh-CN>
        ///   <en>Adds or updates one setting for a verified module instance.</en>
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
        void UpdateModuleSetting(int moduleId, string key, string val);
    }
}
