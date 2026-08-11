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
            // <lang>
            //   <zh-CN>读取 Web.config 中的轻量环境标识；缺省值由配置属性定义为 dev。</zh-CN>
            //   <en>Reads the lightweight environment marker from Web.config; the configuration property defines dev as the default.</en>
            // </lang>
            get { return (string)this["value"]; }
            // <lang>
            //   <zh-CN>写入仅更新配置节对象中的 value 字段，不触达外置敏感配置或运行时密钥。</zh-CN>
            //   <en>Writing updates only the value field on the section object and does not touch external sensitive configuration or runtime secrets.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>保留旧调用点的宽容语义：缺少 env 节时返回 null，由调用方决定默认环境。</zh-CN>
            //   <en>Preserve the tolerant legacy call-site semantics: a missing env section returns null and lets the caller choose the default environment.</en>
            // </lang>
            return section?.Value;
        }
    }
}
