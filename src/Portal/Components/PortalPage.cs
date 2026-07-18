using System;
using System.Web;
using System.Web.UI;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 为门户 Web Forms 页面提供主题预初始化与 Unity 依赖注入的基类。
    /// Base class providing theme pre-initialization and Unity dependency injection for portal Web Forms pages.
    /// </summary>
    /// <typeparam name="T">供 Unity BuildUp 使用的具体页面类型。
    /// Concrete page type supplied to Unity BuildUp.</typeparam>
    /// <remarks>
    /// 生命周期保持既有顺序：先在 <see cref="OnPreInit"/> 选择唯一原生 Theme，再注入当前请求依赖，最后调用
    /// <see cref="Page.OnPreInit"/>。该顺序不能由派生页的 UI 需要或文档工作随意重排。
    /// Lifecycle retains its existing order: <see cref="OnPreInit"/> selects the single native Theme first, then injects
    /// current-request dependencies, and finally calls <see cref="Page.OnPreInit"/>. Derived-page UI needs or
    /// documentation work must not casually reorder it.
    /// </remarks>
    public abstract class PortalPage<T> : Page
        where T : class
    {
        private IContainerAccessor _accessor; // 保存当前请求可用的 Unity 容器访问器。

        /// <summary>
        /// 在页面预初始化阶段应用主题并装配依赖。
        /// Applies the theme and builds dependencies during page pre-initialization.
        /// </summary>
        /// <param name="e">预初始化事件参数。Pre-initialization event arguments.</param>
        /// <remarks>
        /// 主题必须在 Web Forms 加载 App_Themes 资源前设置。主题解析器自身负责可信包验证和安全回退；本方法不读取
        /// 查询字符串来选择主题，也不改变 Tab 覆盖优先级。
        /// The theme must be set before Web Forms loads App_Themes resources. The theme resolver itself handles trusted
        /// package validation and safe fallback; this method does not read query strings to choose a theme or alter
        /// Tab-override precedence.
        /// </remarks>
        protected override void OnPreInit(EventArgs e)
        {
            // 主题必须在 PreInit 阶段完成选择，之后 WebForms 才能正确加载 App_Themes 资源。
            // Theme selection must happen during PreInit so WebForms can load App_Themes resources.
            if (ShouldApplyPortalTheme)
            {
                PortalThemeResolver.ApplyTheme(this);
            }

            InjectDependencies(); // 注入依赖项。
            base.OnPreInit(e); // 调用基类的 OnPreInit 方法。
        }

        /// <summary>
        /// 中文：指示当前页面是否应加载门户 Web Forms 主题。
        ///
        /// English: Indicates whether the current page should load the Portal Web Forms theme.
        /// </summary>
        /// <remarks>
        /// 中文：普通页面应保持默认值；仅无 HTML 外壳的下载、流式响应或极少数兼容页面可覆写为 <c>false</c>，
        /// 以避免 Web Forms 在没有 <c>&lt;head runat="server" /&gt;</c> 的页面上强制注入主题样式。
        ///
        /// English: Normal pages should keep the default value. Only download, streaming-response, or rare compatibility
        /// pages without an HTML shell should override this to <c>false</c>, avoiding Web Forms stylesheet injection on
        /// pages that do not have <c>&lt;head runat="server" /&gt;</c>.
        /// </remarks>
        protected virtual bool ShouldApplyPortalTheme
        {
            get { return true; }
        }

        /// <summary>
        /// 使用当前 HTTP 应用实例公开的 Unity 容器执行页面依赖注入。
        /// Performs page dependency injection through the Unity container exposed by the current HTTP application instance.
        /// </summary>
        /// <remarks>
        /// 当前请求或容器访问器不可用时保持历史静默返回；若访问器存在但容器为空则抛出异常，以便全局异常和诊断流程记录配置错误。
        /// When the current request or container accessor is unavailable, the historical behavior is a silent return;
        /// when an accessor exists but its container is null, an exception is thrown for global error and diagnostics flow.
        /// </remarks>
        protected virtual void InjectDependencies()
        {
            HttpContext context = HttpContext.Current; // 读取当前 Web 请求上下文。
            if (context == null)
            {
                return; // 非 Web 请求中不执行页面 BuildUp。
            }

            _accessor = context.ApplicationInstance as IContainerAccessor; // 从应用实例获取容器访问器。
            if (_accessor == null)
            {
                return; // 应用未公开容器时保持旧页面兼容行为。
            }

            IUnityContainer container = _accessor.Container; // 获取当前应用容器实例。
            if (container == null)
            {
                throw new InvalidOperationException("找不到 Unity 容器"); // 由全局错误流程记录容器配置异常。
            }

            // 使用当前请求容器填充页面声明的依赖。
            container.BuildUp(typeof(T), this, string.Empty);
        }

        /// <summary>
        /// 为同一页面生命周期内的服务器控件执行 Unity BuildUp。
        /// Performs Unity BuildUp for a server control in the same page lifecycle.
        /// </summary>
        /// <param name="ctrl">要装配的服务器控件。
        /// Server control to build up.</param>
        /// <remarks>
        /// 调用方必须确保本页面已完成 <see cref="InjectDependencies"/> 且容器可用；此方法不创建新的作用域，也不替代
        /// 动态模块在自身 <c>OnInit</c> 中执行的当前上下文注入。
        /// Callers must ensure this page has completed <see cref="InjectDependencies"/> and has an available container.
        /// This method creates no new scope and does not replace current-context injection performed by a dynamic module
        /// in its own <c>OnInit</c>.
        /// </remarks>
        public void BuildUpControl(Control ctrl)
        {
            // 使用已解析容器填充控件声明的依赖。
            _accessor.Container.BuildUp(typeof(Control), ctrl, string.Empty);
        }
    }
}
