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
        ///   <zh-CN>原生 Theme 已在 <c>PreInit</c> 选择；此处先写入 body 作用域，再委托样式 helper 把 catalog 最终候选附加到 Page Header，随后保留基类渲染委托。它不重选 Theme、不复验模块路径/启用状态、不读取请求输入，也不实施页面授权或自动加载 JavaScript。</zh-CN>
        ///   <en>Native Theme selection has already completed in <c>PreInit</c>; this method writes body scope first, then delegates to the style helper to attach final catalog candidates to the Page Header, and finally preserves base rendering delegation. It does not reselect Theme, revalidate module paths or enabled state, read request input, enforce page authorization, or auto-load JavaScript.</en>
        /// </lang>
        /// </remarks>
        protected override void OnPreRender(EventArgs e)
        {
            // <lang>
            //   <zh-CN>先把当前请求已解析的主题作用域写到 body；若页面没有标准 body，后续资源挂载仍可独立跳过。</zh-CN>
            //   <en>Write the resolved theme scope to the body first; if the page has no standard body, later resource attachment can still skip independently.</en>
            // </lang>
            HtmlGenericControl body = FindControl("PortalBody") as HtmlGenericControl;

            // <lang>
            //   <zh-CN>仅在标准 body 存在时写入主题作用域；缺少 body 不阻断随后独立的模块样式 helper，也不临时创建或替换标记节点。</zh-CN>
            //   <en>Write the theme scope only when the standard body exists; a missing body does not block the independent module-style helper and does not create or replace markup nodes.</en>
            // </lang>
            if (body != null)
            {
                // <lang>
                //   <zh-CN>使用当前请求已经解析的受控 CSS class 覆盖 body 的 class 属性；此处不重新选择 Theme，也不合并请求提供的 class 文本。</zh-CN>
                //   <en>Set the body class attribute from the controlled CSS class already resolved for the current request; do not reselect Theme or merge request-supplied class text here.</en>
                // </lang>
                body.Attributes["class"] = PortalThemeResolver.GetCurrentCssClass(Context);
            }

            // <lang>
            //   <zh-CN>模块包样式在 body class 之后挂载，确保主题基线先到位，再由受信任模块包补充局部样式。</zh-CN>
            //   <en>Attach module-package styles after body classes so the theme baseline is established before trusted module packages add local styling.</en>
            // </lang>
            AddModulePackageStyles();

            // <lang>
            //   <zh-CN>在本类的受控 body/样式附加完成后交还 Web Forms 基类渲染生命周期；不提前或跳过该委托。</zh-CN>
            //   <en>Return to the Web Forms base rendering lifecycle after this class completes controlled body/style attachment; do not invoke this delegation early or skip it.</en>
            // </lang>
            base.OnPreRender(e);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将当前 Tab 已启用受信任模块包的最终 CSS 候选附加到页面 Header。</zh-CN>
        ///   <en>Attaches final CSS candidates for enabled trusted module packages in the current Tab to the page Header.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>资源候选的 manifest 信任、启用过滤和去重由模块 catalog 完成；本方法只将结果转换为固定 stylesheet 属性的 server 控件，并使用应用虚拟目录解析其站内路径。它不加载 Theme manifest 资源、外部 URL 或 JavaScript，不复验候选，也不实施页面授权；无 Page 或 Header 时保持既有静默跳过。</zh-CN>
        ///   <en>Manifest trust, enabled filtering, and de-duplication of resource candidates are completed by the module catalog; this method only converts the results into server controls with fixed stylesheet attributes and resolves their site-local paths against the application virtual directory. It does not load Theme-manifest resources, external URLs, or JavaScript, revalidate candidates, or enforce page authorization; it retains the existing silent skip when Page or Header is absent.</en>
        /// </lang>
        /// </remarks>
        private void AddModulePackageStyles()
        {
            // <lang>
            //   <zh-CN>没有当前 Page 或可附加控件的 Header 时无法安全输出 link；保持空操作而不构造替代 Header 或改变页面结构。</zh-CN>
            //   <en>Without a current Page or a Header that can receive controls, link output is not safe; keep a no-op rather than constructing a replacement Header or changing page structure.</en>
            // </lang>
            if (Page == null || Page.Header == null)
            {
                // <lang>
                //   <zh-CN>静默结束以保留旧页面/测试宿主没有 Header 时的兼容行为，不把布局状态暴露为渲染异常。</zh-CN>
                //   <en>End silently to preserve compatibility for legacy pages or test hosts without a Header, without exposing layout state as a rendering exception.</en>
                // </lang>
                return;
            }

            // <lang>
            //   <zh-CN>这里仅消费 catalog 返回的最终资源集合；路径信任、启用过滤和去重不在 Master Page 中重复实现，也不把候选输出当作授权结论。</zh-CN>
            //   <en>Consume only the final resource set returned by the catalog; path trust, enabled filtering, and de-duplication are not reimplemented in the Master Page, and candidate output is not treated as an authorization conclusion.</en>
            // </lang>
            foreach (PortalModuleStyleResource resource in PortalModuleCatalog.GetActiveStyleResources(Context))
            {
                // <lang>
                //   <zh-CN>为单个已验证 CSS 候选创建短生命周期 server link 控件；控件仅在当前页面渲染时存活，不回写 catalog 或包元数据。</zh-CN>
                //   <en>Create a short-lived server link control for one validated CSS candidate; the control lives only for the current page render and does not write back to the catalog or package metadata.</en>
                // </lang>
                var link = new HtmlLink();

                // <lang>
                //   <zh-CN>固定 rel/type 以表达样式表语义；属性值不来自模块、请求或页面输入，避免将此输出点扩展为通用标签注入。</zh-CN>
                //   <en>Fix rel/type to express stylesheet semantics; attribute values do not come from module, request, or page input, preventing this output point from becoming general tag injection.</en>
                // </lang>
                link.Attributes["rel"] = "stylesheet";
                link.Attributes["type"] = "text/css";

                // <lang>
                //   <zh-CN>将 catalog 给出的应用相对站内路径解析为当前虚拟目录下可用的 URL；ResolveUrl 不承担第二次资源验证或外部 URL 许可。</zh-CN>
                //   <en>Resolve the application-relative site-local path supplied by the catalog into a URL usable below the current virtual directory; ResolveUrl does not perform a second resource validation or authorize external URLs.</en>
                // </lang>
                link.Href = ResolveUrl(resource.VirtualPath);

                // <lang>
                //   <zh-CN>按 catalog 已确定的稳定顺序把 link 附加到现有 Header；不在此层跨请求缓存、重排或额外去重控件。</zh-CN>
                //   <en>Append the link to the existing Header in the stable order already determined by the catalog; do not cache across requests, reorder, or perform additional control de-duplication at this layer.</en>
                // </lang>
                Page.Header.Controls.Add(link);
            }
        }
    }
}
