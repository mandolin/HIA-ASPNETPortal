using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：文档模块列表项的跨层数据契约。
    ///
    /// English: Cross-layer data contract for one document-module list item.
    /// </summary>
    /// <remarks>
    /// 中文：<see cref="FileNameUrl"/> 可以是受限服务器上传的应用相对地址，也可以是经过策略校验的外部 HTTP(S)
    /// 地址。它不是文件授权凭据；数据库内容大于零时由历史下载页处理。
    ///
    /// English: <see cref="FileNameUrl"/> may be an application-relative address for a restricted server upload or an
    /// HTTP(S) address validated by policy. It is not a file authorization credential; positive database content is
    /// handled by the legacy download page.
    /// </remarks>
    public interface IDocumentItem
    {
        /// <summary>中文：面向门户访问者的文档显示名称。English: Document display name shown to Portal visitors.</summary>
        string FileFriendlyName { get; set; }

        /// <summary>中文：浏览地址或服务器上传相对路径。English: Browse address or server-upload relative path.</summary>
        string FileNameUrl { get; set; }

        /// <summary>中文：最后写入记录的用户名。English: User name that last wrote the record.</summary>
        string CreatedByUser { get; set; }

        /// <summary>中文：记录最后写入时间；旧记录可能为空。English: Record last-write time; legacy records may be empty.</summary>
        DateTime? CreatedDate { get; set; }

        /// <summary>中文：供列表筛选或显示使用的业务分类。English: Business category used for list filtering or display.</summary>
        string Category { get; set; }

        /// <summary>中文：历史数据库内容的声明字节数。English: Declared byte count of legacy database content.</summary>
        int? ContentSize { get; set; }

        /// <summary>中文：兼容旧绑定的内容大小，空值按零处理。English: Content size compatible with legacy binding; null is treated as zero.</summary>
        int Size { get; }

        /// <summary>中文：文档所属模块实例标识。English: Owning module-instance identifier.</summary>
        int ModuleId { get; set; }

        /// <summary>中文：文档项目稳定数值标识。English: Stable numeric document-item identifier.</summary>
        int ItemId { get; set; }
    }
}
