namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>抽象业务申请写入结果。</zh-CN>
    ///   <en>Write result for abstract business applications.</en>
    /// </lang>
    /// </summary>
    public sealed class BusinessApplicationResult
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建业务申请写入结果。</zh-CN>
        ///   <en>Creates a business-application write result.</en>
        /// </lang>
        /// </summary>
        public BusinessApplicationResult(bool succeeded, long applicationId, string applicationCode, string message)
        {
            Succeeded = succeeded;
            ApplicationId = applicationId;
            ApplicationCode = applicationCode ?? string.Empty;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>操作是否成功。</zh-CN>
        ///   <en>Whether the operation succeeded.</en>
        /// </lang>
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>业务申请主键。</zh-CN>
        ///   <en>Business-application primary key.</en>
        /// </lang>
        /// </summary>
        public long ApplicationId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>业务申请编号。</zh-CN>
        ///   <en>Business-application code.</en>
        /// </lang>
        /// </summary>
        public string ApplicationCode { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可展示的低敏结果说明。</zh-CN>
        ///   <en>Display-safe result message.</en>
        /// </lang>
        /// </summary>
        public string Message { get; private set; }
    }
}
