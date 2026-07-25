namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>描述门户 Tab 页签基础字段的轻量契约。</zh-CN>
    ///   <en>Describes the lightweight base field contract for a portal tab.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该接口面向旧后台页签管理和数据读取，不负责解析角色串、加载页面模块或决定主题继承。</zh-CN>
    ///   <en>This interface targets legacy tab administration and data reads. It does not parse role strings, load page modules, or decide theme inheritance.</en>
    /// </lang>
    /// </remarks>
    public interface ITabItem
    {
        /// <summary>
        /// <l>
        ///   <zh-CN>页签在同级导航中的排序值；为空时表示保存流程可沿用默认顺序。</zh-CN>
        ///   <en>Sort value of the tab within its sibling navigation group; null means the save flow may keep its default order.</en>
        /// </l>
        /// </summary>
        int? TabOrder { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>桌面端导航中展示的页签名称。</zh-CN>
        ///   <en>Tab name displayed in desktop navigation.</en>
        /// </l>
        /// </summary>
        string TabName { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>页签主键，供页面路由、模块查询和后台维护使用。</zh-CN>
        ///   <en>Tab primary key used by page routing, module queries, and administration maintenance.</en>
        /// </l>
        /// </summary>
        int TabId { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>允许访问该页签的角色串，沿用旧门户分号分隔格式。</zh-CN>
        ///   <en>Role string allowed to access this tab, using the legacy portal semicolon-delimited format.</en>
        /// </l>
        /// </summary>
        string AccessRoles { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>旧移动端页签名，当前仅作为历史兼容字段保留。</zh-CN>
        ///   <en>Legacy mobile tab name, currently retained only as a backward-compatibility field.</en>
        /// </l>
        /// </summary>
        string MobileTabName { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>旧移动端展示开关，当前不参与新的响应式主题决策。</zh-CN>
        ///   <en>Legacy mobile-display flag, currently not involved in the newer responsive theme decisions.</en>
        /// </l>
        /// </summary>
        bool? ShowMobile { get; set; }
    }
}
