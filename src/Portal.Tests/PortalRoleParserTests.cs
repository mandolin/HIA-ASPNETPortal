using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>覆盖旧门户分号角色列表解析器的纯内存契约测试。</zh-CN>
    ///   <en>Pure in-memory contract tests for the legacy Portal semicolon-separated role-list parser.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>角色解析器被导航、权限和旧后台入口反复依赖；本测试只使用公开示例角色名，不读取账号、数据库或 HTTP 上下文。</zh-CN>
    ///   <en>The role parser is repeatedly used by navigation, permission, and legacy administration entry points; these tests use public sample role names only and do not read accounts, databases, or HTTP context.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalRoleParserTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>验证旧格式角色字符串会去空项、去外围空白并按不区分大小写规则去重。</zh-CN>
        ///   <en>Verifies that a legacy role string removes empty entries, trims surrounding whitespace, and deduplicates case-insensitively.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Parse_TrimsEmptySegmentsAndDeduplicatesRoles()
        {
            // <lang>
            //   <zh-CN>输入字符串模拟旧数据库中常见的尾部分号、空段和大小写漂移。</zh-CN>
            //   <en>The input string simulates trailing semicolons, empty segments, and casing drift commonly found in the legacy database.</en>
            // </lang>
            string legacyRoleList = " Admins ; ; all users ;ADMINS;";

            // <lang>
            //   <zh-CN>解析结果只保留当前方法断言所需的短生命周期角色数组。</zh-CN>
            //   <en>The parse result keeps only the short-lived role array needed by the current assertion.</en>
            // </lang>
            string[] parsedRoles = PortalRoleParser.Parse(legacyRoleList);

            // <lang>
            //   <zh-CN>期望数组保留首次出现的展示大小写，固定旧兼容格式不会被 parser 重写成全局常量大小写。</zh-CN>
            //   <en>The expected array preserves the display casing of first occurrences, pinning that the parser does not rewrite legacy-compatible values into global constant casing.</en>
            // </lang>
            string[] expectedRoles =
            {
                "Admins",
                "all users"
            };

            // <lang>
            //   <zh-CN>集合断言同时覆盖空段过滤、trim、去重和顺序保留。</zh-CN>
            //   <en>The collection assertion covers empty-segment filtering, trimming, deduplication, and order preservation together.</en>
            // </lang>
            CollectionAssert.AreEqual(expectedRoles, parsedRoles);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证角色包含判断按不区分大小写规则处理旧虚拟角色。</zh-CN>
        ///   <en>Verifies that role containment handles the legacy virtual role case-insensitively.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Contains_MatchesLegacyAllUsersRoleCaseInsensitively()
        {
            // <lang>
            //   <zh-CN>角色列表使用旧门户公开角色名，不代表真实用户或真实权限授予。</zh-CN>
            //   <en>The role list uses public legacy Portal role names and does not represent a real user or real permission grant.</en>
            // </lang>
            string legacyRoleList = "Admins;All Users";

            // <lang>
            //   <zh-CN>匹配结果记录 parser 对大小写漂移查询值的判断，生命周期仅限当前断言。</zh-CN>
            //   <en>The match result records the parser decision for a casing-drift query value and lives only for the current assertion.</en>
            // </lang>
            bool containsRole = PortalRoleParser.Contains(legacyRoleList, "all users");

            // <lang>
            //   <zh-CN>断言固定“所有用户”虚拟角色继续按不区分大小写方式被识别。</zh-CN>
            //   <en>The assertion pins that the "All Users" virtual role continues to be recognized case-insensitively.</en>
            // </lang>
            Assert.IsTrue(containsRole);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证写出角色集合时会去空白、去空项和去重，但不会强制尾部分号。</zh-CN>
        ///   <en>Verifies that writing a role collection trims, removes blanks, deduplicates, and does not force a trailing semicolon.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Join_WritesTrimmedDeduplicatedSemicolonListWithoutTrailingSeparator()
        {
            // <lang>
            //   <zh-CN>输入数组模拟 UI 选择、旧配置和迁移脚本合并后的角色候选。</zh-CN>
            //   <en>The input array simulates role candidates merged from UI selection, legacy configuration, and migration scripts.</en>
            // </lang>
            string[] roleCandidates =
            {
                " Admins ",
                "",
                "All Users",
                "admins"
            };

            // <lang>
            //   <zh-CN>写出结果是可持久化的旧格式文本，但本测试只在内存中检查它。</zh-CN>
            //   <en>The written result is legacy-format text that can be persisted, but this test checks it in memory only.</en>
            // </lang>
            string joinedRoles = PortalRoleParser.Join(roleCandidates);

            // <lang>
            //   <zh-CN>断言固定输出不带尾部分号，避免调用方误把尾部分号作为必需格式。</zh-CN>
            //   <en>The assertion pins output without a trailing semicolon so callers do not mistake the trailing separator for a required format.</en>
            // </lang>
            Assert.AreEqual("Admins;All Users", joinedRoles);
        }
    }
}
