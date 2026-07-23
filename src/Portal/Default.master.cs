using System;
using System.Web.UI;
using System.Web.UI.HtmlControls;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户默认 Master Page 的主题 CSS 作用域宿主。</zh-CN>
    ///   <en>Theme CSS-scope host for the portal default Master Page.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>页面 Theme 已在 PortalPage.PreInit 中确定。本类只在渲染前把已解析主题和门户 Tab 转为受控 body class，不读取查询字符串、不重新选择 Theme，也不使 GenericErrorPage 依赖主题解析。主题包 resources 列表不在此处作为通用资源协议处理；本类仅按模块 catalog 结果加载当前 Tab 已启用模块包声明的 CSS。</zh-CN>
    ///   <en>Page Theme is decided in PortalPage.PreInit. This class only converts the resolved theme and portal tab into controlled body classes before rendering; it does not read query strings, reselect Theme, or make GenericErrorPage depend on theme resolution. The theme-package resources list is not handled here as a general resource protocol; this class loads only CSS declared by enabled module packages used by the current Tab.</en>
    /// </lang>
    /// </remarks>
    public class PortalMasterPage : MasterPage
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>在输出 HTML 前写入稳定的主题与 Tab CSS class，并挂载已验证模块包 CSS。</zh-CN>
        ///   <en>Writes stable theme and Tab CSS classes and adds validated module-package CSS before HTML is rendered.</en>
        /// </lang>
        /// </summary>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>页面事件参数。</zh-CN>
        ///   <en>Page event arguments.</en>
        /// </l>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>原生 Theme 已在 <c>PreInit</c> 选择；此处仅补充 body 作用域和 catalog 已验证的模块 CSS，不重选主题、不读取请求输入，也不自动加载 JavaScript。</zh-CN>
        ///   <en>Native Theme selection has already completed in <c>PreInit</c>; this method only adds body scope classes and catalog-validated module CSS. It does not reselect Theme, read request input, or auto-load JavaScript.</en>
        /// </lang>
        /// </remarks>
        protected override void OnPreRender(EventArgs e)
        {
            // <lang>
            //   <zh-CN>先把当前请求已解析的主题作用域写到 body；若页面没有标准 body，后续资源挂载仍可独立跳过。</zh-CN>
            //   <en>Write the resolved theme scope to the body first; if the page has no standard body, later resource attachment can still skip independently.</en>
            // </lang>
            HtmlGenericControl body = FindControl("PortalBody") as HtmlGenericControl;
            if (body != null)
            {
                body.Attributes["class"] = PortalThemeResolver.GetCurrentCssClass(Context);
            }

            // <lang>
            //   <zh-CN>模块包样式在 body class 之后挂载，确保主题基线先到位，再由受信任模块包补充局部样式。</zh-CN>
            //   <en>Attach module-package styles after body classes so the theme baseline is established before trusted module packages add local styling.</en>
            // </lang>
            AddModulePackageStyles();

            base.OnPreRender(e);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>仅为当前 Tab 的已启用受信任模块包挂载已声明 CSS。</zh-CN>
        ///   <en>Adds declared CSS only for enabled trusted module packages used by the current Tab.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>资源由模块 catalog 去重并校验为站内路径。本方法不加载主题 manifest 资源、外部 URL 或 JavaScript，并在无 Header 的页面静默跳过。</zh-CN>
        ///   <en>Resources are de-duplicated and validated as site-local paths by the module catalog. This method does not load theme-manifest resources, external URLs, or JavaScript, and silently skips pages without a Header.</en>
        /// </lang>
        /// </remarks>
        private void AddModulePackageStyles()
        {
            if (Page == null || Page.Header == null)
            {
                return;
            }

            // <lang>
            //   <zh-CN>这里信任的是 catalog 返回的最终资源集合；路径校验和去重不在 Master Page 中重复实现。</zh-CN>
            //   <en>This point trusts the final resource set returned by the catalog; path validation and de-duplication are not reimplemented in the Master Page.</en>
            // </lang>
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
