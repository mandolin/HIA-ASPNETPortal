using System;
using System.Web;
using System.Web.UI;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /*
     * 本文件代码定义了一个抽象基类 PortalPage<T>，它继承自 Page 并实现了依赖注入的功能。依赖注入是通过 Unity 容器完成的，该容器需要由外部提供并通过 HttpContext 的 ApplicationInstance 属性访问。OnPreInit 方法覆盖了基类的行为，在页面预初始化时注入依赖项。InjectDependencies 方法负责实际的依赖注入操作，而 BuildUpControl 方法则用于为页面上的控件注入依赖项。
     *
     */

    /// <summary>
    ///   抽象基类用于门户页面，实现依赖注入。
    /// </summary>
    /// <typeparam name="T">页面模型的类型。</typeparam>
    public abstract class PortalPage<T> : Page
        where T : class
    {
        private IContainerAccessor _accessor; // 用于访问容器的私有字段。

        /// <summary>
        ///   在页面预初始化阶段执行依赖注入。
        /// </summary>
        /// <param name="e">事件参数。</param>
        protected override void OnPreInit(EventArgs e)
        {
            // 主题必须在 PreInit 阶段完成选择，之后 WebForms 才能正确加载 App_Themes 资源。
            // Theme selection must happen during PreInit so WebForms can load App_Themes resources.
            PortalThemeResolver.ApplyTheme(this);
            InjectDependencies(); // 注入依赖项。
            base.OnPreInit(e); // 调用基类的 OnPreInit 方法。
        }

        /// <summary>
        ///   执行依赖注入。
        /// </summary>
        protected virtual void InjectDependencies()
        {
            HttpContext context = HttpContext.Current; // 获取当前的 HTTP 上下文。
            if (context == null)
            {
                return; // 如果没有上下文，则退出。
            }

            _accessor = context.ApplicationInstance as IContainerAccessor; // 尝试从 ApplicationInstance 获取容器访问器。
            if (_accessor == null)
            {
                return; // 如果没有找到容器访问器，则退出。
            }

            IUnityContainer container = _accessor.Container; // 获取 Unity 容器实例。
            if (container == null)
            {
                throw new InvalidOperationException("找不到 Unity 容器"); // 如果容器不存在，则抛出异常。
            }

            // 使用容器填充页面模型的依赖项。
            container.BuildUp(typeof(T), this, string.Empty);
        }

        /// <summary>
        ///   为控件注入依赖项。
        /// note 此函数似乎未用到
        /// </summary>
        /// <param name="ctrl">要注入依赖项的控件。</param>
        public void BuildUpControl(Control ctrl)
        {
            // 使用容器填充控件的依赖项。
            _accessor.Container.BuildUp(typeof(Control), ctrl, string.Empty);
        }
    }
}
