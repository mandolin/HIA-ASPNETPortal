using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>门户模块实例的 Entity Framework 投影。</zh-CN>
    ///     <en>Entity Framework projection for a portal module instance.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该类型映射 <c>PortalCfg_Modules</c>，承接旧门户中“Tab 页面上的模块实例”概念。
    ///       模块定义、页面位置、编辑角色和缓存设置都在这里形成运行期快照；路径可信性仍由模块定义目录和后台维护页校验。
    ///     </zh-CN>
    ///     <en>
    ///       This type maps <c>PortalCfg_Modules</c> and represents the legacy portal concept of a module
    ///       instance placed on a tab. Module definition, pane placement, edit roles, and cache settings are
    ///       captured here as runtime state; trusted path validation remains owned by the module definition
    ///       catalog and administration pages.
    ///     </en>
    ///   </lang>
    /// </remarks>
    [Table("PortalCfg_Modules")]
    public class ModuleItem : IModuleItem
    {
        /// <summary>
        ///   <l>
        ///     <zh-CN>模块实例附加设置集合，由 EF 导航属性加载。</zh-CN>
        ///     <en>Additional module settings loaded through the EF navigation property.</en>
        ///   </l>
        /// </summary>
        public virtual ICollection<ModuleSettingItem> Settings { get; set; }

        #region IModuleItem Members

        /// <summary>
        ///   <l>
        ///     <zh-CN>同一 Pane 内的显示顺序；为空时由调用层决定默认排序。</zh-CN>
        ///     <en>Display order within the pane; callers choose the fallback order when null.</en>
        ///   </l>
        /// </summary>
        public int? ModuleOrder { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>模块实例标题，展示层输出前必须编码。</zh-CN>
        ///     <en>Module instance title; presentation code must encode it before output.</en>
        ///   </l>
        /// </summary>
        public string ModuleTitle { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>旧门户布局 Pane 名称，如左栏、内容栏或右栏。</zh-CN>
        ///     <en>Legacy portal pane name, such as left, content, or right pane.</en>
        ///   </l>
        /// </summary>
        public string PaneName { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>模块实例数据库主键。</zh-CN>
        ///     <en>Database primary key for the module instance.</en>
        ///   </l>
        /// </summary>
        [Key]
        public int ModuleId { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>关联的模块定义标识；为空代表历史异常或未完整配置的数据。</zh-CN>
        ///     <en>Associated module definition identifier; null indicates legacy or incomplete configuration.</en>
        ///   </l>
        /// </summary>
        public int? ModuleDefId { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>分号分隔的模块编辑角色字符串。</zh-CN>
        ///     <en>Semicolon-delimited role string allowed to edit this module.</en>
        ///   </l>
        /// </summary>
        public string EditRoles { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>模块输出缓存秒数；为空时沿用调用层或旧门户默认策略。</zh-CN>
        ///     <en>Module output cache duration in seconds; null keeps caller or legacy defaults.</en>
        ///   </l>
        /// </summary>
        public int? CacheTimeout { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>历史移动端显示标记；当前移动端方案不以它作为主要机制。</zh-CN>
        ///     <en>Legacy mobile visibility flag; the current mobile strategy does not use it as the primary mechanism.</en>
        ///   </l>
        /// </summary>
        public bool? ShowMobile { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>模块所属 Tab 标识。</zh-CN>
        ///     <en>Identifier of the tab that owns this module instance.</en>
        ///   </l>
        /// </summary>
        public int? TabId { get; set; }

        #endregion
    }
}
