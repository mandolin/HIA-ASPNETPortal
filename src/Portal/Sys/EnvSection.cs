using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace ASPNET.StarterKit.Portal.Sys
{
    public class EnvSection : ConfigurationSection
    {
        [ConfigurationProperty("value", DefaultValue = "dev", IsRequired = false)]
        public string Value
        {
            get { return (string)this["value"]; }
            set { this["value"] = value; }
        }

        // 可选：支持直接转 string（让你后面可以直接强转）
        public static implicit operator string(EnvSection section)
        {
            return section?.Value;
        }
    }
}