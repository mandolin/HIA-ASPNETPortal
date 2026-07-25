namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>包含历史数据库二进制内容的文档模块详情契约。</zh-CN>
    ///   <en>Document-module detail contract that includes legacy database binary content.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>当前项目不再写入此二进制内容；它只为已有记录下载兼容保留，输出时必须作为附件处理，并由下载页重新决定文件名、MIME 类型和缓存策略。</zh-CN>
    ///   <en>The project no longer writes this binary content; it remains only for existing-record download compatibility, must be emitted as an attachment, and lets the download page decide file name, MIME type, and caching policy again.</en>
    /// </lang>
    /// </remarks>
    public interface IDocumentItemDetails : IDocumentItem
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>历史数据库保存的二进制内容。</zh-CN>
        ///   <en>Binary content stored by the legacy database.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该字段可能较大，不应在列表页、审计日志或诊断日志中展开；读取后只应进入受控下载响应。</zh-CN>
        ///   <en>This field may be large and should not be expanded in list pages, audit logs, or diagnostic logs; after reading it should only flow into a controlled download response.</en>
        /// </lang>
        /// </remarks>
        byte[] Content { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>历史记录保存的 MIME 类型提示，不应直接信任。</zh-CN>
        ///   <en>MIME-type hint stored by legacy records; it must not be trusted directly.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>下载页应按当前文档策略复核此值，无法确认时使用安全的二进制附件类型。</zh-CN>
        ///   <en>The download page should re-check this value against the current document policy and use a safe binary attachment type when it cannot be confirmed.</en>
        /// </lang>
        /// </remarks>
        string ContentType { get; set; }
    }
}
