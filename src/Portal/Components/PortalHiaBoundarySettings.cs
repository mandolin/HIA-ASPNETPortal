namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>HIA 外围契约的部署级设置读取器。</zh-CN>
    ///   <en>Deployment-level settings reader for the HIA peripheral contract.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该读取器只解析实例标识，不启用 adapter、transport 或外部程序集加载。</zh-CN>
    ///   <en>This reader resolves the instance identifier only; it does not enable adapters, transport, or external assembly loading.</en>
    /// </lang>
    /// </remarks>
    public static class PortalHiaBoundarySettings
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>获取已配置且通过格式验证的门户实例标识。</zh-CN>
        ///   <en>Gets the configured portal instance identifier when it passes format validation.</en>
        /// </lang>
        /// </summary>
        /// <param name="portalInstanceId">
        /// <l>
        ///   <zh-CN>成功时返回规范化标识；未配置或非法时为空。</zh-CN>
        ///   <en>Normalized identifier when successful; empty when absent or invalid.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>存在可用部署级标识时为 true。</zh-CN>
        ///   <en>True when a usable deployment-level identifier exists.</en>
        /// </l>
        /// </returns>
        public static bool TryGetPortalInstanceId(out string portalInstanceId)
        {
            // <lang>
            //   <zh-CN>从受控 registry 读取部署级候选值；该读取不会启用外围适配器或外部连接。</zh-CN>
            //   <en>Read the deployment-level candidate from the controlled registry; this read does not enable peripheral adapters or external connections.</en>
            // </lang>
            string configuredValue = PortalRuntimeSettings.GetString(PortalSettingsRegistry.HiaPortalInstanceId);

            // <lang>
            //   <zh-CN>统一交给正式契约执行空值、GUID、大小写和字符范围校验，避免设置读取器复制规则。</zh-CN>
            //   <en>Delegate null, GUID, casing, and character-range validation to the production contract so the settings reader does not duplicate rules.</en>
            // </lang>
            return PortalHiaBoundaryContract.TryNormalizePortalInstanceId(configuredValue, out portalInstanceId);
        }
    }
}
