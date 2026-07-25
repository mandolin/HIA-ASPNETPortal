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
        /// <l zh-CN="需要接收注册项的 Unity 容器。" en="Unity container that receives registrations." />
        /// </param>
        /// <param name="unityConfig">
        /// <l zh-CN="相对于站点根目录的 Unity XML 配置文件路径。" en="Unity XML configuration file path relative to the site root." />
        /// </param>
        public static void LoadUnityConfig(IUnityContainer container, string unityConfig)
        {
            string envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, unityConfig);
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
        /// <l zh-CN="需要接收注册项的 Unity 容器。" en="Unity container that receives registrations." />
        /// </param>
        /// <param name="configFile">
        /// <l zh-CN="相对于站点根目录的 Unity XML 配置文件路径。" en="Unity XML configuration file path relative to the site root." />
        /// </param>
        /// <exception cref="ConfigurationErrorsException">
        /// <l zh-CN="配置文件存在但缺少 `&lt;unity&gt;` 节时抛出。" en="Thrown when the file exists but does not contain a `&lt;unity&gt;` section." />
        /// </exception>
        private static void LoadConfig(IUnityContainer container, string configFile)
        {
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFile);
            var fileMap = new ExeConfigurationFileMap { ExeConfigFilename = fullPath };
            var config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
            var unitySection = (UnityConfigurationSection)config.GetSection("unity");

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
