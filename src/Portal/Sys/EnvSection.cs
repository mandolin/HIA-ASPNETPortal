using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace ASPNET.StarterKit.Portal.Sys
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>表示 Web.config 中的门户环境配置节。</zh-CN>
    ///   <en>Represents the portal environment configuration section in Web.config.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该配置节只保存轻量环境标识，例如 `dev`；真实敏感值仍应通过外置配置或环境变量机制提供。</zh-CN>
    ///   <en>This section stores only a lightweight environment marker such as `dev`; real sensitive values must still come from external configuration or environment-variable mechanisms.</en>
    /// </lang>
    /// </remarks>
    public class EnvSection : ConfigurationSection
    {
        /// <summary>
        /// <l>
        ///   <zh-CN>当前门户环境标识。</zh-CN>
        ///   <en>Current portal environment marker.</en>
        /// </l>
        /// </summary>
        [ConfigurationProperty("value", DefaultValue = "dev", IsRequired = false)]
        public string Value
        {
            get { return (string)this["value"]; }
            set { this["value"] = value; }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>允许旧调用点把配置节直接作为字符串读取。</zh-CN>
        ///   <en>Allows legacy call sites to read the configuration section directly as a string.</en>
        /// </lang>
        /// </summary>
        /// <param name="section">
        /// <l zh-CN="环境配置节实例。" en="Environment configuration section instance." />
        /// </param>
        /// <returns>
        /// <l zh-CN="配置节值；配置节为空时返回空。" en="Section value, or null when the section itself is null." />
        /// </returns>
        public static implicit operator string(EnvSection section)
        {
            return section?.Value;
        }
    }
}
