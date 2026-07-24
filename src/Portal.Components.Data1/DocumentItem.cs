using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>映射到 <c>Portal_Documents</c> 的旧文档模块实体。</zh-CN>
    ///   <en>Legacy document-module entity mapped to <c>Portal_Documents</c>.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>二进制内容列仅用于读取已有数据库记录；当前服务器上传将文件写入受限 uploads 目录， 并将应用相对路径写入 <see cref="FileNameUrl"/>。</zh-CN>
    ///   <en>The binary-content columns are retained only to read existing database records. Current server uploads write files to the restricted uploads directory and store an application-relative path in <see cref="FileNameUrl"/>.</en>
    /// </lang>
    /// </remarks>
    [Table("Portal_Documents")]
    public class DocumentItem : IDocumentItemDetails
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>文档显示名称。</zh-CN>
        ///   <en>Document display name.</en>
        /// </lang>
        /// </summary>
        public string FileFriendlyName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>浏览地址或服务器上传路径。</zh-CN>
        ///   <en>Browse address or server-upload path.</en>
        /// </lang>
        /// </summary>
        public string FileNameUrl { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最后写入用户名。</zh-CN>
        ///   <en>Last writing user name.</en>
        /// </lang>
        /// </summary>
        public string CreatedByUser { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最后写入时间。</zh-CN>
        ///   <en>Last write time.</en>
        /// </lang>
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>业务分类。</zh-CN>
        ///   <en>Business category.</en>
        /// </lang>
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>历史数据库内容的声明大小。</zh-CN>
        ///   <en>Declared size of legacy database content.</en>
        /// </lang>
        /// </summary>
        public int? ContentSize { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>兼容旧绑定的内容大小，空值按零处理。</zh-CN>
        ///   <en>Legacy-binding content size; null is treated as zero.</en>
        /// </lang>
        /// </summary>
        public int Size
        {
            get { return ContentSize ?? 0; }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>所属模块实例标识。</zh-CN>
        ///   <en>Owning module-instance identifier.</en>
        /// </lang>
        /// </summary>
        public int ModuleId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>文档项目主键。</zh-CN>
        ///   <en>Document-item primary key.</en>
        /// </lang>
        /// </summary>
        [Key]
        public int ItemId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>历史数据库二进制内容。</zh-CN>
        ///   <en>Legacy database binary content.</en>
        /// </lang>
        /// </summary>
        public byte[] Content { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>历史 MIME 类型提示。</zh-CN>
        ///   <en>Legacy MIME-type hint.</en>
        /// </lang>
        /// </summary>
        public string ContentType { get; set; }
    }
}
