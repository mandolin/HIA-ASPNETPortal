using System.Web;

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

        /// <summary>
        /// <lang>
        ///   <zh-CN>把当前解析出的传输安全属性应用到指定 Cookie；Unset 时不触碰 SameSite，保持框架默认。</zh-CN>
        ///   <en>Applies the currently resolved transport-security attributes to the specified cookie; Unset leaves SameSite untouched so the framework default is preserved.</en>
        /// </lang>
        /// </summary>
        /// <param name="cookie">
        /// <l>
        ///   <zh-CN>要应用属性的 Cookie；为 null 时直接返回。</zh-CN>
        ///   <en>Cookie to apply attributes to; returns directly when null.</en>
        /// </l>
        /// </param>
        public static void ApplyTo(HttpCookie cookie)
        {
            // <lang>
            //   <zh-CN>空引用不抛异常：Cookie 写入路径不应因策略调用新增失败模式。</zh-CN>
            //   <en>Null never throws: the cookie-write path must not gain a new failure mode from the policy call.</en>
            // </lang>
            if (cookie == null)
            {
                return;
            }

            PortalCookieSecurityOptions options = Resolve();

            // <lang>
            //   <zh-CN>Secure 总是按策略显式赋值；默认 false 与历史行为一致。</zh-CN>
            //   <en>Secure is always assigned explicitly from policy; the false default matches historical behavior.</en>
            // </lang>
            cookie.Secure = options.Secure;

            // <lang>
            //   <zh-CN>Unset 不触碰 SameSite 属性，确保默认部署不额外下发该属性，与历史行为完全一致。</zh-CN>
            //   <en>Unset leaves the SameSite property untouched so default deployments emit no extra attribute, exactly matching historical behavior.</en>
            // </lang>
            if (options.SameSite == PortalCookieSameSiteMode.Unset)
            {
                return;
            }

            // <lang>
            //   <zh-CN>只在明确配置 Lax/Strict/None 时映射到框架 SameSiteMode；None 已在策略层校验必须配合 Secure。</zh-CN>
            //   <en>Map to the framework SameSiteMode only for explicitly configured Lax/Strict/None; None was already validated at the policy layer to require Secure.</en>
            // </lang>
            switch (options.SameSite)
            {
                case PortalCookieSameSiteMode.Lax:
                    cookie.SameSite = SameSiteMode.Lax;
                    break;
                case PortalCookieSameSiteMode.Strict:
                    cookie.SameSite = SameSiteMode.Strict;
                    break;
                case PortalCookieSameSiteMode.None:
                    cookie.SameSite = SameSiteMode.None;
                    break;
            }
        }
    }
}
