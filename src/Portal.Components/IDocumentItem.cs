using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>文档模块列表项的跨层数据契约。</zh-CN>
    ///   <en>Cross-layer data contract for one document-module list item.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN><see cref="FileNameUrl"/> 可以是受限服务器上传的应用相对地址，也可以是经过策略校验的外部 HTTP(S) 地址。它不是文件授权凭据；数据库内容大于零时由历史下载页处理。</zh-CN>
    ///   <en><see cref="FileNameUrl"/> may be an application-relative address for a restricted server upload or an HTTP(S) address validated by policy. It is not a file authorization credential; positive database content is handled by the legacy download page.</en>
    /// </lang>
    /// </remarks>
    public interface IDocumentItem
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>面向门户访问者的文档显示名称。</zh-CN>
        ///   <en>Document display name shown to Portal visitors.</en>
        /// </lang>
        /// </summary>
        string FileFriendlyName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>浏览地址或服务器上传相对路径。</zh-CN>
        ///   <en>Browse address or server-upload relative path.</en>
        /// </lang>
        /// </summary>
        string FileNameUrl { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最后写入记录的用户名。</zh-CN>
        ///   <en>User name that last wrote the record.</en>
        /// </lang>
        /// </summary>
        string CreatedByUser { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>记录最后写入时间；旧记录可能为空。</zh-CN>
        ///   <en>Record last-write time; legacy records may be empty.</en>
        /// </lang>
        /// </summary>
        DateTime? CreatedDate { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>供列表筛选或显示使用的业务分类。</zh-CN>
        ///   <en>Business category used for list filtering or display.</en>
        /// </lang>
        /// </summary>
        string Category { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>历史数据库内容的声明字节数。</zh-CN>
        ///   <en>Declared byte count of legacy database content.</en>
        /// </lang>
        /// </summary>
        int? ContentSize { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>兼容旧绑定的内容大小，空值按零处理。</zh-CN>
        ///   <en>Content size compatible with legacy binding; null is treated as zero.</en>
        /// </lang>
        /// </summary>
        int Size { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>文档所属模块实例标识。</zh-CN>
        ///   <en>Owning module-instance identifier.</en>
        /// </lang>
        /// </summary>
        int ModuleId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>文档项目稳定数值标识。</zh-CN>
        ///   <en>Stable numeric document-item identifier.</en>
        /// </lang>
        /// </summary>
        int ItemId { get; set; }
    }
}
