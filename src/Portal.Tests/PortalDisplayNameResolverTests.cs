using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>`PortalDisplayNameResolver` 单元测试：覆盖中文侧、英文侧、其它语言 JSON 的三级键匹配，
    ///   以及坏 JSON / 缺列 / 未知文化三类降级路径。</zh-CN>
    ///   <en>Unit tests for `PortalDisplayNameResolver`: cover the Chinese side, the English side, the three-tier JSON key lookup
    ///   for other languages, and the three degradation paths of broken JSON, missing columns, and unknown cultures.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>每条测试都显式传入目标文化，**不依赖也不修改**当前线程的界面文化：否则测试会随宿主机区域设置漂移，
    ///   在中文机器上恰好通过、在英文机器上恰好失败 —— 那正是这类本地化代码最容易假装通过的地方。</zh-CN>
    ///   <en>Every test passes the target culture explicitly and neither depends on nor mutates the current thread UI culture:
    ///   otherwise tests drift with the host's regional settings, passing on a Chinese machine and failing on an English one —
    ///   precisely where localization code is most likely to appear to pass.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public class PortalDisplayNameResolverTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>中文文化取中文列，英文列即使存在也不参与。</zh-CN>
        ///   <en>Chinese cultures take the Chinese column; the English column does not participate even when present.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Resolve_ChineseCulture_ReturnsChineseColumn()
        {
            string diagnostic;
            string resolved = PortalDisplayNameResolver.Resolve("首页", "Home", "{\"ja-JP\":\"ホーム\"}", new CultureInfo("zh-CN"), out diagnostic);

            Assert.AreEqual("首页", resolved);
            Assert.AreEqual(string.Empty, diagnostic, "正常命中不应产生降级原因。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>`zh-TW` 等中文区域变体同样落在中文侧：判定用两字母语言码而非完整文化名。</zh-CN>
        ///   <en>Regional Chinese variants such as `zh-TW` also land on the Chinese side: the decision uses the two-letter language code rather than the full culture name.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Resolve_ChineseRegionalVariant_ReturnsChineseColumn()
        {
            string resolved = PortalDisplayNameResolver.Resolve("首页", "Home", null, new CultureInfo("zh-TW"), out _);

            Assert.AreEqual("首页", resolved);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>英文文化取英文列。</zh-CN>
        ///   <en>English cultures take the English column.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Resolve_EnglishCulture_ReturnsEnglishColumn()
        {
            string resolved = PortalDisplayNameResolver.Resolve("首页", "Home", null, new CultureInfo("en-US"), out _);

            Assert.AreEqual("Home", resolved);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>`en-GB` 等英文区域变体同样落在英文侧。</zh-CN>
        ///   <en>Regional English variants such as `en-GB` also land on the English side.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Resolve_EnglishRegionalVariant_ReturnsEnglishColumn()
        {
            string resolved = PortalDisplayNameResolver.Resolve("首页", "Home", null, new CultureInfo("en-GB"), out _);

            Assert.AreEqual("Home", resolved);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>其它文化按完整文化名命中 JSON 键。</zh-CN>
        ///   <en>Other cultures hit the JSON key by full culture name.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Resolve_OtherCultureWithExactJsonKey_ReturnsJsonValue()
        {
            string resolved = PortalDisplayNameResolver.Resolve("首页", "Home", "{\"ja-JP\":\"ホーム\",\"fr-FR\":\"Accueil\"}", new CultureInfo("ja-JP"), out _);

            Assert.AreEqual("ホーム", resolved);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>完整文化名未登记时，退化到两字母语言码命中（管理员只填了 `ja`）。</zh-CN>
        ///   <en>When the full culture name is absent, the lookup degrades to the two-letter language code (the administrator filled in only `ja`).</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Resolve_OtherCultureWithoutExactKey_FallsBackToLanguageCode()
        {
            string resolved = PortalDisplayNameResolver.Resolve("首页", "Home", "{\"ja\":\"ホーム\"}", new CultureInfo("ja-JP"), out _);

            Assert.AreEqual("ホーム", resolved);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>地区变体（fr-CA）未登记时，退化到父文化名（fr）。</zh-CN>
        ///   <en>When a regional variant (fr-CA) is absent, the lookup degrades to the parent culture name (fr).</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Resolve_OtherCultureRegionalVariant_FallsBackToParentCulture()
        {
            string resolved = PortalDisplayNameResolver.Resolve("首页", "Home", "{\"fr\":\"Accueil\"}", new CultureInfo("fr-CA"), out _);

            Assert.AreEqual("Accueil", resolved);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>JSON 键的大小写差异不应导致取不到值。</zh-CN>
        ///   <en>Casing differences in JSON keys must not lose the value.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Resolve_OtherCultureWithDifferentlyCasedKey_MatchesIgnoringCase()
        {
            string resolved = PortalDisplayNameResolver.Resolve("首页", "Home", "{\"ja-jp\":\"ホーム\"}", new CultureInfo("ja-JP"), out _);

            Assert.AreEqual("ホーム", resolved);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>中文列缺失时，中文文化回落到英文列，而不是显示空标签。</zh-CN>
        ///   <en>When the Chinese column is missing, Chinese cultures fall back to the English column rather than showing an empty label.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Resolve_ChineseCultureWithBlankChinese_FallsBackToEnglish()
        {
            string resolved = PortalDisplayNameResolver.Resolve("   ", "Home", null, new CultureInfo("zh-CN"), out _);

            Assert.AreEqual("Home", resolved);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>英文列缺失时，英文文化回落到中文列。</zh-CN>
        ///   <en>When the English column is missing, English cultures fall back to the Chinese column.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Resolve_EnglishCultureWithBlankEnglish_FallsBackToChinese()
        {
            string resolved = PortalDisplayNameResolver.Resolve("首页", null, null, new CultureInfo("en-US"), out _);

            Assert.AreEqual("首页", resolved);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>未知文化且 JSON 无对应键时回落到中文列，且不产生"JSON 损坏"诊断 —— 缺翻译不等于数据坏。</zh-CN>
        ///   <en>An unknown culture with no matching JSON key falls back to the Chinese column and must not raise a "JSON damaged"
        ///   diagnostic — a missing translation is not damaged data.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Resolve_UnknownCultureWithoutJsonKey_FallsBackToChineseWithoutDiagnostic()
        {
            string diagnostic;
            string resolved = PortalDisplayNameResolver.Resolve("首页", "Home", "{\"ja-JP\":\"ホーム\"}", new CultureInfo("de-DE"), out diagnostic);

            Assert.AreEqual("首页", resolved);
            Assert.AreEqual(string.Empty, diagnostic, "缺翻译属于正常回退，不应与 JSON 损坏混为一谈。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>**负向**：JSON 文本损坏时回落到中文列，并通过诊断通道暴露原因，而不是抛异常或渲染空标签。</zh-CN>
        ///   <en>**Negative**: when the JSON text is damaged it falls back to the Chinese column and exposes the reason through the
        ///   diagnostic channel, rather than throwing or rendering an empty label.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Resolve_MalformedJson_FallsBackToChineseAndReportsDiagnostic()
        {
            string diagnostic;
            string resolved = PortalDisplayNameResolver.Resolve("首页", "Home", "{not-json", new CultureInfo("ja-JP"), out diagnostic);

            Assert.AreEqual("首页", resolved);
            Assert.IsTrue(diagnostic.StartsWith(PortalDisplayNameResolver.JsonInvalidReasonPrefix, StringComparison.Ordinal),
                "损坏的 JSON 必须产生带稳定前缀的诊断原因，便于运维检索。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>**负向**：JSON 是数组（管理员填错形态）时同样安全回落并给出诊断。</zh-CN>
        ///   <en>**Negative**: when the JSON is an array (an administrator filled in the wrong shape) it also degrades safely and reports a diagnostic.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Resolve_JsonArrayInsteadOfObject_FallsBackToChineseAndReportsDiagnostic()
        {
            string diagnostic;
            string resolved = PortalDisplayNameResolver.Resolve("首页", "Home", "[\"ja-JP\",\"ホーム\"]", new CultureInfo("ja-JP"), out diagnostic);

            Assert.AreEqual("首页", resolved);
            Assert.AreNotEqual(string.Empty, diagnostic, "形态错误必须可诊断，否则脏数据会长期静默。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>**负向**：所有候选都为空时返回空字符串而不是 `null`，保证渲染层不会拿到 null 参与拼接。</zh-CN>
        ///   <en>**Negative**: when every candidate is blank it returns an empty string rather than `null`, so the render layer never concatenates a null.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Resolve_AllCandidatesBlank_ReturnsEmptyString()
        {
            string resolved = PortalDisplayNameResolver.Resolve(null, "   ", null, new CultureInfo("zh-CN"), out _);

            Assert.AreEqual(string.Empty, resolved);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>空白 JSON 被视为"没有其它语言"，既不算命中也不算损坏。</zh-CN>
        ///   <en>Blank JSON counts as "no other languages": neither a hit nor damage.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Resolve_BlankJson_TreatedAsNoOtherLanguages()
        {
            string diagnostic;
            string resolved = PortalDisplayNameResolver.Resolve("首页", "Home", "   ", new CultureInfo("ja-JP"), out diagnostic);

            Assert.AreEqual("首页", resolved);
            Assert.AreEqual(string.Empty, diagnostic, "空白 JSON 与损坏 JSON 必须区分，否则诊断会被噪声淹没。");
        }
    }
}
