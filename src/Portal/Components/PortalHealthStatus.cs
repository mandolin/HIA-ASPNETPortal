namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 系统健康状态级别。
    /// System health status levels.
    /// </summary>
    public enum PortalHealthStatus
    {
        /// <summary>
        /// 检查通过。
        /// The check is healthy.
        /// </summary>
        Healthy,

        /// <summary>
        /// 存在需要关注但不一定阻断运行的问题。
        /// The check has a warning that may not block runtime.
        /// </summary>
        Warning,

        /// <summary>
        /// 存在错误或关键资源不可用。
        /// The check has an error or a critical resource is unavailable.
        /// </summary>
        Error,

        /// <summary>
        /// 当前无法判断状态。
        /// The state cannot be determined.
        /// </summary>
        Unknown
    }
}
