using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>覆盖门户静态导航注册表与导航入口值对象的纯内存契约测试。</zh-CN>
    ///   <en>Pure in-memory contract tests for the Portal static navigation registry and navigation-entry value object.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>测试不解析真实 URL、不读取配置、不判断当前用户授权，也不启动 Web Forms 页面。</zh-CN>
    ///   <en>The tests do not resolve real URLs, read configuration, authorize the current user, or start Web Forms pages.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalNavigationRegistryTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>验证按 key 查询会修剪输入并忽略大小写。</zh-CN>
        ///   <en>Verifies that key lookup trims input and ignores casing.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void FindByKey_TrimsAndMatchesRegisteredKeyCaseInsensitively()
        {
            // <lang>
            //   <zh-CN>首个注册入口来自静态注册表，只作为已知合法 key 的来源，不固定注册表总数。</zh-CN>
            //   <en>The first registered entry comes from the static registry and is used only as a known valid key source, without pinning total registry count.</en>
            // </lang>
            PortalNavigationEntry registeredEntry = PortalNavigationRegistry.GetEntries().First();

            // <lang>
            //   <zh-CN>查询 key 故意加入空白和大小写漂移，以覆盖消费边界常见输入形态。</zh-CN>
            //   <en>The query key intentionally adds whitespace and casing drift to cover common input shapes at consuming boundaries.</en>
            // </lang>
            string queryKey = " " + registeredEntry.EntryKey.ToUpperInvariant() + " ";

            // <lang>
            //   <zh-CN>查找结果保存注册表按稳定 key 命中的入口，不代表当前用户已获访问权限。</zh-CN>
            //   <en>The lookup result stores the entry matched by stable key and does not mean the current user is authorized to access it.</en>
            // </lang>
            PortalNavigationEntry foundEntry = PortalNavigationRegistry.FindByKey(queryKey);

            // <lang>
            //   <zh-CN>非空断言固定已注册 key 能被 trim/case 查询命中。</zh-CN>
            //   <en>The non-null assertion pins that a registered key can be found through trimmed case-insensitive lookup.</en>
            // </lang>
            Assert.IsNotNull(foundEntry);

            // <lang>
            //   <zh-CN>key 断言固定返回的是同一个稳定入口，而不是按显示名或目标路径猜测的其它入口。</zh-CN>
            //   <en>The key assertion pins that the returned item is the same stable entry rather than another entry guessed by display name or target path.</en>
            // </lang>
            Assert.AreEqual(registeredEntry.EntryKey, foundEntry.EntryKey);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证空白或未知 key 不会被注册表猜测匹配。</zh-CN>
        ///   <en>Verifies that blank or unknown keys are not guessed into registry matches.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void FindByKey_ReturnsNullForBlankOrUnknownKey()
        {
            // <lang>
            //   <zh-CN>空白查询结果记录缺失 key 的稳定回退。</zh-CN>
            //   <en>The blank lookup result records the stable fallback for a missing key.</en>
            // </lang>
            PortalNavigationEntry blankResult = PortalNavigationRegistry.FindByKey(" ");

            // <lang>
            //   <zh-CN>未知查询结果使用测试专属 key，避免未来真实入口偶然碰撞。</zh-CN>
            //   <en>The unknown lookup result uses a test-only key to avoid accidental collision with future real entries.</en>
            // </lang>
            PortalNavigationEntry unknownResult = PortalNavigationRegistry.FindByKey("UnitTest.Unknown.Navigation.Entry");

            // <lang>
            //   <zh-CN>空白断言固定注册表不会从显示名、目标路径或其它字段猜测入口。</zh-CN>
            //   <en>The blank assertion pins that the registry does not guess entries from display names, targets, or other fields.</en>
            // </lang>
            Assert.IsNull(blankResult);

            // <lang>
            //   <zh-CN>未知 key 断言固定未注册入口保持 null，不被隐式创建。</zh-CN>
            //   <en>The unknown-key assertion pins that an unregistered entry remains null and is not implicitly created.</en>
            // </lang>
            Assert.IsNull(unknownResult);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证导航入口会复制、修剪、去空项、去重并只读化依赖列表。</zh-CN>
        ///   <en>Verifies that a navigation entry copies, trims, removes blanks, deduplicates, and makes dependency lists read-only.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Constructor_NormalizesDependencyListsAndMakesThemReadOnly()
        {
            // <lang>
            //   <zh-CN>角色输入模拟配置中重复、空白和大小写漂移的角色依赖。</zh-CN>
            //   <en>The role input simulates duplicated, blank, and casing-drift role dependencies from configuration.</en>
            // </lang>
            string[] roleDependencies =
            {
                " Admins ",
                "",
                "admins",
                "All Users"
            };

            // <lang>
            //   <zh-CN>权限输入使用公开权限常量和重复项，证明导航元数据只规范化提示依赖。</zh-CN>
            //   <en>The permission input uses public permission constants and duplicates to prove navigation metadata only normalizes hint dependencies.</en>
            // </lang>
            string[] permissionDependencies =
            {
                PortalPermissionKeys.BusinessApplicationSubmit,
                " business.application.submit "
            };

            // <lang>
            //   <zh-CN>导航入口变量是本测试专属值对象，不注册到全局静态注册表。</zh-CN>
            //   <en>The navigation-entry variable is a test-only value object and is not registered into the global static registry.</en>
            // </lang>
            PortalNavigationEntry entry = new PortalNavigationEntry(
                " UnitTest.Entry ",
                PortalNavigationEntryKind.AdminPage,
                "测试入口",
                "Test Entry",
                "~/Admin/UnitTest.aspx",
                PortalNavigationVisibilityMode.AdminOnly,
                PortalNavigationLifecycleState.Draft,
                10,
                roleDependencies,
                permissionDependencies,
                new[] { " EnterpriseWorkbench ", "enterpriseworkbench" },
                new[] { " test ", "TEST" },
                "unit-test");

            // <lang>
            //   <zh-CN>key 断言固定稳定入口键会修剪外围空白。</zh-CN>
            //   <en>The key assertion pins that the stable entry key trims surrounding whitespace.</en>
            // </lang>
            Assert.AreEqual("UnitTest.Entry", entry.EntryKey);

            // <lang>
            //   <zh-CN>角色断言固定依赖列表按首次出现保留展示大小写并去重。</zh-CN>
            //   <en>The role assertion pins that the dependency list preserves first-occurrence display casing while deduplicating.</en>
            // </lang>
            CollectionAssert.AreEqual(new[] { "Admins", "All Users" }, entry.RequiredRoles.ToArray());

            // <lang>
            //   <zh-CN>权限断言固定权限依赖也按 trim/case 规则去重。</zh-CN>
            //   <en>The permission assertion pins that permission dependencies are also deduplicated by trim/case rules.</en>
            // </lang>
            CollectionAssert.AreEqual(new[] { PortalPermissionKeys.BusinessApplicationSubmit }, entry.RequiredPermissionKeys.ToArray());

            // <lang>
            //   <zh-CN>只读断言固定构造后外部调用方不能修改依赖列表。</zh-CN>
            //   <en>The read-only assertion pins that external callers cannot mutate dependency lists after construction.</en>
            // </lang>
            Assert.Throws<NotSupportedException>(
                () => entry.RequiredRoles.Add("Operators"),
                string.Empty,
                string.Empty);
        }
    }
}
