namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>系统健康状态级别。</zh-CN>
    ///   <en>System health status levels.</en>
    /// </lang>
    /// </summary>
    public enum PortalHealthStatus
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>检查通过。</zh-CN>
        ///   <en>The check is healthy.</en>
        /// </lang>
        /// </summary>
        Healthy,

        /// <summary>
        /// <lang>
        ///   <zh-CN>存在需要关注但不一定阻断运行的问题。</zh-CN>
        ///   <en>The check has a warning that may not block runtime.</en>
        /// </lang>
        /// </summary>
        Warning,

        /// <summary>
        /// <lang>
        ///   <zh-CN>存在错误或关键资源不可用。</zh-CN>
        ///   <en>The check has an error or a critical resource is unavailable.</en>
        /// </lang>
        /// </summary>
        Error,

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前无法判断状态。</zh-CN>
        ///   <en>The state cannot be determined.</en>
        /// </lang>
        /// </summary>
        Unknown
    }
}
