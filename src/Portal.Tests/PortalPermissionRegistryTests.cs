using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>覆盖门户权限键注册表的首批稳定单元测试。</zh-CN>
    ///   <en>First stable unit tests for the Portal permission-key registry.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>这些测试只读取 `Portal.Components` 的纯内存契约，不连接数据库、不读取配置、不创建账号，也不依赖 IIS 或 HTTP 上下文。</zh-CN>
    ///   <en>These tests read only pure in-memory contracts from `Portal.Components`; they do not connect to databases, read configuration, create accounts, or depend on IIS or HTTP context.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalPermissionRegistryTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>验证权限键集合规范化会去空白、去重，并恢复注册表中的规范大小写。</zh-CN>
        ///   <en>Verifies that permission-key collection normalization trims, deduplicates, and restores canonical registry casing.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void NormalizeDefinedKeys_TrimsDeduplicatesAndReturnsCanonicalKeys()
        {
            // <lang>
            //   <zh-CN>输入集合模拟迁移脚本、角色映射或配置文本常见的大小写漂移、重复项和空白项。</zh-CN>
            //   <en>The input collection simulates casing drift, duplicates, and blank entries commonly seen in migration scripts, role mappings, or configuration text.</en>
            // </lang>
            string[] rawPermissionKeys =
            {
                " business.application.review ",
                "Business.Application.Submit",
                "BUSINESS.APPLICATION.REVIEW",
                null,
                string.Empty
            };

            // <lang>
            //   <zh-CN>结果变量保存被测注册表返回的规范键名，生命周期仅限当前断言。</zh-CN>
            //   <en>The result variable stores canonical keys returned by the registry under test and lives only for the current assertion.</en>
            // </lang>
            string[] normalizedPermissionKeys = PortalPermissionRegistry.NormalizeDefinedKeys(rawPermissionKeys);

            // <lang>
            //   <zh-CN>期望值使用公开常量而非重复字符串，确保测试固定的是注册表契约而不是测试文件里的第二份魔法文本。</zh-CN>
            //   <en>The expected value uses public constants instead of repeated literals, so the test pins the registry contract rather than a second copy of magic text in this file.</en>
            // </lang>
            string[] expectedPermissionKeys =
            {
                PortalPermissionKeys.BusinessApplicationReview,
                PortalPermissionKeys.BusinessApplicationSubmit
            };

            // <lang>
            //   <zh-CN>集合断言同时验证去重、排序和规范大小写；任何未定义键都会在前一步直接抛出异常。</zh-CN>
            //   <en>The collection assertion verifies deduplication, ordering, and canonical casing together; any undefined key would have thrown in the previous step.</en>
            // </lang>
            CollectionAssert.AreEqual(expectedPermissionKeys, normalizedPermissionKeys);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证未知权限键不会被注册表静默接受。</zh-CN>
        ///   <en>Verifies that the registry does not silently accept an unknown permission key.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void NormalizeDefinedKey_RejectsUnknownPermissionKey()
        {
            // <lang>
            //   <zh-CN>未知键使用测试专属命名空间，避免与未来真实权限键偶然碰撞。</zh-CN>
            //   <en>The unknown key uses a test-only namespace to avoid accidental collision with future real permission keys.</en>
            // </lang>
            const string UnknownPermissionKey = "UnitTest.Unknown.Permission";

            // <lang>
            //   <zh-CN>异常变量记录注册表对未知键的拒绝结果，避免测试只验证“抛了某种异常”而不看参数语义。</zh-CN>
            //   <en>The exception variable records the registry's rejection of the unknown key so the test checks parameter semantics instead of only checking that some exception was thrown.</en>
            // </lang>
            ArgumentException exception = Assert.Throws<ArgumentException>(
                () => PortalPermissionRegistry.NormalizeDefinedKey(UnknownPermissionKey),
                string.Empty,
                string.Empty);

            // <lang>
            //   <zh-CN>参数名断言固定调用方可诊断的错误位置；消息文本保持实现可调整，不在本测试中硬编码。</zh-CN>
            //   <en>The parameter-name assertion pins the diagnosable error location for callers; the message text remains implementation-adjustable and is not hard-coded here.</en>
            // </lang>
            Assert.AreEqual("key", exception.ParamName);
        }
    }
}
