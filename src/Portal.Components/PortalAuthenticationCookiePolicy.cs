using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户身份与角色 Cookie 的 SameSite 模式。</zh-CN>
    ///   <en>SameSite mode for Portal identity and role cookies.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>Unset 表示不下发 SameSite 属性，用于保持与历史部署和 IE9+ 旧浏览器完全一致的行为；None 只在同时启用 Secure 时才有效，否则会被降级为 Unset。</zh-CN>
    ///   <en>Unset means the SameSite attribute is not emitted, preserving behavior identical to historical deployments and IE9+ legacy browsers; None is valid only together with Secure and is otherwise degraded to Unset.</en>
    /// </lang>
    /// </remarks>
    public enum PortalCookieSameSiteMode
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>不下发 SameSite 属性，保持历史兼容行为。</zh-CN>
        ///   <en>Do not emit the SameSite attribute, preserving historical compatibility behavior.</en>
        /// </lang>
        /// </summary>
        Unset,

        /// <summary>
        /// <lang>
        ///   <zh-CN>下发 SameSite=Lax。</zh-CN>
        ///   <en>Emit SameSite=Lax.</en>
        /// </lang>
        /// </summary>
        Lax,

        /// <summary>
        /// <lang>
        ///   <zh-CN>下发 SameSite=Strict。</zh-CN>
        ///   <en>Emit SameSite=Strict.</en>
        /// </lang>
        /// </summary>
        Strict,

        /// <summary>
        /// <lang>
        ///   <zh-CN>下发 SameSite=None；必须同时启用 Secure 才有效。</zh-CN>
        ///   <en>Emit SameSite=None; valid only when Secure is enabled at the same time.</en>
        /// </lang>
        /// </summary>
        None
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>身份与角色 Cookie 的传输安全属性决策结果。</zh-CN>
    ///   <en>Transport-security attribute decision result for identity and role cookies.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该对象只承载决策结果，不创建、不写入也不读取任何 Cookie；Degraded 与 DegradeReason 仅用于诊断，且不含凭据、票据或路径等敏感内容。</zh-CN>
    ///   <en>This object carries decision results only and never creates, writes, or reads cookies; Degraded and DegradeReason exist for diagnostics and contain no credential, ticket, or path data.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalCookieSecurityOptions
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>使用给定的安全属性与降级状态初始化 Cookie 安全选项。</zh-CN>
        ///   <en>Initializes cookie security options with the supplied attributes and degradation state.</en>
        /// </lang>
        /// </summary>
        /// <param name="secure">
        /// <l>
        ///   <zh-CN>是否下发 Secure 属性。</zh-CN>
        ///   <en>Whether to emit the Secure attribute.</en>
        /// </l>
        /// </param>
        /// <param name="sameSite">
        /// <l>
        ///   <zh-CN>要下发的 SameSite 模式。</zh-CN>
        ///   <en>SameSite mode to emit.</en>
        /// </l>
        /// </param>
        /// <param name="degraded">
        /// <l>
        ///   <zh-CN>配置是否被降级处理。</zh-CN>
        ///   <en>Whether the configuration was degraded.</en>
        /// </l>
        /// </param>
        /// <param name="degradeReason">
        /// <l>
        ///   <zh-CN>降级原因代码；未降级时为空字符串。</zh-CN>
        ///   <en>Degradation reason code; an empty string when not degraded.</en>
        /// </l>
        /// </param>
        public PortalCookieSecurityOptions(
            bool secure,
            PortalCookieSameSiteMode sameSite,
            bool degraded,
            string degradeReason)
        {
            // <lang>
            //   <zh-CN>逐字段保存决策结果；原因文本统一规范化为空字符串，避免调用方对 null 做额外判断。</zh-CN>
            //   <en>Store each decision field; the reason text is normalized to an empty string so callers need no extra null checks.</en>
            // </lang>
            Secure = secure;
            SameSite = sameSite;
            Degraded = degraded;
            DegradeReason = degradeReason ?? string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否下发 Secure 属性；默认关闭以兼容 HTTP 与旧浏览器部署。</zh-CN>
        ///   <en>Whether to emit the Secure attribute; disabled by default for HTTP and legacy-browser compatibility.</en>
        /// </lang>
        /// </summary>
        public bool Secure { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>要下发的 SameSite 模式；默认为 Unset（不下发该属性）。</zh-CN>
        ///   <en>SameSite mode to emit; defaults to Unset (the attribute is not emitted).</en>
        /// </lang>
        /// </summary>
        public PortalCookieSameSiteMode SameSite { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>配置是否被降级处理；为 true 时应结合 DegradeReason 写入诊断。</zh-CN>
        ///   <en>Whether the configuration was degraded; when true, DegradeReason should be written to diagnostics.</en>
        /// </lang>
        /// </summary>
        public bool Degraded { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>降级原因代码；未降级时为空字符串。</zh-CN>
        ///   <en>Degradation reason code; an empty string when not degraded.</en>
        /// </lang>
        /// </summary>
        public string DegradeReason { get; private set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>裁定身份与角色 Cookie 的传输安全属性；只做纯决策，不读取配置、不创建或写入 Cookie。</zh-CN>
    ///   <en>Decides transport-security attributes for identity and role cookies; it performs pure decisions only and never reads configuration or creates/writes cookies.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本类不依赖 System.Web，也不依赖运行时设置读取，便于被纯内存单测覆盖；读取配置并调用本类的职责由 Web 项目的 PortalAuthenticationCookieSettings 承担。SameSite 取 None 而未启用 Secure 时一律降级为 Unset。</zh-CN>
    ///   <en>This class depends on neither System.Web nor runtime setting reads so pure in-memory unit tests can cover it; reading configuration and invoking this class belongs to PortalAuthenticationCookieSettings in the web project. SameSite=None without Secure always degrades to Unset.</en>
    /// </lang>
    /// </remarks>
    public static class PortalAuthenticationCookiePolicy
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>SameSite 取值无法解析时的降级原因代码。</zh-CN>
        ///   <en>Degradation reason code used when the SameSite value cannot be parsed.</en>
        /// </lang>
        /// </summary>
        public const string ReasonUnparsableSameSite = "UnparsableSameSite";

        /// <summary>
        /// <lang>
        ///   <zh-CN>SameSite 取 None 但未启用 Secure 时的降级原因代码。</zh-CN>
        ///   <en>Degradation reason code used when SameSite is None but Secure is disabled.</en>
        /// </lang>
        /// </summary>
        public const string ReasonNoneRequiresSecure = "SameSiteNoneRequiresSecure";

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取设置失败并回落默认时的降级原因代码。</zh-CN>
        ///   <en>Degradation reason code used when reading settings fails and defaults are applied.</en>
        /// </lang>
        /// </summary>
        public const string ReasonSettingsReadFailed = "SettingsReadFailed";

        /// <summary>
        /// <lang>
        ///   <zh-CN>按安全约束归一化 Cookie 传输安全属性；SameSite 取 None 而未启用 Secure 时降级为 Unset。</zh-CN>
        ///   <en>Normalizes cookie transport-security attributes against security constraints; SameSite=None without Secure degrades to Unset.</en>
        /// </lang>
        /// </summary>
        /// <param name="secure">
        /// <l>
        ///   <zh-CN>配置解析出的 Secure 取值。</zh-CN>
        ///   <en>Secure value resolved from configuration.</en>
        /// </l>
        /// </param>
        /// <param name="sameSite">
        /// <l>
        ///   <zh-CN>配置解析出的 SameSite 模式。</zh-CN>
        ///   <en>SameSite mode resolved from configuration.</en>
        /// </l>
        /// </param>
        /// <param name="reason">
        /// <l>
        ///   <zh-CN>已发生的降级原因代码；无降级时传空字符串。</zh-CN>
        ///   <en>Degradation reason code already encountered; pass an empty string when none occurred.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>满足安全约束的最终属性决策结果。</zh-CN>
        ///   <en>Final attribute decision result that satisfies the security constraints.</en>
        /// </l>
        /// </returns>
        public static PortalCookieSecurityOptions Normalize(
            bool secure,
            PortalCookieSameSiteMode sameSite,
            string reason)
        {
            // <lang>
            //   <zh-CN>已有原因代码即代表发生过降级；空原因表示配置尚未触发任何降级。</zh-CN>
            //   <en>A non-empty reason code means degradation already occurred; an empty reason means configuration triggered no degradation yet.</en>
            // </lang>
            bool degraded = !string.IsNullOrEmpty(reason);

            // <lang>
            //   <zh-CN>None 必须配合 Secure，否则现代浏览器会直接拒绝该 Cookie；此处降级为 Unset 以保持可用性，而不是放任无效组合生效。</zh-CN>
            //   <en>None requires Secure, otherwise modern browsers reject the cookie outright; degrade to Unset to keep the cookie usable instead of allowing an invalid combination to take effect.</en>
            // </lang>
            if (sameSite == PortalCookieSameSiteMode.None && !secure)
            {
                sameSite = PortalCookieSameSiteMode.Unset;
                degraded = true;
                reason = ReasonNoneRequiresSecure;
            }

            return new PortalCookieSecurityOptions(secure, sameSite, degraded, reason);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>尝试把配置文本解析为 SameSite 模式；无法识别时返回 false 并给出 Unset。</zh-CN>
        ///   <en>Attempts to parse configuration text into a SameSite mode; returns false with Unset when unrecognized.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>配置文本；为空时视为 Unset。</zh-CN>
        ///   <en>Configuration text; blank is treated as Unset.</en>
        /// </l>
        /// </param>
        /// <param name="sameSite">
        /// <l>
        ///   <zh-CN>成功时返回对应模式；失败时为 Unset。</zh-CN>
        ///   <en>Corresponding mode on success; Unset on failure.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>取值为空或已识别的模式时返回 true；未知取值时返回 false。</zh-CN>
        ///   <en>Returns true for blank or a recognized mode; false for an unknown value.</en>
        /// </l>
        /// </returns>
        public static bool TryParseSameSite(string value, out PortalCookieSameSiteMode sameSite)
        {
            // <lang>
            //   <zh-CN>空值按 Unset 处理且不视为降级，因为默认本就不下发该属性。</zh-CN>
            //   <en>Blank is treated as Unset and is not a degradation because the default already omits the attribute.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(value))
            {
                sameSite = PortalCookieSameSiteMode.Unset;
                return true;
            }

            // <lang>
            //   <zh-CN>按不区分大小写精确比对四个受控取值，避免宽松匹配把未知文本解释为某种模式。</zh-CN>
            //   <en>Compare the four controlled values case-insensitively and exactly so loose matching never interprets unknown text as a mode.</en>
            // </lang>
            if (string.Equals(value, "Unset", StringComparison.OrdinalIgnoreCase))
            {
                sameSite = PortalCookieSameSiteMode.Unset;
                return true;
            }

            if (string.Equals(value, "Lax", StringComparison.OrdinalIgnoreCase))
            {
                sameSite = PortalCookieSameSiteMode.Lax;
                return true;
            }

            if (string.Equals(value, "Strict", StringComparison.OrdinalIgnoreCase))
            {
                sameSite = PortalCookieSameSiteMode.Strict;
                return true;
            }

            if (string.Equals(value, "None", StringComparison.OrdinalIgnoreCase))
            {
                sameSite = PortalCookieSameSiteMode.None;
                return true;
            }

            // <lang>
            //   <zh-CN>未知取值不猜测兼容模式，交回 Unset 并由调用方标记降级原因。</zh-CN>
            //   <en>Unknown values are never guessed into a compatibility mode; Unset is returned and the caller records the degradation reason.</en>
            // </lang>
            sameSite = PortalCookieSameSiteMode.Unset;
            return false;
        }
    }
}
