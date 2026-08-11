using Linq.Extras;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Hosting;
using System.Xml;
using System.Xml.Linq;
using Unity;

namespace ASPNET.StarterKit.Portal.Util
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>从本地 JSON 文件加载运行期 appSettings 覆盖值。</zh-CN>
    ///   <en>Loads runtime appSettings overrides from a local JSON file.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该加载器服务于旧 WebForms 启动期配置扩展；调用方传入的是站点根目录下的相对路径，真实敏感值仍应优先走外置配置或环境变量覆盖。</zh-CN>
    ///   <en>This loader supports legacy WebForms startup-time configuration extension; callers pass a relative path under the site root, while real sensitive values should still prefer external configuration or environment-variable overrides.</en>
    /// </lang>
    /// </remarks>
    public static class AppSettingsLoader
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>在配置文件存在时加载 JSON appSettings 覆盖。</zh-CN>
        ///   <en>Loads JSON appSettings overrides when the configuration file exists.</en>
        /// </lang>
        /// </summary>
        /// <param name="appSettingsConfig">
        /// <l>
        ///   <zh-CN>相对于站点根目录的 JSON 配置文件路径。</zh-CN>
        ///   <en>JSON configuration file path relative to the site root.</en>
        /// </l>
        /// </param>
        /// <param name="preservePrevious">
        /// <l>
        ///   <zh-CN>是否保留 JSON 中未声明的既有 appSettings 键。</zh-CN>
        ///   <en>Whether to keep existing appSettings keys that are not declared in JSON.</en>
        /// </l>
        /// </param>
        /// <param name="preserveLocalSqlServer">
        /// <l>
        ///   <zh-CN>历史兼容参数；当前加载流程不直接处理 SQL Server 保留逻辑。</zh-CN>
        ///   <en>Historical compatibility parameter; the current loading flow does not directly process SQL Server preservation logic.</en>
        /// </l>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>文件不存在时静默跳过，保持 Visual Studio、VSCode 和不同部署环境可共享同一启动路径。</zh-CN>
        ///   <en>Missing files are skipped silently so Visual Studio, VSCode, and different deployment environments can share the same startup path.</en>
        /// </lang>
        /// </remarks>
        public static void LoadConfig(string appSettingsConfig, bool preservePrevious = true,
            bool preserveLocalSqlServer = true)
        {
            string envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, appSettingsConfig);
            if (File.Exists(envPath))
            {
                LoadConfigFile(appSettingsConfig, preservePrevious, preserveLocalSqlServer);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取并解析 JSON 配置文件，然后把 `appSettings` 节应用到运行期配置集合。</zh-CN>
        ///   <en>Reads and parses the JSON configuration file, then applies the `appSettings` section to the runtime configuration collection.</en>
        /// </lang>
        /// </summary>
        /// <param name="configFile">
        /// <l>
        ///   <zh-CN>相对于站点根目录的 JSON 配置文件路径。</zh-CN>
        ///   <en>JSON configuration file path relative to the site root.</en>
        /// </l>
        /// </param>
        /// <param name="preservePrevious">
        /// <l>
        ///   <zh-CN>是否保留 JSON 中未声明的既有 appSettings 键。</zh-CN>
        ///   <en>Whether to keep existing appSettings keys that are not declared in JSON.</en>
        /// </l>
        /// </param>
        /// <param name="preserveLocalSqlServer">
        /// <l>
        ///   <zh-CN>历史兼容参数；当前方法保留签名但不直接使用。</zh-CN>
        ///   <en>Historical compatibility parameter; this method keeps the signature but does not use it directly.</en>
        /// </l>
        /// </param>
        private static void LoadConfigFile(string configFile, bool preservePrevious = true,
            bool preserveLocalSqlServer = true)
        {
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFile);

            var json = File.ReadAllText(fullPath);
            var config = JObject.Parse(json);

            var appSettings = config["appSettings"] as JObject;

            LoadAppSettings(appSettings, preservePrevious);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把 JSON 中的 appSettings 键值写入 `ConfigurationManager.AppSettings`。</zh-CN>
        ///   <en>Writes JSON appSettings key/value pairs into `ConfigurationManager.AppSettings`.</en>
        /// </lang>
        /// </summary>
        /// <param name="appSettings">
        /// <l>
        ///   <zh-CN>JSON `appSettings` 对象；为空时不做任何修改。</zh-CN>
        ///   <en>JSON `appSettings` object; when null, no changes are applied.</en>
        /// </l>
        /// </param>
        /// <param name="preservePrevious">
        /// <l>
        ///   <zh-CN>是否保留 JSON 中未声明的既有键；为 false 时会移除未声明键。</zh-CN>
        ///   <en>Whether to keep existing keys not declared by JSON; when false, undeclared keys are removed.</en>
        /// </l>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该方法只处理普通 appSettings 字符串值，不负责连接串、密钥加密或复杂对象绑定。</zh-CN>
        ///   <en>This method handles only ordinary appSettings string values and does not manage connection strings, secret encryption, or complex object binding.</en>
        /// </lang>
        /// </remarks>
        private static void LoadAppSettings(JObject appSettings, bool preservePrevious)
        {
            if (appSettings != null)
            {
                var prevKeys = ConfigurationManager.AppSettings.AllKeys;

                foreach (var prop in appSettings.Properties())
                {
                    ConfigurationManager.AppSettings.Set(prop.Name, prop.Value.ToString());
                }

                if (!preservePrevious)
                {
                    // <lang>
                    //   <zh-CN>显式替换模式会移除 JSON 未声明的旧键，避免历史配置在当前环境中继续生效。</zh-CN>
                    //   <en>Explicit replacement mode removes old keys not declared by JSON so historical configuration does not continue to affect the current environment.</en>
                    // </lang>
                    foreach (var key in prevKeys)
                    {
                        if (!appSettings.ContainsKey(key))
                        {
                            ConfigurationManager.AppSettings.Remove(key);
                        }
                    }

                }
            }
        }
    }
}
