using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：编辑受信任 HTML 模块内容的页面。
    ///
    /// English: Page for editing trusted HTML-module content.
    /// </summary>
    /// <remarks>
    /// 中文：本页面保留历史兼容行为：具有模块编辑权限的受信任管理员可以输入原始 HTML，系统以 HTML 编码形式存储，
    /// 渲染时再解码。它不是面向普通用户的富文本入口，未来应由“原始 HTML”细粒度权限替代当前宽泛信任。
    ///
    /// English: This page retains the historical compatibility behavior: a trusted administrator with module-edit permission
    /// may enter raw HTML, which is stored HTML-encoded and decoded during rendering. It is not a general-user rich-text entry;
    /// a future granular Raw HTML permission should replace the current broad trust.
    /// </remarks>
    public partial class EditHtml : PortalPage<EditHtml>
    {
        private int moduleId;

        /// <summary>
        /// 中文：HTML 文本数据访问服务。English: HTML-text data-access service.
        /// </summary>
        [Dependency]
        public IHtmlTextsDb HtmlTextDB { private get; set; }

        /// <summary>
        /// 中文：模块编辑权限服务。English: Module edit-authorization service.
        /// </summary>
        [Dependency]
        public IPortalSecurity PortalSecurity { private get; set; }

        /// <summary>
        /// 中文：初始化受信任 HTML 编辑请求，并在首次访问时读取已有内容或显示首次编辑提示。
        ///
        /// English: Initializes a trusted HTML editing request and reads existing content, or shows first-edit hints, on the first request.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                IHtmlTextItem item = HtmlTextDB.GetHtmlText(moduleId);
                if (item == null)
                {
                    DesktopText.Text = "Todo: Add Content...";
                    MobileSummary.Text = "Todo: Add Content...";
                    MobileDetails.Text = "Todo: Add Content...";
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
        /// 中文：以历史编码存储约定保存受信任 HTML 内容。English: Saves trusted HTML content using the historical encoded-storage convention.
        /// </summary>
        protected void UpdateBtn_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            HtmlTextDB.UpdateHtmlText(
                moduleId,
                Server.HtmlEncode(DesktopText.Text),
                Server.HtmlEncode(MobileSummary.Text),
                Server.HtmlEncode(MobileDetails.Text));
            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// 中文：放弃编辑并返回安全地址。English: Cancels editing and returns to a safe URL.
        /// </summary>
        protected void CancelBtn_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        private bool TryInitializeRequest()
        {
            // 中文：原始 HTML 回发会触发普通 Request.Form 的请求验证；只读取未验证集合中的 Mid 参数。
            // English: A raw-HTML postback triggers request validation on ordinary Request.Form access, so read only Mid from the unvalidated collection.
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
