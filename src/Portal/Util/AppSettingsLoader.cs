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
    public static class AppSettingsLoader
    {
        public static void LoadConfig(string appSettingsConfig, bool preservePrevious = true,
            bool preserveLocalSqlServer = true)
        {
            string envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, appSettingsConfig);
            if (File.Exists(envPath))
            {
                LoadConfigFile(appSettingsConfig, preservePrevious, preserveLocalSqlServer);
            }
        }
       

        private static void LoadConfigFile(string configFile, bool preservePrevious = true,
            bool preserveLocalSqlServer = true)
        {
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFile);

            var json = File.ReadAllText(fullPath);
            var config = JObject.Parse(json);

            var appSettings = config["appSettings"] as JObject;

            LoadAppSettings(appSettings, preservePrevious);
            
        }

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
                    //移除掉 prevKeys 中 不在 appSettings 中的项
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
