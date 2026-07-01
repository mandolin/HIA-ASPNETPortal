using System;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 统一管理当前请求中的 PortalSettings，避免各页面直接硬编码 Context.Items 键名。
    /// </summary>
    public static class PortalContext
    {
        /// <summary>
        /// 当前请求中保存 PortalSettings 的 HttpContext.Items 键名。
        /// </summary>
        public const string PortalSettingsKey = "PortalSettings";

        /// <summary>
        /// 将当前请求的门户上下文写入 HttpContext。
        /// </summary>
        public static void SetPortalSettings(PortalSettings settings, HttpContext context = null)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            GetHttpContext(context).Items[PortalSettingsKey] = settings;
        }

        /// <summary>
        /// 读取当前请求的门户上下文；缺失时抛出明确异常，便于定位早期空引用问题。
        /// </summary>
        public static PortalSettings GetPortalSettings(HttpContext context = null)
        {
            HttpContext current = GetHttpContext(context);
            var settings = current.Items[PortalSettingsKey] as PortalSettings;
            if (settings != null)
            {
                return settings;
            }

            string path = current.Request?.RawUrl ?? "(unknown request)";
            throw new InvalidOperationException(
                $"当前请求缺少 PortalSettings。请求路径：{path}。请确认 Global.Application_BeginRequest 已成功构建门户上下文。");
        }

        private static HttpContext GetHttpContext(HttpContext context)
        {
            HttpContext current = context ?? HttpContext.Current;
            if (current == null)
            {
                throw new InvalidOperationException("当前线程没有可用的 HttpContext，无法读取门户上下文。");
            }

            return current;
        }
    }
}
