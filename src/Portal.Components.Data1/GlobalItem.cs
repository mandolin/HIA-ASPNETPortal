using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>门户全局配置的 Entity Framework 投影。</zh-CN>
    ///     <en>Entity Framework projection for portal global configuration.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该类型映射旧表 <c>PortalCfg_Globals</c>。它只承载门户名称和旧编辑按钮显示开关，
    ///       不负责读取在线系统设置 registry，也不参与主题、权限或运行期安全策略解析。
    ///     </zh-CN>
    ///     <en>
    ///       This type maps the legacy <c>PortalCfg_Globals</c> table. It only carries the portal name and
    ///       legacy edit-button visibility flag, and does not read the online settings registry or participate
    ///       in theme, authorization, or runtime security policy resolution.
    ///     </en>
    ///   </lang>
    /// </remarks>
    [Table("PortalCfg_Globals")]
    public class GlobalItem : IGlobalItem
    {
        #region IGlobalItem Members

        /// <summary>
        ///   <l>
        ///     <zh-CN>门户全局配置主键；旧项目通常只有一个门户记录。</zh-CN>
        ///     <en>Primary key for portal global configuration; the legacy project usually has one portal row.</en>
        ///   </l>
        /// </summary>
        [Key]
        public int PortalId { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>门户显示名称，顶栏和旧站点设置页会读取该值。</zh-CN>
        ///     <en>Portal display name read by the banner and legacy site settings page.</en>
        ///   </l>
        /// </summary>
        public string PortalName { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>旧门户是否始终显示模块编辑按钮的兼容开关。</zh-CN>
        ///     <en>Legacy compatibility flag that controls whether module edit buttons are always shown.</en>
        ///   </l>
        /// </summary>
        public bool? AlwaysShowEditButton { get; set; }

        #endregion
    }
}
