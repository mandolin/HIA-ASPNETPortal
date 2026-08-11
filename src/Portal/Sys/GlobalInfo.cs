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
        /// <lang>
        ///   <zh-CN>当前环境标识，默认 `dev`。</zh-CN>
        ///   <en>Current environment marker, defaulting to `dev`.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该值只用于选择本地运行 profile 和诊断展示，不应编码数据库、账号或部署机密。</zh-CN>
        ///   <en>This value is used only for local runtime profile selection and diagnostic display; it must not encode databases, accounts, or deployment secrets.</en>
        /// </lang>
        /// </remarks>
        public static string Environment = "dev";

        /// <summary>
        /// <lang>
        ///   <zh-CN>低敏扩展信息字典，供运行期辅助组件临时共享状态。</zh-CN>
        ///   <en>Low-sensitivity extension information dictionary used by runtime helper components to share temporary state.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>字典是进程级共享对象，值必须可重建且不含凭据；跨请求持久化状态应使用受治理的数据访问层。</zh-CN>
        ///   <en>The dictionary is a process-level shared object and values must be rebuildable and credential-free; cross-request durable state belongs in the governed data-access layer.</en>
        /// </lang>
        /// </remarks>
        public static ConcurrentDictionary<string, object> ExtInfo
            // <lang>
            //   <zh-CN>初始化为线程安全字典，适配旧 WebForms 多请求并发访问的静态共享模式。</zh-CN>
            //   <en>Initialize as a thread-safe dictionary to fit the legacy WebForms static sharing pattern under concurrent requests.</en>
            // </lang>
            = new ConcurrentDictionary<string, object>();
    }
}
