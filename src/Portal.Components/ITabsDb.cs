using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户桌面与移动兼容 Tab 的数据访问契约。</zh-CN>
    ///   <en>Data-access contract for Portal desktop and mobile-compatibility Tabs.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>此接口仍承载旧门户 Tab 模型，包括分号角色串、移动端兼容字段和模块级布局关系。调用方在写入前应先完成后台授权、Tab 归属和历史字段兼容判断。</zh-CN>
    ///   <en>This interface still carries the legacy portal Tab model, including semicolon role strings, mobile-compatibility fields, and module-layout relationships. Callers should complete administration authorization, Tab ownership validation, and historical-field compatibility checks before writes.</en>
    /// </lang>
    /// </remarks>
    public interface ITabsDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>读取全部桌面 Tab。</zh-CN>
        ///   <en>Reads all desktop Tabs.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>按门户显示顺序返回的桌面 Tab 集合。</zh-CN>
        ///   <en>Desktop Tabs returned in portal display order.</en>
        /// </l>
        /// </returns>
        IEnumerable<ITabItem> GetTabs();

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取标记为移动端显示的兼容 Tab。</zh-CN>
        ///   <en>Reads compatibility Tabs marked for mobile display.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>标记为移动兼容显示的 Tab 集合。</zh-CN>
        ///   <en>Tabs marked for mobile-compatibility display.</en>
        /// </l>
        /// </returns>
        IEnumerable<ITabItem> GetMobileTabs();

        /// <summary>
        /// <lang>
        ///   <zh-CN>严格读取 Tab；调用方必须已验证标识及其门户配置关系。</zh-CN>
        ///   <en>Strictly reads a Tab; callers must already validate its identifier and Portal configuration relationship.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabId">
        /// <l>
        ///   <zh-CN>Tab 标识。</zh-CN>
        ///   <en>Tab identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配的 Tab；不存在或数据不完整时由实现按完整性错误处理。</zh-CN>
        ///   <en>The matching Tab; implementations handle missing or inconsistent data as integrity errors.</en>
        /// </l>
        /// </returns>
        ITabItem GetSingleTab(int tabId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>按标识查找 Tab；不存在时返回 <c>null</c>。</zh-CN>
        ///   <en>Finds a Tab by identifier, returning <c>null</c> when it does not exist.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>用于模块编辑授权等外部请求标识路径；重复记录仍应作为完整性错误暴露。</zh-CN>
        ///   <en>Intended for external request-identifier paths such as module-edit authorization; duplicate records must still surface as integrity errors.</en>
        /// </lang>
        /// </remarks>
        /// <param name="tabId">
        /// <l>
        ///   <zh-CN>Tab 标识。</zh-CN>
        ///   <en>Tab identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配的 Tab；不存在时为 <c>null</c>。</zh-CN>
        ///   <en>The matching Tab, or <c>null</c> when it does not exist.</en>
        /// </l>
        /// </returns>
        ITabItem FindTabById(int tabId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>为门户创建 Tab。</zh-CN>
        ///   <en>Creates a Tab for a Portal.</en>
        /// </lang>
        /// </summary>
        /// <param name="portalId">
        /// <l>
        ///   <zh-CN>所属门户标识。</zh-CN>
        ///   <en>Owning Portal identifier.</en>
        /// </l>
        /// </param>
        /// <param name="tabName">
        /// <l>
        ///   <zh-CN>桌面 Tab 显示名。</zh-CN>
        ///   <en>Desktop Tab display name.</en>
        /// </l>
        /// </param>
        /// <param name="tabOrder">
        /// <l>
        ///   <zh-CN>显示顺序。</zh-CN>
        ///   <en>Display order.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>新建 Tab 标识。</zh-CN>
        ///   <en>New Tab identifier.</en>
        /// </l>
        /// </returns>
        int AddTab(int portalId, string tabName, int tabOrder);

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新已验证 Tab 的显示与访问设置。</zh-CN>
        ///   <en>Updates display and access settings for a verified Tab.</en>
        /// </lang>
        /// </summary>
        /// <param name="portalId">
        /// <l>
        ///   <zh-CN>所属门户标识。</zh-CN>
        ///   <en>Owning Portal identifier.</en>
        /// </l>
        /// </param>
        /// <param name="tabId">
        /// <l>
        ///   <zh-CN>待更新 Tab 标识。</zh-CN>
        ///   <en>Tab identifier to update.</en>
        /// </l>
        /// </param>
        /// <param name="tabName">
        /// <l>
        ///   <zh-CN>桌面 Tab 显示名。</zh-CN>
        ///   <en>Desktop Tab display name.</en>
        /// </l>
        /// </param>
        /// <param name="tabOrder">
        /// <l>
        ///   <zh-CN>显示顺序。</zh-CN>
        ///   <en>Display order.</en>
        /// </l>
        /// </param>
        /// <param name="authorizedRoles">
        /// <l>
        ///   <zh-CN>允许访问的旧门户分号角色串。</zh-CN>
        ///   <en>Legacy semicolon-delimited role string allowed to access the Tab.</en>
        /// </l>
        /// </param>
        /// <param name="mobileTabName">
        /// <l>
        ///   <zh-CN>移动兼容显示名。</zh-CN>
        ///   <en>Mobile-compatibility display name.</en>
        /// </l>
        /// </param>
        /// <param name="showMobile">
        /// <l>
        ///   <zh-CN>是否在移动兼容列表中显示。</zh-CN>
        ///   <en>Whether the Tab should appear in the mobile-compatibility list.</en>
        /// </l>
        /// </param>
        void UpdateTab(int portalId, int tabId, string tabName, int tabOrder, string authorizedRoles,
                       string mobileTabName, bool showMobile);

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新已验证 Tab 的显示顺序。</zh-CN>
        ///   <en>Updates display order for a verified Tab.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabId">
        /// <l>
        ///   <zh-CN>待更新 Tab 标识。</zh-CN>
        ///   <en>Tab identifier to update.</en>
        /// </l>
        /// </param>
        /// <param name="tabOrder">
        /// <l>
        ///   <zh-CN>新的显示顺序。</zh-CN>
        ///   <en>New display order.</en>
        /// </l>
        /// </param>
        void UpdateTabOrder(int tabId, int tabOrder);

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除已验证 Tab 及其关联模块。</zh-CN>
        ///   <en>Deletes a verified Tab and its associated modules.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabId">
        /// <l>
        ///   <zh-CN>待删除 Tab 标识。</zh-CN>
        ///   <en>Tab identifier to delete.</en>
        /// </l>
        /// </param>
        void DeleteTab(int tabId);
    }
}
