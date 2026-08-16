using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>覆盖数据库 profile 值对象的纯内存契约测试。</zh-CN>
    ///   <en>Pure in-memory contract tests for the database profile value object.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>这些测试只构造值对象，不注册 provider、不创建连接、不打开数据库，也不读取真实连接串。</zh-CN>
    ///   <en>These tests construct value objects only; they do not register providers, create connections, open databases, or read real connection strings.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalDatabaseProfileTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>验证构造器会修剪稳定标识、默认环境名，并原样保留调用方提供的连接文本。</zh-CN>
        ///   <en>Verifies that the constructor trims stable identifiers, defaults the environment name, and preserves caller-supplied connection text unchanged.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Constructor_TrimsIdentifiersDefaultsEnvironmentAndPreservesConnectionText()
        {
            // <lang>
            //   <zh-CN>样例连接文本不包含密码、Token 或生产主机，仅作为非空敏感字段占位。</zh-CN>
            //   <en>The sample connection text contains no password, token, or production host and acts only as a non-empty sensitive-field placeholder.</en>
            // </lang>
            const string SampleConnectionText = "Server=(local);Database=Portal;Integrated Security=True;";

            // <lang>
            //   <zh-CN>profile 变量保存构造后的值对象，不会创建或打开任何数据库连接。</zh-CN>
            //   <en>The profile variable stores the constructed value object and does not create or open any database connection.</en>
            // </lang>
            PortalDatabaseProfile profile = new PortalDatabaseProfile(
                " Portal ",
                " System.Data.SqlClient ",
                SampleConnectionText,
                " ",
                PortalDatabasePurpose.PrimaryPortal);

            // <lang>
            //   <zh-CN>逻辑名称断言固定外部配置键会被修剪后保存。</zh-CN>
            //   <en>The logical-name assertion pins that external configuration keys are stored after trimming.</en>
            // </lang>
            Assert.AreEqual("Portal", profile.LogicalName);

            // <lang>
            //   <zh-CN>provider 断言固定 invariant 名称只修剪外围空白，不改变大小写文本。</zh-CN>
            //   <en>The provider assertion pins that the invariant name trims surrounding whitespace without changing casing text.</en>
            // </lang>
            Assert.AreEqual(PortalDatabaseProviderNames.SqlServer, profile.ProviderInvariantName);

            // <lang>
            //   <zh-CN>连接文本断言固定构造器不改写 provider 特定的连接字符串内容。</zh-CN>
            //   <en>The connection-text assertion pins that the constructor does not rewrite provider-specific connection-string content.</en>
            // </lang>
            Assert.AreEqual(SampleConnectionText, profile.ConnectionString);

            // <lang>
            //   <zh-CN>环境断言固定空白环境名会回退到既有 dev 默认值。</zh-CN>
            //   <en>The environment assertion pins that a blank environment name falls back to the established dev default.</en>
            // </lang>
            Assert.AreEqual("dev", profile.EnvironmentName);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证 provider 匹配会修剪查询值并按序号不区分大小写比较。</zh-CN>
        ///   <en>Verifies that provider matching trims the query value and compares ordinally case-insensitively.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void UsesProvider_TrimsQueryAndComparesCaseInsensitively()
        {
            // <lang>
            //   <zh-CN>profile 使用公开 provider invariant 和低敏样例连接文本，仅测试值对象行为。</zh-CN>
            //   <en>The profile uses a public provider invariant and low-sensitivity sample connection text to test value-object behavior only.</en>
            // </lang>
            PortalDatabaseProfile profile = new PortalDatabaseProfile(
                "Portal",
                PortalDatabaseProviderNames.SqlServer,
                "Server=(local);Database=Portal;Integrated Security=True;",
                "test",
                PortalDatabasePurpose.ProviderProof);

            // <lang>
            //   <zh-CN>匹配结果保存 trim/case 查询路径的判断，不代表 provider 已注册或可连接。</zh-CN>
            //   <en>The match result stores the trim/case query-path decision and does not mean the provider is registered or connectable.</en>
            // </lang>
            bool matchesProvider = profile.UsesProvider(" system.data.sqlclient ");

            // <lang>
            //   <zh-CN>断言固定 profile 层只负责 invariant 文本匹配，不负责实际连接能力。</zh-CN>
            //   <en>The assertion pins that the profile layer is responsible only for invariant text matching, not actual connectivity.</en>
            // </lang>
            Assert.IsTrue(matchesProvider);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证空白 provider invariant 会以明确参数名被拒绝。</zh-CN>
        ///   <en>Verifies that a blank provider invariant is rejected with an explicit parameter name.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Constructor_RejectsBlankProviderInvariantName()
        {
            // <lang>
            //   <zh-CN>异常变量记录值对象对缺失 provider invariant 的拒绝结果。</zh-CN>
            //   <en>The exception variable records the value object's rejection of a missing provider invariant.</en>
            // </lang>
            ArgumentException exception = Assert.Throws<ArgumentException>(
                () => new PortalDatabaseProfile(
                    "Portal",
                    " ",
                    "Server=(local);Database=Portal;Integrated Security=True;",
                    "dev",
                    PortalDatabasePurpose.PrimaryPortal),
                string.Empty,
                string.Empty);

            // <lang>
            //   <zh-CN>参数名断言固定调用方可定位的配置字段，不检查可能调整的消息文本。</zh-CN>
            //   <en>The parameter-name assertion pins the diagnosable configuration field and does not check message text that may evolve.</en>
            // </lang>
            Assert.AreEqual("providerInvariantName", exception.ParamName);
        }
    }
}
