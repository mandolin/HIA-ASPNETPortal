using System;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>统一管理当前请求中的 PortalSettings，集中固定 Items 键名、上下文回退和缺失异常边界。</zh-CN>
    ///   <en>Centralizes the current request's PortalSettings contract, including the Items key, context fallback, and missing-context exception boundary.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该类型只管理请求级对象引用，不负责构造门户配置、执行权限判断或持久化设置。</zh-CN>
    ///   <en>This type manages only a request-scoped object reference; it does not build portal configuration, perform authorization, or persist settings.</en>
    /// </lang>
    /// </remarks>
    public static class PortalContext
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>当前请求中保存 PortalSettings 的 HttpContext.Items 键名。</zh-CN>
        ///   <en>HttpContext.Items key used to store PortalSettings for the current request.</en>
        /// </lang>
        /// </summary>
        public const string PortalSettingsKey = "PortalSettings";

        /// <summary>
        /// <lang>
        ///   <zh-CN>将当前请求的门户上下文写入 HttpContext。</zh-CN>
        ///   <en>Stores the portal context for the current request in HttpContext.</en>
        /// </lang>
        /// </summary>
        /// <param name="settings"><lang><zh-CN>要写入的门户设置对象；不能为空。</zh-CN><en>Portal-settings object to store; must not be null.</en></lang></param>
        /// <param name="context"><lang><zh-CN>可选的请求上下文；为空时回退到 HttpContext.Current。</zh-CN><en>Optional request context; null falls back to HttpContext.Current.</en></lang></param>
        /// <exception cref="ArgumentNullException"><lang><zh-CN>settings 为空时抛出。</zh-CN><en>Thrown when settings is null.</en></lang></exception>
        /// <exception cref="InvalidOperationException"><lang><zh-CN>显式上下文和当前线程上下文都不可用时抛出。</zh-CN><en>Thrown when neither the explicit context nor the current-thread context is available.</en></lang></exception>
        public static void SetPortalSettings(PortalSettings settings, HttpContext context = null)
        {
            // <lang>
            //   <zh-CN>拒绝空设置，避免把缺失对象写入请求容器后延迟到无关读取点才失败。</zh-CN>
            //   <en>Rejects null settings so a missing object cannot be stored and fail later at an unrelated read point.</en>
            // </lang>
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            // <lang>
            //   <zh-CN>通过统一 helper 解析显式或当前线程上下文，并只写入固定键名；不会复制或持久化设置对象。</zh-CN>
            //   <en>Resolves the explicit or current-thread context through one helper and writes only the fixed key; it does not copy or persist the settings object.</en>
            // </lang>
            GetHttpContext(context).Items[PortalSettingsKey] = settings;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前请求的门户上下文；缺失时抛出明确异常，便于定位早期空引用问题。</zh-CN>
        ///   <en>Reads the current request's portal context and throws a clear exception when it is missing to expose early lifecycle defects.</en>
        /// </lang>
        /// </summary>
        /// <param name="context"><lang><zh-CN>可选的请求上下文；为空时回退到 HttpContext.Current。</zh-CN><en>Optional request context; null falls back to HttpContext.Current.</en></lang></param>
        /// <returns><lang><zh-CN>当前请求中已写入的同一 PortalSettings 引用。</zh-CN><en>The same PortalSettings reference stored in the current request.</en></lang></returns>
        /// <exception cref="InvalidOperationException"><lang><zh-CN>上下文不可用或请求容器中没有 PortalSettings 时抛出。</zh-CN><en>Thrown when the context is unavailable or PortalSettings is absent from the request container.</en></lang></exception>
        public static PortalSettings GetPortalSettings(HttpContext context = null)
        {
            // <lang>
            //   <zh-CN>先解析上下文，确保后续 Items 读取遵循与写入相同的显式参数优先级和线程回退。</zh-CN>
            //   <en>Resolves the context first so the later Items read uses the same explicit-argument priority and thread fallback as writes.</en>
            // </lang>
            HttpContext current = GetHttpContext(context);

            // <lang>
            //   <zh-CN>从固定键读取并限定为 PortalSettings 类型；错误类型按缺失处理，避免向调用方泄露无关对象。</zh-CN>
            //   <en>Reads the fixed key and narrows it to PortalSettings; an unexpected type is treated as missing rather than exposing an unrelated object.</en>
            // </lang>
            var settings = current.Items[PortalSettingsKey] as PortalSettings;

            // <lang>
            //   <zh-CN>保留请求级对象引用，不在读取路径重新构造配置或改变其生命周期。</zh-CN>
            //   <en>Returns the request-scoped reference without rebuilding configuration or changing its lifecycle on the read path.</en>
            // </lang>
            if (settings != null)
            {
                return settings;
            }

            // <lang>
            //   <zh-CN>仅提取低敏原始 URL 作为诊断定位信息；缺失 URL 时使用稳定占位文本。</zh-CN>
            //   <en>Extracts only the low-sensitivity raw URL for diagnostics and uses a stable placeholder when it is unavailable.</en>
            // </lang>
            string path = current.Request?.RawUrl ?? "(unknown request)";

            // <lang>
            //   <zh-CN>以固定生命周期提示报告缺失事实，不回显设置内容或其它请求敏感数据。</zh-CN>
            //   <en>Reports the missing fact with a fixed lifecycle hint without echoing settings content or other request-sensitive data.</en>
            // </lang>
            throw new InvalidOperationException(
                $"当前请求缺少 PortalSettings。请求路径：{path}。请确认 Global.Application_BeginRequest 已成功构建门户上下文。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析显式请求上下文，或在未提供时回退到当前线程上下文。</zh-CN>
        ///   <en>Resolves an explicit request context or falls back to the current-thread context when none is supplied.</en>
        /// </lang>
        /// </summary>
        /// <param name="context"><lang><zh-CN>调用方显式提供的请求上下文，可为空。</zh-CN><en>Request context explicitly supplied by the caller; may be null.</en></lang></param>
        /// <returns><lang><zh-CN>可用于读取或写入 Items 的 HttpContext。</zh-CN><en>HttpContext that can be used to read or write Items.</en></lang></returns>
        /// <exception cref="InvalidOperationException"><lang><zh-CN>显式上下文和 HttpContext.Current 都为空时抛出。</zh-CN><en>Thrown when both the explicit context and HttpContext.Current are null.</en></lang></exception>
        private static HttpContext GetHttpContext(HttpContext context)
        {
            // <lang>
            //   <zh-CN>显式参数优先于线程静态上下文，便于后台/测试调用方提供受控请求容器。</zh-CN>
            //   <en>Prefers the explicit argument over thread-static context so background or test callers can provide a controlled request container.</en>
            // </lang>
            HttpContext current = context ?? HttpContext.Current;

            // <lang>
            //   <zh-CN>没有任何上下文时立即失败，避免用伪造容器掩盖错误的调用生命周期。</zh-CN>
            //   <en>Fails immediately when no context exists instead of masking an invalid call lifecycle with a fabricated container.</en>
            // </lang>
            if (current == null)
            {
                throw new InvalidOperationException("当前线程没有可用的 HttpContext，无法读取门户上下文。");
            }

            // <lang>
            //   <zh-CN>返回已确认可用的上下文；调用方随后负责遵守 Items 键和对象类型契约。</zh-CN>
            //   <en>Returns the confirmed context; callers remain responsible for the Items key and object-type contract.</en>
            // </lang>
            return current;
        }
    }
}
