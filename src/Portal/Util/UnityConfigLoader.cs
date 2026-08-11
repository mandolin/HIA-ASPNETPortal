using Linq.Extras;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Xml;
using System.Xml.Linq;
using Unity;

namespace ASPNET.StarterKit.Portal.Util
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>从本地 Unity XML 配置文件加载依赖注入注册。</zh-CN>
    ///   <en>Loads dependency-injection registrations from a local Unity XML configuration file.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该加载器用于旧 WebForms 启动期组合根；配置文件不存在时静默跳过，使本地、VS 和 VSCode 环境可以按需提供不同 Unity 覆盖文件。</zh-CN>
    ///   <en>This loader supports the legacy WebForms startup composition root; missing files are skipped silently so local, Visual Studio, and VSCode environments can provide different Unity override files as needed.</en>
    /// </lang>
    /// </remarks>
    public static class UnityConfigLoader
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>在 Unity 配置文件存在时将其注册到目标容器。</zh-CN>
        ///   <en>Registers a Unity configuration file into the target container when the file exists.</en>
        /// </lang>
        /// </summary>
        /// <param name="container">
        /// <l>
        ///   <zh-CN>需要接收注册项的 Unity 容器。</zh-CN>
        ///   <en>Unity container that receives registrations.</en>
        /// </l>
        /// </param>
        /// <param name="unityConfig">
        /// <l>
        ///   <zh-CN>相对于站点根目录的 Unity XML 配置文件路径。</zh-CN>
        ///   <en>Unity XML configuration file path relative to the site root.</en>
        /// </l>
        /// </param>
        public static void LoadUnityConfig(IUnityContainer container, string unityConfig)
        {
            // <lang>
            //   <zh-CN>配置路径始终相对站点根目录解析，避免调用方传入值被解释为当前进程工作目录路径。</zh-CN>
            //   <en>The configuration path is always resolved relative to the site root, preventing caller input from being interpreted as a process working-directory path.</en>
            // </lang>
            string envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, unityConfig);
            // <lang>
            //   <zh-CN>缺少环境专属 Unity 覆盖文件时静默跳过，保留默认容器注册和旧本地开发体验。</zh-CN>
            //   <en>When an environment-specific Unity override file is absent, skip silently to preserve default container registrations and the legacy local-development experience.</en>
            // </lang>
            if (File.Exists(envPath))
            {
                LoadConfig(container, unityConfig);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>打开映射后的 Unity XML 文件并执行默认容器配置。</zh-CN>
        ///   <en>Opens the mapped Unity XML file and applies the default container configuration.</en>
        /// </lang>
        /// </summary>
        /// <param name="container">
        /// <l>
        ///   <zh-CN>需要接收注册项的 Unity 容器。</zh-CN>
        ///   <en>Unity container that receives registrations.</en>
        /// </l>
        /// </param>
        /// <param name="configFile">
        /// <l>
        ///   <zh-CN>相对于站点根目录的 Unity XML 配置文件路径。</zh-CN>
        ///   <en>Unity XML configuration file path relative to the site root.</en>
        /// </l>
        /// </param>
        /// <exception cref="ConfigurationErrorsException">
        /// <l>
        ///   <zh-CN>配置文件存在但缺少 `&lt;unity&gt;` 节时抛出。</zh-CN>
        ///   <en>Thrown when the file exists but does not contain a `&lt;unity&gt;` section.</en>
        /// </l>
        /// </exception>
        private static void LoadConfig(IUnityContainer container, string configFile)
        {
            // <lang>
            //   <zh-CN>实际读取路径再次从站点根组合，确保公开入口与内部加载使用同一信任边界。</zh-CN>
            //   <en>The actual read path is again combined from the site root so the public entry point and internal loader use the same trust boundary.</en>
            // </lang>
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFile);
            // <lang>
            //   <zh-CN>映射配置对象把外部 XML 文件作为独立 exe 配置读取，不修改 Web.config 或运行时配置集合。</zh-CN>
            //   <en>The mapped-configuration object reads the external XML file as a standalone exe configuration without modifying Web.config or runtime configuration collections.</en>
            // </lang>
            var fileMap = new ExeConfigurationFileMap { ExeConfigFilename = fullPath };
            // <lang>
            //   <zh-CN>按非用户级配置打开文件，使部署目录中的 Unity 覆盖注册成为唯一输入来源。</zh-CN>
            //   <en>Open the file as non-user-level configuration so Unity override registrations come only from the deployment directory.</en>
            // </lang>
            var config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
            // <lang>
            //   <zh-CN>只读取 Unity 配置节；其它配置节不在本加载器中解释，降低旧覆盖文件的副作用面。</zh-CN>
            //   <en>Read only the Unity section; other configuration sections are not interpreted by this loader, limiting side effects from legacy override files.</en>
            // </lang>
            var unitySection = (UnityConfigurationSection)config.GetSection("unity");

            // <lang>
            //   <zh-CN>文件存在但缺少 Unity 节属于配置错误，需要显式失败，避免站点以未注入依赖的半初始化状态运行。</zh-CN>
            //   <en>A present file without a Unity section is a configuration error and must fail explicitly, preventing the site from running half-initialized without injected dependencies.</en>
            // </lang>
            if (unitySection == null)
                throw new ConfigurationErrorsException($"<unity> section missing in {configFile}");

            // <lang>
            //   <zh-CN>沿用 Unity 配置节的默认容器解析规则，避免在旧配置文件中额外约束容器名称。</zh-CN>
            //   <en>Use Unity's default container resolution rule so legacy configuration files do not need an extra container-name constraint.</en>
            // </lang>
            unitySection.Configure(container);
        }
    }
}
