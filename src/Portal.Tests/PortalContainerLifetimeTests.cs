using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>守护 P42 修复：Unity 容器配置（UnityCfg.xml）中数据访问层注册不得再使用单例生命周期。原先的单例 DbContext 会被并发请求（含并行的 WebResource.axd 子资源）共享，导致实时安全版本/角色查询竞态失败并误签退活跃会话；改为瞬态后可彻底隔离。</zh-CN>
    ///   <en>Guards the P42 fix: the Unity container configuration (UnityCfg.xml) must no longer register the data-access layer with a singleton lifetime. The former singleton DbContext was shared by concurrent requests (including parallel WebResource.axd sub-resources), racing on real-time security-version/role queries and wrongly signing out active sessions; the transient lifetime isolates them.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该测试只读源码树中的配置文件，不启动容器、不连接数据库；若配置文件因路径变动找不到则标记为 Inconclusive，避免误报。</zh-CN>
    ///   <en>The test only reads the configuration file in the source tree; it starts no container and opens no database. If the config file cannot be located due to path changes, it is marked Inconclusive to avoid false failures.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalContainerLifetimeTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>定位源码树中的 UnityCfg.xml：从测试程序集目录向上查找含 *.sln 的解决方案根，再拼接 src/Portal/Config/UnityCfg.xml。</zh-CN>
        ///   <en>Locates UnityCfg.xml in the source tree: walk up from the test assembly directory to the solution root containing *.sln, then append src/Portal/Config/UnityCfg.xml.</en>
        /// </lang>
        /// </summary>
        private static string ResolveUnityConfigPath()
        {
            // <lang>
            //   <zh-CN>baseDir 变量保存测试运行时的程序集基目录，向上回溯到解决方案根。</zh-CN>
            //   <en>The baseDir variable holds the test runtime assembly base directory, walked upward to the solution root.</en>
            // </lang>
            DirectoryInfo directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null && !directory.EnumerateFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                return null;
            }

            // <lang>
            //   <zh-CN>configPath 保存从解决方案根到配置文件的固定相对路径。</zh-CN>
            //   <en>The configPath variable holds the fixed relative path from the solution root to the configuration file.</en>
            // </lang>
            string configPath = Path.Combine(directory.FullName, "src", "Portal", "Config", "UnityCfg.xml");
            return File.Exists(configPath) ? configPath : null;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>断言 UnityCfg.xml 不再包含任何单例生命周期声明；一旦有人把数据层注册回单例，此测试失败。</zh-CN>
        ///   <en>Asserts that UnityCfg.xml no longer contains any singleton lifetime declaration; if anyone reverts a data-layer registration to singleton, this test fails.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void DataLayerRegistrations_AreNotSingleton()
        {
            // <lang>
            //   <zh-CN>configPath 变量保存解析到的配置文件路径；找不到时跳过而非失败。</zh-CN>
            //   <en>The configPath variable holds the resolved config path; skip instead of failing when it cannot be found.</en>
            // </lang>
            string configPath = ResolveUnityConfigPath();
            if (configPath == null)
            {
                Assert.Inconclusive("未找到源码树中的 UnityCfg.xml，跳过配置守恒检查。");
                return;
            }

            // <lang>
            //   <zh-CN>content 变量保存配置文件全文，用于断言生命周期声明。</zh-CN>
            //   <en>The content variable holds the full configuration file text for lifetime assertions.</en>
            // </lang>
            string content = File.ReadAllText(configPath);

            // <lang>
            //   <zh-CN>断言固定单例生命周期已被移除，瞬态生命周期已生效。</zh-CN>
            //   <en>The assertion pins that the singleton lifetime is removed and the transient lifetime is in effect.</en>
            // </lang>
            Assert.IsFalse(
                content.Contains("<lifetime type=\"singleton\" />"),
                "UnityCfg.xml 仍存在 singleton 生命周期；P42 修复要求数据层改为 transient 以消除共享 DbContext 并发竞态。");

            Assert.IsTrue(
                content.Contains("<lifetime type=\"transient\" />"),
                "UnityCfg.xml 未包含 transient 生命周期声明。");
        }
    }
}
