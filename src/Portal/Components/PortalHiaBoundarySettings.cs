namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// HIA 外围契约的部署级设置读取器。
    /// Deployment-level settings reader for the HIA peripheral contract.
    /// </summary>
    /// <remarks>
    /// 该读取器只解析实例标识，不启用 adapter、transport 或外部程序集加载。
    /// This reader resolves the instance identifier only; it does not enable adapters, transport, or external assembly loading.
    /// </remarks>
    public static class PortalHiaBoundarySettings
    {
        /// <summary>
        /// 获取已配置且通过格式验证的门户实例标识。
        /// Gets the configured portal instance identifier when it passes format validation.
        /// </summary>
        /// <param name="portalInstanceId">成功时返回规范化标识；未配置或非法时为空。Normalized identifier when successful; empty when absent or invalid.</param>
        /// <returns>存在可用部署级标识时为 true。True when a usable deployment-level identifier exists.</returns>
        public static bool TryGetPortalInstanceId(out string portalInstanceId)
        {
            string configuredValue = PortalRuntimeSettings.GetString(PortalSettingsRegistry.HiaPortalInstanceId);
            return PortalHiaBoundaryContract.TryNormalizePortalInstanceId(configuredValue, out portalInstanceId);
        }
    }
}
