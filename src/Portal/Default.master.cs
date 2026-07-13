using System;
using System.Web.UI;
using System.Web.UI.HtmlControls;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 门户默认 Master Page 的主题 CSS 作用域宿主。
    /// Theme CSS-scope host for the portal default Master Page.
    /// </summary>
    /// <remarks>
    /// 页面 Theme 已在 PortalPage.PreInit 中确定。本类只在渲染前把已解析主题和门户 Tab 转为受控 body class，
    /// 不读取查询字符串、不重新选择 Theme，也不使 GenericErrorPage 依赖主题解析。
    /// Page Theme is decided in PortalPage.PreInit. This class only converts the resolved theme and portal tab
    /// into controlled body classes before rendering; it does not read query strings, reselect Theme, or make
    /// GenericErrorPage depend on theme resolution.
    /// </remarks>
    public class PortalMasterPage : MasterPage
    {
        /// <summary>
        /// 在输出 HTML 前写入稳定的主题与 Tab CSS class。
        /// Writes stable theme and tab CSS classes before HTML is rendered.
        /// </summary>
        /// <param name="e">页面事件参数。Page event arguments.</param>
        protected override void OnPreRender(EventArgs e)
        {
            HtmlGenericControl body = FindControl("PortalBody") as HtmlGenericControl;
            if (body != null)
            {
                body.Attributes["class"] = PortalThemeResolver.GetCurrentCssClass(Context);
            }

            AddModulePackageStyles();

            base.OnPreRender(e);
        }

        /// <summary>
        /// 仅为当前 Tab 的已启用受信任模块包挂载已声明 CSS。
        /// Adds declared CSS only for enabled trusted module packages used by the current Tab.
        /// </summary>
        private void AddModulePackageStyles()
        {
            if (Page == null || Page.Header == null)
            {
                return;
            }

            foreach (PortalModuleStyleResource resource in PortalModuleCatalog.GetActiveStyleResources(Context))
            {
                var link = new HtmlLink();
                link.Attributes["rel"] = "stylesheet";
                link.Attributes["type"] = "text/css";
                link.Href = ResolveUrl(resource.VirtualPath);
                Page.Header.Controls.Add(link);
            }
        }
    }
}
