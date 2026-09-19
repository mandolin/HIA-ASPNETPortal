namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>读取当前系统设置并产出身份与角色 Cookie 的传输安全属性。</zh-CN>
    ///   <en>Reads current system settings and produces transport-security attributes for identity and role cookies.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本类只负责读取设置与委托裁定，不创建或写入 Cookie；安全约束（SameSite 取 None 必须配合 Secure）统一由 PortalAuthenticationCookiePolicy 裁定。设置缺失、无法解析或读取异常一律回落为默认（不下发 Secure、不下发 SameSite），绝不因配置问题阻断登录。</zh-CN>
    ///   <en>This class only reads settings and delegates the decision; it never creates or writes cookies. Security constraints (SameSite=None requires Secure) are decided centrally by PortalAuthenticationCookiePolicy. Missing, unparsable, or failing settings always fall back to the default (neither Secure nor SameSite emitted) and never block sign-in because of configuration problems.</en>
    /// </lang>
    /// </remarks>
    public static class PortalAuthenticationCookieSettings
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前配置并解析 Cookie 传输安全属性；读取失败时回落默认，不阻断登录。</zh-CN>
        ///   <en>Reads current configuration and resolves cookie transport-security attributes; read failures fall back to defaults without blocking sign-in.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>可直接供 Cookie 写入点消费的安全属性决策结果。</zh-CN>
        ///   <en>Security-attribute decision result that cookie write sites can consume directly.</en>
        /// </l>
        /// </returns>
        public static PortalCookieSecurityOptions Resolve()
        {
            // <lang>
            //   <zh-CN>先以默认（现状）建立结果；后续任何失败路径都保持这一基线，避免配置问题改变既有部署行为。</zh-CN>
            //   <en>Start from the default (current-behavior) result so every failure path preserves that baseline and configuration problems never change existing deployment behavior.</en>
            // </lang>
            bool secure = false;
            PortalCookieSameSiteMode sameSite = PortalCookieSameSiteMode.Unset;
            string reason = string.Empty;

            try
            {
                // <lang>
                //   <zh-CN>Secure 由布尔设置直接决定；SameSite 由字符串设置解析，无法识别时保留 Unset 并记录降级原因。</zh-CN>
                //   <en>Secure comes directly from the Boolean setting; SameSite is parsed from the string setting, and an unrecognized value keeps Unset while recording the degradation reason.</en>
                // </lang>
                secure = PortalRuntimeSettings.GetBoolean(PortalSettingsRegistry.CookiesSecure);
                if (!PortalAuthenticationCookiePolicy.TryParseSameSite(
                        PortalRuntimeSettings.GetString(PortalSettingsRegistry.CookiesSameSite),
                        out sameSite))
                {
                    sameSite = PortalCookieSameSiteMode.Unset;
                    reason = PortalAuthenticationCookiePolicy.ReasonUnparsableSameSite;
                }
            }
            catch
            {
                // <lang>
                //   <zh-CN>设置读取可能因配置、数据库或解析异常失败；此处统一回落默认，绝不向上抛出，避免登录流程被配置问题中断。</zh-CN>
                //   <en>Setting reads may fail because of configuration, database, or parsing errors; failures fall back to defaults and are never rethrown so sign-in is not interrupted by configuration problems.</en>
                // </lang>
                secure = false;
                sameSite = PortalCookieSameSiteMode.Unset;
                reason = PortalAuthenticationCookiePolicy.ReasonSettingsReadFailed;
            }

            // <lang>
            //   <zh-CN>统一交给纯策略类裁定，确保"None 必须配合 Secure"等约束在任何读取路径下都成立。</zh-CN>
            //   <en>Delegate to the pure policy class so constraints such as "None requires Secure" hold on every read path.</en>
            // </lang>
            return PortalAuthenticationCookiePolicy.Normalize(secure, sameSite, reason);
        }
    }
}
