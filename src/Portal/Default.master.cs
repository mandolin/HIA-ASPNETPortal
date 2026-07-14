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
        /// 主题包 resources 列表不在此处作为通用资源协议处理；本类仅按模块 catalog 结果加载当前 Tab 已启用模块包声明的 CSS。
        /// Page Theme is decided in PortalPage.PreInit. This class only converts the resolved theme and portal tab
        /// into controlled body classes before rendering; it does not read query strings, reselect Theme, or make
        /// GenericErrorPage depend on theme resolution. The theme-package resources list is not handled here as a general
        /// resource protocol; this class loads only CSS declared by enabled module packages used by the current Tab.
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
        /// <remarks>
        /// 资源由模块 catalog 去重并校验为站内路径。本方法不加载主题 manifest 资源、外部 URL 或 JavaScript，
        /// 并在无 Header 的页面静默跳过。
        /// Resources are de-duplicated and validated as site-local paths by the module catalog. This method does not load
        /// theme-manifest resources, external URLs, or JavaScript, and silently skips pages without a Header.
        /// </remarks>
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
