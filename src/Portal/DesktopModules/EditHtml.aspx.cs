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
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                // <lang>
                //   <zh-CN>首次加载只做读模型回填；后续保存时再统一做权限复核和编码写入，避免旧 Web Forms 回发绕过初始化分支。</zh-CN>
                //   <en>Initial loading only hydrates the edit model; later saves re-check authorization and perform encoded writes, so legacy Web Forms postbacks cannot bypass the initialization branch.</en>
                // </lang>
                IHtmlTextItem item = HtmlTextDB.GetHtmlText(moduleId);
                if (item == null)
                {
                    DesktopText.Text = "Add content...";
                    MobileSummary.Text = "Add summary...";
                    MobileDetails.Text = "Add details...";
                }
                else
                {
                    DesktopText.Text = Server.HtmlDecode(item.DesktopHtml);
                    MobileSummary.Text = Server.HtmlDecode(item.MobileSummary);
                    MobileDetails.Text = Server.HtmlDecode(item.MobileDetails);
                }

                ViewState["UrlReferrer"] = PortalNavigationPolicy.GetSafeReturnUrl(Request);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>以历史编码存储约定保存受信任 HTML 内容。</zh-CN>
        ///   <en>Saves trusted HTML content using the historical encoded-storage convention.</en>
        /// </lang>
        /// </summary>
        protected void UpdateBtn_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            // <lang>
            //   <zh-CN>这里仍按旧门户约定在入库前编码，而不是做 HTML 白名单净化；真正的安全边界来自模块编辑权限和原始 HTML 权限。</zh-CN>
            //   <en>This still follows the legacy portal convention of encoding before persistence instead of sanitizing with an HTML allowlist; the real safety boundary is module-edit authorization plus Raw HTML permission.</en>
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
        protected void CancelBtn_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

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
            string moduleValue = Request.QueryString["Mid"] ?? Request.Unvalidated.Form["Mid"];
            if (!PortalNavigationPolicy.TryReadPositiveInt32(moduleValue, out moduleId) ||
                !PortalSecurity.HasEditPermissions(moduleId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            return PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.ContentRawHtmlEdit);
        }
    }
}
