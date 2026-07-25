using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>旧门户 Tab 配置表的 EF 投影实体。</zh-CN>
    ///   <en>Entity Framework projection entity for the legacy portal tab configuration table.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该实体映射 `PortalCfg_Tabs`，保留旧门户的排序、角色字符串和移动端字段。访问授权仍由 `PortalSecurity`、`PortalSettings` 和页面装载流程统一解释。</zh-CN>
    ///   <en>This entity maps to `PortalCfg_Tabs` and preserves the legacy portal ordering, role-string and mobile fields. Access authorization is still interpreted by `PortalSecurity`, `PortalSettings` and page loading flow.</en>
    /// </lang>
    /// </remarks>
    [Table("PortalCfg_Tabs")]
    public class TabItem : ITabItem
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>当前 Tab 下挂载的模块实例集合。</zh-CN>
        ///   <en>Module instances mounted under the current tab.</en>
        /// </lang>
        /// </summary>
        public ICollection<ModuleItem> Modules { get; set; }

        #region ITabItem Members

        /// <summary>
        /// <lang>
        ///   <zh-CN>Tab 在桌面门户导航中的排序值。</zh-CN>
        ///   <en>Sort value of the tab in desktop portal navigation.</en>
        /// </lang>
        /// </summary>
        public int? TabOrder { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>Tab 显示名称。</zh-CN>
        ///   <en>Display name of the tab.</en>
        /// </lang>
        /// </summary>
        public string TabName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>Tab 配置主键。</zh-CN>
        ///   <en>Primary key of the tab configuration row.</en>
        /// </lang>
        /// </summary>
        [Key]
        public int TabId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>旧门户分号分隔角色字符串，需通过统一角色解析工具解释。</zh-CN>
        ///   <en>Legacy semicolon-delimited role string, which must be interpreted through the shared role parser.</en>
        /// </lang>
        /// </summary>
        public string AccessRoles { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>旧移动端 Tab 名称字段，作为历史兼容数据保留。</zh-CN>
        ///   <en>Legacy mobile tab-name field retained as historical compatibility data.</en>
        /// </lang>
        /// </summary>
        public string MobileTabName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>旧移动端可见性标记，当前主题体系不直接依赖该字段。</zh-CN>
        ///   <en>Legacy mobile visibility flag; the current theme system does not directly depend on it.</en>
        /// </lang>
        /// </summary>
        public bool? ShowMobile { get; set; }

        #endregion
    }
}
