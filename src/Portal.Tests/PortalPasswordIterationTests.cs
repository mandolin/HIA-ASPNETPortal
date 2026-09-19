using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>覆盖密码哈希迭代次数策略纯决策逻辑的契约测试。</zh-CN>
    ///   <en>Contract tests covering the pure decision logic of the password-hash iteration-count policy.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>测试只调用 PortalPasswordIterationPolicy，不读取配置文件、不访问数据库、不接触任何密码、盐或哈希材料；每个用例前后都会恢复默认提供器，避免静态委托状态泄漏。</zh-CN>
    ///   <en>The tests invoke only PortalPasswordIterationPolicy, read no configuration file, access no database, and touch no password, salt, or hash material; the default provider is restored around each test so static delegate state cannot leak.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalPasswordIterationTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>每个测试前清除提供器，确保用例从组件硬下限开始。</zh-CN>
        ///   <en>Clear the provider before each test so cases start from the component hard lower bound.</en>
        /// </lang>
        /// </summary>
        [TestInitialize]
        public void ResetTargetProviderBeforeTest()
        {
            PortalPasswordIterationPolicy.ConfigureTargetProvider(null);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>每个测试后再次清除提供器，避免后续用例继承本测试注入的成本。</zh-CN>
        ///   <en>Clear the provider again after each test so later cases do not inherit the cost injected by this test.</en>
        /// </lang>
        /// </summary>
        [TestCleanup]
        public void ResetTargetProviderAfterTest()
        {
            PortalPasswordIterationPolicy.ConfigureTargetProvider(null);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证未注入提供器时回落到硬下限，与组件当前默认成本一致。</zh-CN>
        ///   <en>Verifies that the hard lower bound is used without an injected provider, matching the component's current default cost.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ResolveTargetIterationCount_ReturnsHardLowerBoundWithoutProvider()
        {
            Assert.AreEqual(
                PortalPasswordIterationPolicy.MinimumIterationCount,
                PortalPasswordIterationPolicy.ResolveTargetIterationCount(),
                "未注入提供器时应直接使用硬下限。");
            Assert.AreEqual(210000, PortalPasswordIterationPolicy.MinimumIterationCount, "硬下限应等于当前默认成本。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证低于硬下限的配置被回落，哈希强度绝不因配置而降低。</zh-CN>
        ///   <en>Verifies that configuration below the hard lower bound is clamped up so hashing strength is never weakened by configuration.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ResolveTargetIterationCount_ClampsConfiguredValueBelowLowerBound()
        {
            // <lang>
            //   <zh-CN>注入一个明显更低且不安全的成本，断言解析结果不会被采纳。</zh-CN>
            //   <en>Inject a clearly lower and unsafe cost and assert the resolved result does not adopt it.</en>
            // </lang>
            PortalPasswordIterationPolicy.ConfigureTargetProvider(() => 1000);

            Assert.AreEqual(
                PortalPasswordIterationPolicy.MinimumIterationCount,
                PortalPasswordIterationPolicy.ResolveTargetIterationCount(),
                "低于下限的配置必须回落到下限。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证更高的配置值被采纳，支持后续渐进提高成本。</zh-CN>
        ///   <en>Verifies that a higher configured value is adopted, supporting later gradual cost increases.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ResolveTargetIterationCount_UsesHigherConfiguredValue()
        {
            PortalPasswordIterationPolicy.ConfigureTargetProvider(() => 310000);

            Assert.AreEqual(
                310000,
                PortalPasswordIterationPolicy.ResolveTargetIterationCount(),
                "高于下限的配置应被采纳。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证提供器抛异常时 fail-safe 回落到硬下限，不向上抛出。</zh-CN>
        ///   <en>Verifies that a throwing provider fails safely back to the hard lower bound instead of rethrowing.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void ResolveTargetIterationCount_FallsBackWhenProviderThrows()
        {
            // <lang>
            //   <zh-CN>模拟配置读取故障，登录路径不应因此中断。</zh-CN>
            //   <en>Simulate a configuration-read fault; the sign-in path must not be interrupted by it.</en>
            // </lang>
            PortalPasswordIterationPolicy.ConfigureTargetProvider(
                () => throw new InvalidOperationException("simulated configuration failure"));

            Assert.AreEqual(
                PortalPasswordIterationPolicy.MinimumIterationCount,
                PortalPasswordIterationPolicy.ResolveTargetIterationCount(),
                "提供器故障时应回落到硬下限。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证已持久化成本低于目标时才需要重哈希。</zh-CN>
        ///   <en>Verifies that rehashing is needed only when the persisted cost is below the target.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void NeedsRehash_IsTrueOnlyWhenStoredBelowTarget()
        {
            // <lang>
            //   <zh-CN>逐组断言：低于目标为真，等于或高于目标为假，避免无意义的重复写入。</zh-CN>
            //   <en>Assert each pair: below target is true, equal or above is false, avoiding pointless repeated writes.</en>
            // </lang>
            Assert.IsTrue(
                PortalPasswordIterationPolicy.NeedsRehash(100000, 210000),
                "低于目标成本时应需要重哈希。");
            Assert.IsFalse(
                PortalPasswordIterationPolicy.NeedsRehash(210000, 210000),
                "等于目标成本时不应重哈希。");
            Assert.IsFalse(
                PortalPasswordIterationPolicy.NeedsRehash(310000, 210000),
                "高于目标成本时不应重哈希。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证已持久化成本非正（缺失或损坏）时不触发重哈希，交由既有校验路径处理。</zh-CN>
        ///   <en>Verifies that a non-positive persisted cost never triggers rehash and is left to the existing validation path.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void NeedsRehash_IsFalseForNonPositiveStoredCount()
        {
            Assert.IsFalse(
                PortalPasswordIterationPolicy.NeedsRehash(0, 210000),
                "零值代表缺失或损坏，不应触发重哈希。");
            Assert.IsFalse(
                PortalPasswordIterationPolicy.NeedsRehash(-1, 210000),
                "负值代表缺失或损坏，不应触发重哈希。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证单参重载使用当前解析出的目标成本，默认配置下不触发重哈希。</zh-CN>
        ///   <en>Verifies that the single-argument overload uses the currently resolved target cost and triggers no rehash under default configuration.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void NeedsRehash_SingleArgumentUsesResolvedTarget()
        {
            // <lang>
            //   <zh-CN>未注入提供器时目标即硬下限，等于该值的存量凭据不应触发写入。</zh-CN>
            //   <en>Without an injected provider the target is the hard lower bound, so credentials already at that value must not trigger a write.</en>
            // </lang>
            Assert.IsFalse(
                PortalPasswordIterationPolicy.NeedsRehash(210000),
                "默认配置下等于硬下限的凭据不应触发重哈希。");

            // <lang>
            //   <zh-CN>注入更高成本后，同一凭据应当需要重哈希。</zh-CN>
            //   <en>After a higher cost is injected, the same credential should need rehashing.</en>
            // </lang>
            PortalPasswordIterationPolicy.ConfigureTargetProvider(() => 310000);
            Assert.IsTrue(
                PortalPasswordIterationPolicy.NeedsRehash(210000),
                "目标提高后，低于目标的凭据应需要重哈希。");
        }
    }
}
