using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>模块实例键值设置的 Entity Framework 投影。</zh-CN>
    ///     <en>Entity Framework projection for module instance key-value settings.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该类型映射 <c>PortalCfg_ModuleSettings</c>。它保存模块实例私有配置，例如图片路径、
    ///       XML/XSL 路径或模块级文本设置；具体键名和值格式由对应模块解释。
    ///     </zh-CN>
    ///     <en>
    ///       This type maps <c>PortalCfg_ModuleSettings</c>. It stores private module instance settings such
    ///       as image paths, XML/XSL paths, or module-level text settings; each module interprets its own keys
    ///       and value formats.
    ///     </en>
    ///   </lang>
    /// </remarks>
    [Table("PortalCfg_ModuleSettings")]
    public class ModuleSettingItem
    {
        /// <summary>
        ///   <l>
        ///     <zh-CN>拥有该设置的模块实例标识。</zh-CN>
        ///     <en>Module instance identifier that owns this setting.</en>
        ///   </l>
        /// </summary>
        public int ModuleId { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>模块私有设置键名。</zh-CN>
        ///     <en>Private module setting key.</en>
        ///   </l>
        /// </summary>
        public string SettingName { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>模块私有设置文本值；调用模块负责解析、校验和输出安全处理。</zh-CN>
        ///     <en>Private module setting text value; the owning module parses, validates, and safely outputs it.</en>
        ///   </l>
        /// </summary>
        public string SettingText { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>模块设置行主键。</zh-CN>
        ///     <en>Primary key of the module setting row.</en>
        ///   </l>
        /// </summary>
        [Key]
        public int ModuleSettingId { get; set; }
    }
}
