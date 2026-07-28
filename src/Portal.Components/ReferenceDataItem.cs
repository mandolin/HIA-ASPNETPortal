namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>受治理业务参考数据目录的一条可读记录。</zh-CN>
    ///   <en>One readable record in the governed business reference-data catalog.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>事实记录应保存 <see cref="ValueKey"/>，而不是可被运营调整的 <see cref="DisplayName"/>。此投影不包含目录写入能力或任何敏感业务内容。</zh-CN>
    ///   <en>Fact records must persist <see cref="ValueKey"/>, not the operationally editable <see cref="DisplayName"/>. This projection exposes neither catalog-write capability nor sensitive business content.</en>
    /// </lang>
    /// </remarks>
    public sealed class ReferenceDataItem
    {
        /// <summary><lang><zh-CN>目录技术标识。</zh-CN><en>Catalog technical identifier.</en></lang></summary>
        public long ReferenceDataId { get; set; }

        /// <summary><lang><zh-CN>稳定参考集合键。</zh-CN><en>Stable reference-set key.</en></lang></summary>
        public string ReferenceSetKey { get; set; }

        /// <summary><lang><zh-CN>集合内稳定值键。</zh-CN><en>Stable value key within the set.</en></lang></summary>
        public string ValueKey { get; set; }

        /// <summary><lang><zh-CN>面向用户的显示名。</zh-CN><en>User-facing display name.</en></lang></summary>
        public string DisplayName { get; set; }

        /// <summary><lang><zh-CN>低敏用途说明。</zh-CN><en>Low-sensitivity usage description.</en></lang></summary>
        public string Description { get; set; }

        /// <summary><lang><zh-CN>稳定展示顺序。</zh-CN><en>Stable display order.</en></lang></summary>
        public int SortOrder { get; set; }

        /// <summary><lang><zh-CN>是否允许新事实使用该值。</zh-CN><en>Whether new facts may use this value.</en></lang></summary>
        public bool IsActive { get; set; }

        /// <summary><lang><zh-CN>是否为安装脚本提供的基础种子。</zh-CN><en>Whether this is an installation-script base seed.</en></lang></summary>
        public bool IsSystemSeed { get; set; }
    }
}
