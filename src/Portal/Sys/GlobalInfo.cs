using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ASPNET.StarterKit.Portal.Sys
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>保存门户进程级的少量全局运行信息。</zh-CN>
    ///   <en>Stores a small set of portal process-level runtime information.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该类型是旧项目中的静态共享点，只适合保存低敏、可重建的信息；不要在这里放连接串、口令、Token 或用户会话数据。</zh-CN>
    ///   <en>This type is a legacy static sharing point and is suitable only for low-sensitivity, rebuildable information; do not store connection strings, passwords, tokens, or user-session data here.</en>
    /// </lang>
    /// </remarks>
    public static class GlobalInfo
    {
        /// <summary>
        /// <l>
        ///   <zh-CN>当前环境标识，默认 `dev`。</zh-CN>
        ///   <en>Current environment marker, defaulting to `dev`.</en>
        /// </l>
        /// </summary>
        public static string Environment = "dev";

        /// <summary>
        /// <l>
        ///   <zh-CN>低敏扩展信息字典，供运行期辅助组件临时共享状态。</zh-CN>
        ///   <en>Low-sensitivity extension information dictionary used by runtime helper components to share temporary state.</en>
        /// </l>
        /// </summary>
        public static ConcurrentDictionary<string, object> ExtInfo
            = new ConcurrentDictionary<string, object>();
    }
}
