using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户桌面与移动兼容 Tab 的数据访问契约。
    ///
    /// English: Data-access contract for Portal desktop and mobile-compatibility Tabs.
    /// </summary>
    public interface ITabsDb
    {
        /// <summary>中文：读取全部桌面 Tab。English: Reads all desktop Tabs.</summary>
        IEnumerable<ITabItem> GetTabs();

        /// <summary>中文：读取标记为移动端显示的兼容 Tab。English: Reads compatibility Tabs marked for mobile display.</summary>
        IEnumerable<ITabItem> GetMobileTabs();

        /// <summary>
        /// 中文：严格读取 Tab；调用方必须已验证标识及其门户配置关系。
        /// English: Strictly reads a Tab; callers must already validate its identifier and Portal configuration relationship.
        /// </summary>
        ITabItem GetSingleTab(int tabId);

        /// <summary>
        /// 中文：按标识查找 Tab；不存在时返回 <c>null</c>。
        /// English: Finds a Tab by identifier, returning <c>null</c> when it does not exist.
        /// </summary>
        /// <remarks>
        /// 中文：用于模块编辑授权等外部请求标识路径；重复记录仍应作为完整性错误暴露。
        /// English: Intended for external request-identifier paths such as module-edit authorization; duplicate records
        /// must still surface as integrity errors.
        /// </remarks>
        ITabItem FindTabById(int tabId);

        /// <summary>中文：为门户创建 Tab。English: Creates a Tab for a Portal.</summary>
        int AddTab(int portalId, string tabName, int tabOrder);

        /// <summary>中文：更新已验证 Tab 的显示与访问设置。English: Updates display and access settings for a verified Tab.</summary>
        void UpdateTab(int portalId, int tabId, string tabName, int tabOrder, string authorizedRoles,
                       string mobileTabName, bool showMobile);

        /// <summary>中文：更新已验证 Tab 的显示顺序。English: Updates display order for a verified Tab.</summary>
        void UpdateTabOrder(int tabId, int tabOrder);

        /// <summary>中文：删除已验证 Tab 及其关联模块。English: Deletes a verified Tab and its associated modules.</summary>
        void DeleteTab(int tabId);
    }
}
