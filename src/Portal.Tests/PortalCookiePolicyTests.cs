using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>覆盖 Cookie 传输安全策略纯决策逻辑的契约测试。</zh-CN>
    ///   <en>Contract tests covering the pure decision logic of the cookie transport-security policy.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>测试只调用不依赖 System.Web 的纯策略类 PortalAuthenticationCookiePolicy，因此不需要 Web 运行时、配置文件或数据库；读取设置的 PortalAuthenticationCookieSettings 属 Web 项目，不在本测试范围内。</zh-CN>
    ///   <en>The tests invoke only PortalAuthenticationCookiePolicy, which has no System.Web dependency, so no web runtime, configuration file, or database is required; PortalAuthenticationCookieSettings reads settings and belongs to the web project, so it is out of scope here.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalCookiePolicyTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>验证默认输入下不下发 Secure 也不下发 SameSite，且未发生降级。</zh-CN>
        ///   <en>Verifies that default input emits neither Secure nor SameSite and reports no degradation.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Normalize_DefaultsToNoSecureAndUnsetSameSite()
        {
            // <lang>
            //   <zh-CN>以空原因调用归一化，代表配置尚未触发任何降级。</zh-CN>
            //   <en>Call normalization with an empty reason, representing configuration that triggered no degradation.</en>
            // </lang>
            PortalCookieSecurityOptions options = PortalAuthenticationCookiePolicy.Normalize(
                false,
                PortalCookieSameSiteMode.Unset,
                string.Empty);

            Assert.IsFalse(options.Secure, "默认不应下发 Secure 属性。");
            Assert.AreEqual(PortalCookieSameSiteMode.Unset, options.SameSite, "默认不应下发 SameSite 属性。");
            Assert.IsFalse(options.Degraded, "默认输入不应产生降级。");
            Assert.AreEqual(string.Empty, options.DegradeReason, "未降级时原因代码应为空。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证 SameSite 取 None 但未启用 Secure 时降级为 Unset，并给出对应原因代码。</zh-CN>
        ///   <en>Verifies that SameSite=None without Secure degrades to Unset with the matching reason code.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Normalize_DegradesNoneToUnsetWhenSecureDisabled()
        {
            PortalCookieSecurityOptions options = PortalAuthenticationCookiePolicy.Normalize(
                false,
                PortalCookieSameSiteMode.None,
                string.Empty);

            Assert.AreEqual(PortalCookieSameSiteMode.Unset, options.SameSite, "None 未配 Secure 时应降级为 Unset。");
            Assert.IsTrue(options.Degraded, "该组合应被标记为降级。");
            Assert.AreEqual(
                PortalAuthenticationCookiePolicy.ReasonNoneRequiresSecure,
                options.DegradeReason,
                "降级原因应为 None 需要 Secure。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证 SameSite 取 None 且已启用 Secure 时保持 None，不产生降级。</zh-CN>
        ///   <en>Verifies that SameSite=None with Secure enabled stays None without degradation.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Normalize_KeepsNoneWhenSecureEnabled()
        {
            PortalCookieSecurityOptions options = PortalAuthenticationCookiePolicy.Normalize(
                true,
                PortalCookieSameSiteMode.None,
                string.Empty);

            Assert.IsTrue(options.Secure, "显式启用 Secure 时应保留。");
            Assert.AreEqual(PortalCookieSameSiteMode.None, options.SameSite, "配 Secure 时 None 应保留。");
            Assert.IsFalse(options.Degraded, "合法组合不应被标记为降级。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证 Lax 与 Strict 不要求 Secure，未启用 Secure 时也不降级。</zh-CN>
        ///   <en>Verifies that Lax and Strict do not require Secure and are not degraded when Secure is disabled.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Normalize_KeepsLaxAndStrictWithoutSecure()
        {
            // <lang>
            //   <zh-CN>Lax 与 Strict 只约束跨站请求，不依赖 Secure；分别断言两者保持原值。</zh-CN>
            //   <en>Lax and Strict constrain cross-site requests only and do not depend on Secure; assert that both keep their values.</en>
            // </lang>
            PortalCookieSecurityOptions lax = PortalAuthenticationCookiePolicy.Normalize(
                false,
                PortalCookieSameSiteMode.Lax,
                string.Empty);
            Assert.AreEqual(PortalCookieSameSiteMode.Lax, lax.SameSite, "Lax 不需要 Secure，应保留。");
            Assert.IsFalse(lax.Degraded, "Lax 未配 Secure 不应降级。");

            PortalCookieSecurityOptions strict = PortalAuthenticationCookiePolicy.Normalize(
                false,
                PortalCookieSameSiteMode.Strict,
                string.Empty);
            Assert.AreEqual(PortalCookieSameSiteMode.Strict, strict.SameSite, "Strict 不需要 Secure，应保留。");
            Assert.IsFalse(strict.Degraded, "Strict 未配 Secure 不应降级。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证已存在的降级原因会被保留，不会被后续归一化覆盖。</zh-CN>
        ///   <en>Verifies that an existing degradation reason is preserved and not overwritten by later normalization.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Normalize_PreservesExistingDegradeReason()
        {
            PortalCookieSecurityOptions options = PortalAuthenticationCookiePolicy.Normalize(
                false,
                PortalCookieSameSiteMode.Lax,
                PortalAuthenticationCookiePolicy.ReasonUnparsableSameSite);

            Assert.IsTrue(options.Degraded, "传入非空原因即代表已降级。");
            Assert.AreEqual(
                PortalAuthenticationCookiePolicy.ReasonUnparsableSameSite,
                options.DegradeReason,
                "已有降级原因应被保留。");
            Assert.AreEqual(PortalCookieSameSiteMode.Lax, options.SameSite, "Lax 合法，不应被进一步降级。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证四个受控取值按不区分大小写解析成功。</zh-CN>
        ///   <en>Verifies that the four controlled values parse successfully case-insensitively.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void TryParseSameSite_AcceptsControlledValuesCaseInsensitively()
        {
            // <lang>
            //   <zh-CN>逐组断言：输出模式与返回值都必须正确，避免宽松匹配把未知文本解释成合法模式。</zh-CN>
            //   <en>Assert each pair: both the output mode and the return value must be correct so loose matching cannot turn unknown text into a valid mode.</en>
            // </lang>
            PortalCookieSameSiteMode mode;

            Assert.IsTrue(PortalAuthenticationCookiePolicy.TryParseSameSite("Unset", out mode));
            Assert.AreEqual(PortalCookieSameSiteMode.Unset, mode);

            Assert.IsTrue(PortalAuthenticationCookiePolicy.TryParseSameSite("lax", out mode));
            Assert.AreEqual(PortalCookieSameSiteMode.Lax, mode);

            Assert.IsTrue(PortalAuthenticationCookiePolicy.TryParseSameSite("STRICT", out mode));
            Assert.AreEqual(PortalCookieSameSiteMode.Strict, mode);

            Assert.IsTrue(PortalAuthenticationCookiePolicy.TryParseSameSite("none", out mode));
            Assert.AreEqual(PortalCookieSameSiteMode.None, mode);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证空白配置文本按 Unset 处理，且不视为降级。</zh-CN>
        ///   <en>Verifies that blank configuration text is treated as Unset and is not a degradation.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void TryParseSameSite_TreatsBlankAsUnset()
        {
            PortalCookieSameSiteMode mode;

            // <lang>
            //   <zh-CN>空引用与空白串都应回落到 Unset，因为默认本就不下发该属性。</zh-CN>
            //   <en>Both null and whitespace fall back to Unset because the default already omits the attribute.</en>
            // </lang>
            Assert.IsTrue(PortalAuthenticationCookiePolicy.TryParseSameSite(null, out mode));
            Assert.AreEqual(PortalCookieSameSiteMode.Unset, mode);

            Assert.IsTrue(PortalAuthenticationCookiePolicy.TryParseSameSite("   ", out mode));
            Assert.AreEqual(PortalCookieSameSiteMode.Unset, mode);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证未知取值被拒绝，不猜测为任何兼容模式。</zh-CN>
        ///   <en>Verifies that unknown values are rejected instead of being guessed into a compatibility mode.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void TryParseSameSite_RejectsUnknownValue()
        {
            PortalCookieSameSiteMode mode;

            Assert.IsFalse(PortalAuthenticationCookiePolicy.TryParseSameSite("Bogus", out mode));
            Assert.AreEqual(PortalCookieSameSiteMode.Unset, mode, "未知取值应回落为 Unset。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证降级原因代码为稳定非空常量，便于诊断与日志比对。</zh-CN>
        ///   <en>Verifies that degradation reason codes are stable non-empty constants for diagnostics and log comparison.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ReasonCodes_AreStableNonEmptyConstants()
        {
            // <lang>
            //   <zh-CN>原因代码会被写入诊断，保持稳定非空才能被运维检索。</zh-CN>
            //   <en>Reason codes are written to diagnostics, so they must stay stable and non-empty to remain searchable by operators.</en>
            // </lang>
            Assert.AreNotEqual(string.Empty, PortalAuthenticationCookiePolicy.ReasonUnparsableSameSite);
            Assert.AreNotEqual(string.Empty, PortalAuthenticationCookiePolicy.ReasonNoneRequiresSecure);
            Assert.AreNotEqual(string.Empty, PortalAuthenticationCookiePolicy.ReasonSettingsReadFailed);
        }
    }
}
