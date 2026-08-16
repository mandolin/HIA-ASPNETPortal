using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>覆盖门户密码策略纯内存边界的契约测试。</zh-CN>
    ///   <en>Contract tests covering pure in-memory boundaries of the Portal password policy.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>测试使用公开低敏样例字符串，不读取真实账号、不保存凭据、不访问数据库，也不运行登录流程。</zh-CN>
    ///   <en>The tests use public low-sensitivity sample strings, do not read real accounts, do not store credentials, do not access databases, and do not run the sign-in flow.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalPasswordPolicyTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>每个测试前恢复默认策略提供器，避免静态委托状态从上一测试泄漏。</zh-CN>
        ///   <en>Restores the default policy provider before each test so static delegate state cannot leak from a previous test.</en>
        /// </lang>
        /// </summary>
        [TestInitialize]
        public void ResetPolicyProviderBeforeTest()
        {
            // <lang>
            //   <zh-CN>null 提供器表示使用组件默认策略；这里不传入任何密码或账号上下文。</zh-CN>
            //   <en>A null provider means the component default policy is used; no password or account context is passed here.</en>
            // </lang>
            PortalPasswordPolicy.ConfigureOptionsProvider(null);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>每个测试后再次恢复默认策略提供器，避免后续测试或调用方继承本测试的运行期策略。</zh-CN>
        ///   <en>Restores the default policy provider after each test so later tests or callers do not inherit this test's runtime policy.</en>
        /// </lang>
        /// </summary>
        [TestCleanup]
        public void ResetPolicyProviderAfterTest()
        {
            // <lang>
            //   <zh-CN>清理动作只重置静态委托，不接触配置、数据库或真实用户状态。</zh-CN>
            //   <en>The cleanup action resets only the static delegate and does not touch configuration, databases, or real user state.</en>
            // </lang>
            PortalPasswordPolicy.ConfigureOptionsProvider(null);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证默认策略接受满足长度和类别要求的公开样例候选。</zh-CN>
        ///   <en>Verifies that the default policy accepts a public sample candidate satisfying length and category requirements.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void TryValidate_AcceptsStrongSampleUnderDefaultPolicy()
        {
            // <lang>
            //   <zh-CN>样例候选是公开 fixture 字符串，只用于类别和长度验证，不代表真实密码。</zh-CN>
            //   <en>The sample candidate is a public fixture string used only for category and length validation; it is not a real password.</en>
            // </lang>
            const string SampleCandidate = "Fixture9!";

            // <lang>
            //   <zh-CN>消息变量接收用户可见失败说明；通过场景应保持为空。</zh-CN>
            //   <en>The message variable receives display-safe failure text and should remain empty in a passing scenario.</en>
            // </lang>
            string message;

            // <lang>
            //   <zh-CN>验证结果记录默认策略对公开样例的判断，生命周期仅限本测试。</zh-CN>
            //   <en>The validation result records the default policy decision for the public sample and lives only for this test.</en>
            // </lang>
            bool isValid = PortalPasswordPolicy.TryValidate(SampleCandidate, out message);

            // <lang>
            //   <zh-CN>通过断言固定默认策略的最小可用强度入口。</zh-CN>
            //   <en>The true assertion pins the default policy's minimum usable-strength entrypoint.</en>
            // </lang>
            Assert.IsTrue(isValid);

            // <lang>
            //   <zh-CN>消息断言确保通过场景不会回显样例候选或产生误导性失败文本。</zh-CN>
            //   <en>The message assertion ensures a passing scenario does not echo the sample candidate or produce misleading failure text.</en>
            // </lang>
            Assert.AreEqual(string.Empty, message);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证运行期策略提供器不能把组件硬下限降到不安全值。</zh-CN>
        ///   <en>Verifies that a runtime policy provider cannot lower component hard bounds to unsafe values.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void GetEffectiveOptions_NormalizesUnsafeProviderValuesToHardBounds()
        {
            // <lang>
            //   <zh-CN>提供器返回故意不安全的长度和类别数，用于证明策略层会施加硬下限和四类上限。</zh-CN>
            //   <en>The provider returns intentionally unsafe length and category values to prove the policy layer applies hard lower bounds and the four-category upper bound.</en>
            // </lang>
            PortalPasswordPolicy.ConfigureOptionsProvider(
                () => new PortalPasswordPolicyOptions(1, 99, false, false));

            // <lang>
            //   <zh-CN>有效选项变量保存经过规范化的策略，不包含密码、用户标识或配置秘密。</zh-CN>
            //   <en>The effective-options variable stores the normalized policy and contains no password, user identifier, or configuration secret.</en>
            // </lang>
            PortalPasswordPolicyOptions effectiveOptions = PortalPasswordPolicy.GetEffectiveOptions();

            // <lang>
            //   <zh-CN>最小长度断言固定组件层 8 位硬下限。</zh-CN>
            //   <en>The minimum-length assertion pins the component-layer 8-character hard lower bound.</en>
            // </lang>
            Assert.AreEqual(PortalPasswordPolicy.MinimumLength, effectiveOptions.MinimumLength);

            // <lang>
            //   <zh-CN>类别数量断言固定最多四类字符的可表达上限，同时不低于基线 3 类。</zh-CN>
            //   <en>The category-count assertion pins the expressible upper bound of four character categories while remaining no lower than the baseline of three.</en>
            // </lang>
            Assert.AreEqual(4, effectiveOptions.RequiredCategoryCount);

            // <lang>
            //   <zh-CN>开关断言说明运行期配置仍可关闭字典和上下文词检查，但不能降低硬下限。</zh-CN>
            //   <en>The switch assertions show runtime configuration may still disable dictionary and context-term checks but cannot lower hard bounds.</en>
            // </lang>
            Assert.IsFalse(effectiveOptions.WeakDictionaryEnabled);
            Assert.IsFalse(effectiveOptions.DisallowContextTerms);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证启用上下文词限制时，包含账号上下文片段的候选会被拒绝。</zh-CN>
        ///   <en>Verifies that, when context-term restriction is enabled, a candidate containing an account-context fragment is rejected.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void TryValidate_RejectsCandidateContainingAccountContextTerm()
        {
            // <lang>
            //   <zh-CN>策略提供器提高最小长度并关闭弱口令字典，以便本测试只聚焦账号上下文词判断。</zh-CN>
            //   <en>The policy provider raises minimum length and disables the weak dictionary so this test focuses only on account-context-term handling.</en>
            // </lang>
            PortalPasswordPolicy.ConfigureOptionsProvider(
                () => new PortalPasswordPolicyOptions(12, 3, false, true));

            // <lang>
            //   <zh-CN>样例候选包含公开 fixture 上下文词，不代表真实用户密码。</zh-CN>
            //   <en>The sample candidate contains a public fixture context term and does not represent a real user password.</en>
            // </lang>
            const string SampleCandidate = "PortalUser2026!";

            // <lang>
            //   <zh-CN>上下文词数组使用公开 fixture 词，不来自真实账号、邮箱或员工号。</zh-CN>
            //   <en>The context-term array uses public fixture words and does not come from a real account, email, or employee code.</en>
            // </lang>
            string[] contextTerms =
            {
                "portaluser"
            };

            // <lang>
            //   <zh-CN>消息变量接收预期的上下文词拒绝说明，不应包含样例候选本身。</zh-CN>
            //   <en>The message variable receives the expected context-term rejection text and should not contain the sample candidate itself.</en>
            // </lang>
            string message;

            // <lang>
            //   <zh-CN>验证结果记录策略对包含上下文词样例的拒绝判断。</zh-CN>
            //   <en>The validation result records the policy rejection decision for a sample containing a context term.</en>
            // </lang>
            bool isValid = PortalPasswordPolicy.TryValidate(SampleCandidate, contextTerms, out message);

            // <lang>
            //   <zh-CN>失败断言固定账号上下文词不能出现在候选密码中的安全边界。</zh-CN>
            //   <en>The false assertion pins the security boundary that account-context terms must not appear in a candidate password.</en>
            // </lang>
            Assert.IsFalse(isValid);

            // <lang>
            //   <zh-CN>消息断言只检查低敏原因类别，避免把候选字符串本身写入断言期望值。</zh-CN>
            //   <en>The message assertion checks only the low-sensitivity reason category and avoids writing the candidate string into expected text.</en>
            // </lang>
            StringAssert.Contains(message, "账号相关信息");
        }
    }
}
