using System;
using System.Web;
using System.Web.UI;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>为门户 Web Forms 页面提供主题预初始化与 Unity 依赖注入的基类。</zh-CN>
    ///     <en>Base class providing theme pre-initialization and Unity dependency injection for portal Web Forms pages.</en>
    ///   </lang>
    /// </summary>
    /// <typeparam name="T">
    ///   <l zh-CN="供 Unity BuildUp 使用的具体页面类型。" en="Concrete page type supplied to Unity BuildUp." />
    /// </typeparam>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       生命周期保持既有顺序：先在 <see cref="OnPreInit"/> 选择唯一原生 Theme，再注入当前请求依赖，
    ///       最后调用 <see cref="Page.OnPreInit"/>。该顺序直接影响 Web Forms 主题资源加载和页面控件树构建，
    ///       不能因为派生页 UI 需要或文档化整理而随意重排。
    ///     </zh-CN>
    ///     <en>
    ///       The lifecycle keeps the existing order: <see cref="OnPreInit"/> selects the single native Theme first,
    ///       injects current-request dependencies next, and finally calls <see cref="Page.OnPreInit"/>. This order
    ///       directly affects Web Forms theme resource loading and page control-tree construction, so derived-page UI
    ///       needs or documentation cleanup must not casually reorder it.
    ///     </en>
    ///   </lang>
    /// </remarks>
    public abstract class PortalPage<T> : Page
        where T : class
    {
        /// <summary>
        ///   <l zh-CN="当前请求可用的 Unity 容器访问器。" en="Unity container accessor available to the current request." />
        /// </summary>
        private IContainerAccessor _accessor;

        /// <summary>
        ///   <lang>
        ///     <zh-CN>在页面预初始化阶段应用主题并装配依赖。</zh-CN>
        ///     <en>Applies the theme and builds dependencies during page pre-initialization.</en>
        ///   </lang>
        /// </summary>
        /// <param name="e">
        ///   <l zh-CN="预初始化事件参数。" en="Pre-initialization event arguments." />
        /// </param>
        /// <remarks>
        ///   <lang>
        ///     <zh-CN>
        ///       主题必须在 Web Forms 加载 App_Themes 资源前设置。主题解析器自身负责可信包验证和安全回退；
        ///       本方法不读取查询字符串来选择主题，也不改变 Tab 覆盖优先级。
        ///     </zh-CN>
        ///     <en>
        ///       The theme must be set before Web Forms loads App_Themes resources. The theme resolver handles trusted
        ///       package validation and safe fallback; this method does not read query strings to choose a theme or
        ///       alter tab-override precedence.
        ///     </en>
        ///   </lang>
        /// </remarks>
        protected override void OnPreInit(EventArgs e)
        {
            /*
             * <lang>
             *   <zh-CN>先应用主题，再执行依赖注入；这里的先后顺序是 Web Forms 原生主题机制的硬约束。</zh-CN>
             *   <en>Apply the theme before dependency injection; this ordering is a hard constraint of the native Web Forms theme mechanism.</en>
             * </lang>
             */
            if (ShouldApplyPortalTheme)
            {
                PortalThemeResolver.ApplyTheme(this);
            }

            /*
             * <lang>
             *   <zh-CN>主题确定后再让 Unity 填充页面依赖，最后交还 ASP.NET 页面生命周期。</zh-CN>
             *   <en>After the theme is fixed, let Unity populate page dependencies and then return to the ASP.NET page lifecycle.</en>
             * </lang>
             */
            InjectDependencies();
            base.OnPreInit(e);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>指示当前页面是否应加载门户 Web Forms 主题。</zh-CN>
        ///   <en>Indicates whether the current page should load the Portal Web Forms theme.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>普通页面应保持默认值；仅无 HTML 外壳的下载、流式响应或极少数兼容页面可覆写为 <c>false</c>， 以避免 Web Forms 在没有 <c>&lt;head runat="server" /&gt;</c> 的页面上强制注入主题样式。</zh-CN>
        ///   <en>Normal pages should keep the default value. Only download, streaming-response, or rare compatibility pages without an HTML shell should override this to <c>false</c>, avoiding Web Forms stylesheet injection on pages that do not have <c>&lt;head runat="server" /&gt;</c>.</en>
        /// </lang>
        /// </remarks>
        protected virtual bool ShouldApplyPortalTheme
        {
            get { return true; }
        }

        /// <summary>
        ///   <lang>
        ///     <zh-CN>使用当前 HTTP 应用实例公开的 Unity 容器执行页面依赖注入。</zh-CN>
        ///     <en>Performs page dependency injection through the Unity container exposed by the current HTTP application instance.</en>
        ///   </lang>
        /// </summary>
        /// <remarks>
        ///   <lang>
        ///     <zh-CN>
        ///       当前请求或容器访问器不可用时保持历史静默返回；若访问器存在但容器为空则抛出异常，
        ///       以便全局异常和诊断流程记录配置错误。
        ///     </zh-CN>
        ///     <en>
        ///       When the current request or container accessor is unavailable, the historical behavior is a silent
        ///       return. When an accessor exists but its container is null, an exception is thrown so the global error
        ///       and diagnostics flow can record the configuration problem.
        ///     </en>
        ///   </lang>
        /// </remarks>
        protected virtual void InjectDependencies()
        {
            /*
             * <lang>
             *   <zh-CN>先确认当前确实处于 Web 请求中；部分测试或设计期环境可能没有 HttpContext。</zh-CN>
             *   <en>First confirm that this is a Web request; some test or design-time contexts may not have HttpContext.</en>
             * </lang>
             */
            HttpContext context = HttpContext.Current;
            if (context == null)
            {
                return;
            }

            /*
             * <lang>
             *   <zh-CN>容器访问器来自 Global.asax 暴露的应用实例；缺失时保持旧页面兼容，不主动失败。</zh-CN>
             *   <en>The accessor comes from the application instance exposed by Global.asax; when missing, keep legacy page compatibility instead of failing eagerly.</en>
             * </lang>
             */
            _accessor = context.ApplicationInstance as IContainerAccessor;
            if (_accessor == null)
            {
                return;
            }

            /*
             * <lang>
             *   <zh-CN>访问器存在但容器为空说明启动配置不完整，应交给全局异常和结构化日志记录。</zh-CN>
             *   <en>If the accessor exists but the container is null, startup configuration is incomplete and should be recorded by global error handling and structured logs.</en>
             * </lang>
             */
            IUnityContainer container = _accessor.Container;
            if (container == null)
            {
                throw new InvalidOperationException("找不到 Unity 容器");
            }

            /*
             * <lang>
             *   <zh-CN>使用当前应用容器填充页面声明的依赖；不在这里创建新的生命周期作用域。</zh-CN>
             *   <en>Use the current application container to populate page dependencies; do not create a new lifetime scope here.</en>
             * </lang>
             */
            container.BuildUp(typeof(T), this, string.Empty);
        }

        /// <summary>
        ///   <lang>
        ///     <zh-CN>为同一页面生命周期内的服务器控件执行 Unity BuildUp。</zh-CN>
        ///     <en>Performs Unity BuildUp for a server control in the same page lifecycle.</en>
        ///   </lang>
        /// </summary>
        /// <param name="ctrl">
        ///   <l zh-CN="要装配的服务器控件。" en="Server control to build up." />
        /// </param>
        /// <remarks>
        ///   <lang>
        ///     <zh-CN>
        ///       调用方必须确保本页面已完成 <see cref="InjectDependencies"/> 且容器可用；此方法不创建新的作用域，
        ///       也不替代动态模块在自身 <c>OnInit</c> 中执行的当前上下文注入。
        ///     </zh-CN>
        ///     <en>
        ///       Callers must ensure this page has completed <see cref="InjectDependencies"/> and has an available
        ///       container. This method creates no new scope and does not replace current-context injection performed
        ///       by a dynamic module in its own <c>OnInit</c>.
        ///     </en>
        ///   </lang>
        /// </remarks>
        public void BuildUpControl(Control ctrl)
        {
            /*
             * <lang>
             *   <zh-CN>复用页面已经解析出的容器，为动态控件补齐声明式依赖。</zh-CN>
             *   <en>Reuse the container already resolved by the page to complete declared dependencies on the dynamic control.</en>
             * </lang>
             */
            _accessor.Container.BuildUp(typeof(Control), ctrl, string.Empty);
        }
    }
}
