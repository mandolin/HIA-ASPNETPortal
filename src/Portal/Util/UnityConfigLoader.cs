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
    public static class UnityConfigLoader
    {

        public static void LoadUnityConfig(IUnityContainer container, string unityConfig)
        {
            string envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, unityConfig);
            if (File.Exists(envPath))
            {
                LoadConfig(container, unityConfig);
            }
        }

        private static void LoadConfig(IUnityContainer container, string configFile)
        {
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFile);
            var fileMap = new ExeConfigurationFileMap { ExeConfigFilename = fullPath };
            var config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
            var unitySection = (UnityConfigurationSection)config.GetSection("unity");

            if (unitySection == null)
                throw new ConfigurationErrorsException($"<unity> section missing in {configFile}");

            // 使用默认容器名（或指定 "default"）
            unitySection.Configure(container);
        }
        
    }
}
