using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>旧门户模块类型定义的 Entity Framework 投影。</zh-CN>
    ///     <en>Entity Framework projection for a legacy portal module type definition.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该类型映射 <c>PortalCfg_ModuleDefinitions</c>。P3 之后新模块定义应来自受信任部署包；
    ///       此实体保留旧定义表读取和 Legacy 管理入口兼容，不负责扫描物理文件或安装模块包。
    ///     </zh-CN>
    ///     <en>
    ///       This type maps <c>PortalCfg_ModuleDefinitions</c>. After P3, new module definitions should come
    ///       from trusted deployed packages; this entity preserves legacy definition-table access and the
    ///       Legacy administration entry, and does not scan physical files or install module packages.
    ///     </en>
    ///   </lang>
    /// </remarks>
    [Table("PortalCfg_ModuleDefinitions")]
    public class ModuleDefinitionItem : IModuleDefinitionItem
    {
        #region IModuleDefinitionItem Members

        /// <summary>
        ///   <l zh-CN="模块类型的后台显示名称。" en="Administration display name for the module type." />
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        ///   <l zh-CN="历史移动端控件路径，当前仅作为兼容字段保留。" en="Legacy mobile control path retained only as a compatibility field." />
        /// </summary>
        public string MobileSourceFile { get; set; }

        /// <summary>
        ///   <l zh-CN="桌面端模块控件路径，必须落在受信任部署边界内。" en="Desktop module control path, which must remain inside the trusted deployment boundary." />
        /// </summary>
        public string DesktopSourceFile { get; set; }

        /// <summary>
        ///   <l zh-CN="模块类型定义主键。" en="Primary key of the module type definition." />
        /// </summary>
        [Key]
        public int ModuleDefId { get; set; }

        #endregion
    }
}
