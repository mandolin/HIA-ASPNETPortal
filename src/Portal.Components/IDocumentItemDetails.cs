namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：包含历史数据库二进制内容的文档模块详情契约。
    ///
    /// English: Document-module detail contract that includes legacy database binary content.
    /// </summary>
    /// <remarks>
    /// 中文：当前项目不再写入此二进制内容；它只为已有记录下载兼容保留，输出时必须作为附件处理。
    ///
    /// English: The project no longer writes this binary content; it remains only for existing-record download compatibility
    /// and must be emitted as an attachment.
    /// </remarks>
    public interface IDocumentItemDetails : IDocumentItem
    {
        /// <summary>中文：历史数据库保存的二进制内容。English: Binary content stored by the legacy database.</summary>
        byte[] Content { get; set; }

        /// <summary>中文：历史记录保存的 MIME 类型提示，不应直接信任。English: MIME-type hint stored by legacy records; it must not be trusted directly.</summary>
        string ContentType { get; set; }
    }
}
