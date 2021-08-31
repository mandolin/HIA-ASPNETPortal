using System;
using System.Web;
using System.Web.UI;
using Microsoft.Practices.Unity;

namespace ASPNET.StarterKit.Portal
{
    public abstract class PortalPage<T> : Page
        where T : class
    {
        private IContainerAccessor _accessor;

        protected override void OnPreInit(EventArgs e)
        {
            InjectDependencies();
            base.OnPreInit(e);
        }

        protected virtual void InjectDependencies()
        {
            HttpContext context = HttpContext.Current;
            if (context == null)
            {
                return;
            }

            _accessor = context.ApplicationInstance as IContainerAccessor;
            if (_accessor == null)
            {
                return;
            }

            IUnityContainer container = _accessor.Container;
            if (container == null)
            {
                throw new InvalidOperationException("No Unity container found");
            }

            container.BuildUp(typeof (T), this, string.Empty);
        }

        public void BuildUpControl(Control ctrl)
        {
            _accessor.Container.BuildUp(typeof (Control), ctrl, string.Empty);
        }
    }
}