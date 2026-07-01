using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ASPNET.StarterKit.Portal.Sys
{
    public static class GlobalInfo
    {
        //public static string PortalName = "Portal";

        /// <summary>
        /// 环境标识
        /// </summary>
        public static string Environment = "dev";

        /// <summary>
        /// 扩展信息
        /// </summary>
        public static ConcurrentDictionary<string, object> ExtInfo
            = new ConcurrentDictionary<string, object>();

    }
}