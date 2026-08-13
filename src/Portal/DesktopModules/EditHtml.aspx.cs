using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>编辑受信任 HTML 模块内容的页面。</zh-CN>
    ///   <en>Page for editing trusted HTML-module content.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本页面保留历史兼容行为：具有模块编辑权限的受信任管理员可以输入原始 HTML，系统以 HTML 编码形式存储，渲染时再解码。它不是面向普通用户的富文本入口，未来应由“原始 HTML”细粒度权限替代当前宽泛信任。</zh-CN>
    ///   <en>This page retains the historical compatibility behavior: a trusted administrator with module-edit permission may enter raw HTML, which is stored HTML-encoded and decoded during rendering. It is not a general-user rich-text entry; a future granular Raw HTML permission should replace the current broad trust.</en>
    /// </lang>
    /// </remarks>
    public partial class EditHtml : PortalPage<EditHtml>
    {
        // <lang>
        //   <zh-CN>保存当前请求已通过门禁的模块标识，供首次读取、保存和取消回调共享且不接受客户端二次覆盖。</zh-CN>
        //   <en>Retain the module id that passed the current-request gate so load, save, and cancel callbacks share one validated value without a client-side second override.</en>
        // </lang>
        private int moduleId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>HTML 文本数据访问服务。</zh-CN>
        ///   <en>HTML-text data-access service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IHtmlTextsDb HtmlTextDB { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>模块编辑权限服务。</zh-CN>
        ///   <en>Module edit-authorization service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IPortalSecurity PortalSecurity { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化受信任 HTML 编辑请求，并在首次访问时读取已有内容或显示首次编辑提示。</zh-CN>
        ///   <en>Initializes a trusted HTML editing request and reads existing content, or shows first-edit hints, on the first request.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发页面加载事件的 Web Forms 控件。</zh-CN>
        ///   <en>Web Forms control that raised the load event.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>页面加载事件参数；本回调不依赖其扩展字段。</zh-CN>
        ///   <en>Page-load event arguments; this callback does not depend on extension fields.</en>
        /// </l>
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>所有生命周期分支先通过同一请求门禁，保证无效模块标识或权限缺失不会进入读取/回填路径。</zh-CN>
            //   <en>Run the same request gate before every lifecycle branch so an invalid module id or missing permission cannot enter read or hydration paths.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            // <lang>
            //   <zh-CN>仅首次请求从数据库回填编辑模型；回发保留控件内容，并在保存/取消时重新验证权限。</zh-CN>
            //   <en>Hydrate the edit model only on the first request; postbacks retain control content and revalidate permission on save or cancel.</en>
            // </lang>
            if (!Page.IsPostBack)
            {
                // <lang>
                //   <zh-CN>首次加载只做读模型回填；后续保存时再统一做权限复核和编码写入，避免旧 Web Forms 回发绕过初始化分支。</zh-CN>
                //   <en>Initial loading only hydrates the edit model; later saves re-check authorization and perform encoded writes, so legacy Web Forms postbacks cannot bypass the initialization branch.</en>
                // </lang>
                // <lang>
                //   <zh-CN>按已验证模块标识读取 HTML 文本；服务返回 null 表示首次编辑而不是异常。</zh-CN>
                //   <en>Read HTML text by the validated module id; a null service result means first edit rather than an exception.</en>
                // </lang>
                IHtmlTextItem item = HtmlTextDB.GetHtmlText(moduleId);
                if (item == null)
                {
                    // <lang>
                    //   <zh-CN>没有已有记录时使用固定提示，避免把空数据库值误当成可渲染 HTML。</zh-CN>
                    //   <en>Use fixed prompts when no record exists instead of treating an empty database value as renderable HTML.</en>
                    // </lang>
                    DesktopText.Text = "Add content...";
                    MobileSummary.Text = "Add summary...";
                    MobileDetails.Text = "Add details...";
                }
                else
                {
                    // <lang>
                    //   <zh-CN>历史数据在存储前已编码；回填时只在受信编辑控件内解码，页面输出仍受当前权限门禁保护。</zh-CN>
                    //   <en>Legacy data was encoded before storage; decode only into the trusted edit controls while the page remains protected by the current permission gate.</en>
                    // </lang>
                    DesktopText.Text = Server.HtmlDecode(item.DesktopHtml);
                    MobileSummary.Text = Server.HtmlDecode(item.MobileSummary);
                    MobileDetails.Text = Server.HtmlDecode(item.MobileDetails);
                }

                // <lang>
                //   <zh-CN>保存安全返回地址而不是原始 Referer，后续回调只使用策略组件认可的本地目标。</zh-CN>
                //   <en>Store a safe return target rather than the raw Referer so later callbacks use only a local destination accepted by the navigation policy.</en>
                // </lang>
                ViewState["UrlReferrer"] = PortalNavigationPolicy.GetSafeReturnUrl(Request);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>以历史编码存储约定保存受信任 HTML 内容。</zh-CN>
        ///   <en>Saves trusted HTML content using the historical encoded-storage convention.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发保存事件的按钮控件。</zh-CN>
        ///   <en>Button control that raised the save event.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>按钮点击事件参数；本回调不依赖其扩展字段。</zh-CN>
        ///   <en>Button-click event arguments; this callback does not depend on extension fields.</en>
        /// </l>
        /// </param>
        protected void UpdateBtn_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>保存回调重新执行请求门禁，防止权限或模块目标在首次加载后发生变化而继续写入。</zh-CN>
            //   <en>Re-run the request gate on save so changes after initial load cannot continue to a write with stale authorization or target state.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            // <lang>
            //   <zh-CN>这里仍按旧门户约定在入库前编码，而不是做 HTML 白名单净化；真正的安全边界来自模块编辑权限和原始 HTML 权限。</zh-CN>
            //   <en>This still follows the legacy portal convention of encoding before persistence instead of sanitizing with an HTML allowlist; the real safety boundary is module-edit authorization plus Raw HTML permission.</en>
            // </lang>
            // <lang>
            //   <zh-CN>四个文本值在进入数据访问层前统一 HTML 编码，保持历史存储契约；返回地址仍由同一策略组件处理。</zh-CN>
            //   <en>Encode all four text values before data access to preserve the legacy storage contract, then route the return through the same policy component.</en>
            // </lang>
            HtmlTextDB.UpdateHtmlText(
                moduleId,
                Server.HtmlEncode(DesktopText.Text),
                Server.HtmlEncode(MobileSummary.Text),
                Server.HtmlEncode(MobileDetails.Text));
            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>放弃编辑并返回安全地址。</zh-CN>
        ///   <en>Cancels editing and returns to a safe URL.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发取消事件的按钮控件。</zh-CN>
        ///   <en>Button control that raised the cancel event.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>按钮点击事件参数；本回调不依赖其扩展字段。</zh-CN>
        ///   <en>Button-click event arguments; this callback does not depend on extension fields.</en>
        /// </l>
        /// </param>
        protected void CancelBtn_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>取消也必须通过同一目标和权限门禁，避免未授权请求利用取消回调跳转到未审查流程。</zh-CN>
            //   <en>Cancel also passes the same target and permission gate so an unauthorized request cannot use the callback to enter an unreviewed flow.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            // <lang>
            //   <zh-CN>取消不写数据库，只消费经过校验的安全返回地址。</zh-CN>
            //   <en>Cancel performs no database write and consumes only the previously validated safe return target.</en>
            // </lang>
            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取模块标识并确认当前请求具备编辑该 HTML 模块的权限。</zh-CN>
        ///   <en>Reads the module identifier and verifies that the current request may edit the HTML module.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>请求合法且权限满足时为 <c>true</c>；否则会重定向到编辑拒绝页或通用权限拒绝流程。</zh-CN>
        ///   <en><c>true</c> when the request is valid and authorized; otherwise the method redirects to the edit-denied page or general authorization-denied flow.</en>
        /// </l>
        /// </returns>
        private bool TryInitializeRequest()
        {
            // <lang>
            //   <zh-CN>原始 HTML 回发会触发普通 Request.Form 的请求验证；只读取未验证集合中的 Mid 参数。</zh-CN>
            //   <en>A raw-HTML postback triggers request validation on ordinary Request.Form access, so read only Mid from the unvalidated collection.</en>
            // </lang>
            // <lang>
            //   <zh-CN>优先使用查询字符串中的模块标识，只有原始 HTML 回发场景才从未验证表单读取同名值。</zh-CN>
            //   <en>Prefer the module id from the query string and read the same value from the unvalidated form only for raw-HTML postbacks.</en>
            // </lang>
            string moduleValue = Request.QueryString["Mid"] ?? Request.Unvalidated.Form["Mid"];

            // <lang>
            //   <zh-CN>正整数解析和模块编辑权限必须同时通过；失败时统一重定向并停止调用方的后续分支。</zh-CN>
            //   <en>Positive-integer parsing and module-edit permission must both pass; otherwise redirect through the shared denial flow and stop the caller's branch.</en>
            // </lang>
            if (!PortalNavigationPolicy.TryReadPositiveInt32(moduleValue, out moduleId) ||
                !PortalSecurity.HasEditPermissions(moduleId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            // <lang>
            //   <zh-CN>原始 HTML 是比普通模块编辑更窄的权限边界；由统一授权组件决定是否允许当前上下文继续。</zh-CN>
            //   <en>Raw HTML is a narrower boundary than ordinary module editing; the shared authorization component decides whether this context may continue.</en>
            // </lang>
            return PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.ContentRawHtmlEdit);
        }
    }
}
