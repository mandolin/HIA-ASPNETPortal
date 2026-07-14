using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：映射到 <c>Portal_Documents</c> 的旧文档模块实体。
    ///
    /// English: Legacy document-module entity mapped to <c>Portal_Documents</c>.
    /// </summary>
    /// <remarks>
    /// 中文：二进制内容列仅用于读取已有数据库记录；当前服务器上传将文件写入受限 uploads 目录，
    /// 并将应用相对路径写入 <see cref="FileNameUrl"/>。
    ///
    /// English: The binary-content columns are retained only to read existing database records. Current server uploads write
    /// files to the restricted uploads directory and store an application-relative path in <see cref="FileNameUrl"/>.
    /// </remarks>
    [Table("Portal_Documents")]
    public class DocumentItem : IDocumentItemDetails
    {
        /// <summary>中文：文档显示名称。English: Document display name.</summary>
        public string FileFriendlyName { get; set; }

        /// <summary>中文：浏览地址或服务器上传路径。English: Browse address or server-upload path.</summary>
        public string FileNameUrl { get; set; }

        /// <summary>中文：最后写入用户名。English: Last writing user name.</summary>
        public string CreatedByUser { get; set; }

        /// <summary>中文：最后写入时间。English: Last write time.</summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>中文：业务分类。English: Business category.</summary>
        public string Category { get; set; }

        /// <summary>中文：历史数据库内容的声明大小。English: Declared size of legacy database content.</summary>
        public int? ContentSize { get; set; }

        /// <summary>中文：兼容旧绑定的内容大小，空值按零处理。English: Legacy-binding content size; null is treated as zero.</summary>
        public int Size
        {
            get { return ContentSize ?? 0; }
        }

        /// <summary>中文：所属模块实例标识。English: Owning module-instance identifier.</summary>
        public int ModuleId { get; set; }

        /// <summary>中文：文档项目主键。English: Document-item primary key.</summary>
        [Key]
        public int ItemId { get; set; }

        /// <summary>中文：历史数据库二进制内容。English: Legacy database binary content.</summary>
        public byte[] Content { get; set; }

        /// <summary>中文：历史 MIME 类型提示。English: Legacy MIME-type hint.</summary>
        public string ContentType { get; set; }
    }
}
