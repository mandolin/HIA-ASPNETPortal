using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：编辑链接模块条目的页面。
    ///
    /// English: Page for editing link-module items.
    /// </summary>
    public partial class EditLinks : PortalPage<EditLinks>
    {
        private int itemId;
        private int moduleId;

        /// <summary>
        /// 中文：链接数据访问服务。English: Link data-access service.
        /// </summary>
        [Dependency]
        public ILinksDb LinkDB { private get; set; }

        /// <summary>
        /// 中文：模块编辑权限服务。English: Module edit-authorization service.
        /// </summary>
        [Dependency]
        public IPortalSecurity PortalSecurity { private get; set; }

        /// <summary>
        /// 中文：初始化请求上下文、核验编辑权限和现有条目归属，并在首次访问时绑定表单。
        ///
        /// English: Initializes request context, verifies edit permission and existing-item ownership, and binds the form on the first request.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            ILinkItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                if (item != null)
                {
                    TitleField.Text = item.Title;
                    DescriptionField.Text = item.Description;
                    UrlField.Text = item.Url;
                    MobileUrlField.Text = item.MobileUrl;
                    ViewOrderField.Text = item.ViewOrder.HasValue ? item.ViewOrder.Value.ToString() : string.Empty;
                    CreatedBy.Text = EncodeDisplayText(item.CreatedByUser);
                    CreatedDate.Text = item.CreatedDate.HasValue ? item.CreatedDate.Value.ToShortDateString() : string.Empty;
                }

                ViewState["UrlReferrer"] = PortalNavigationPolicy.GetSafeReturnUrl(Request);
            }
        }

        /// <summary>
        /// 中文：创建或更新已授权链接，并回跳到当前应用内的安全地址。
        ///
        /// English: Creates or updates an authorized link and returns to a safe URL inside the current application.
        /// </summary>
        protected void UpdateBtn_Click(object sender, EventArgs e)
        {
            ILinkItem item;
            if (!TryInitializeRequest(out item) || !Page.IsValid)
            {
                return;
            }

            int viewOrder;
            if (!int.TryParse(ViewOrderField.Text, out viewOrder))
            {
                ShowValidationMessage("请输入有效的显示顺序。");
                return;
            }

            string url;
            string mobileUrl;
            if (!PortalNavigationPolicy.TryNormalizeBrowseUrl(UrlField.Text, Request, out url) ||
                !TryNormalizeOptionalBrowseUrl(MobileUrlField.Text, out mobileUrl))
            {
                ShowValidationMessage("链接地址只能使用站内地址或 HTTP(S) 地址。");
                return;
            }

            if (itemId == 0)
            {
                LinkDB.AddLink(moduleId, Context.User.Identity.Name, TitleField.Text, url, mobileUrl, viewOrder,
                    DescriptionField.Text);
            }
            else
            {
                LinkDB.UpdateLink(itemId, Context.User.Identity.Name, TitleField.Text, url, mobileUrl, viewOrder,
                    DescriptionField.Text);
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// 中文：删除已核验归属的链接。English: Deletes a link whose ownership has been verified.
        /// </summary>
        protected void DeleteBtn_Click(object sender, EventArgs e)
        {
            ILinkItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            if (itemId != 0)
            {
                LinkDB.DeleteLink(itemId);
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// 中文：放弃编辑并返回安全地址。English: Cancels editing and returns to a safe URL.
        /// </summary>
        protected void CancelBtn_Click(object sender, EventArgs e)
        {
            ILinkItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        private bool TryInitializeRequest(out ILinkItem item)
        {
            item = null;
            if (!PortalNavigationPolicy.TryReadPositiveInt32(Request.Params["Mid"], out moduleId) ||
                !PortalSecurity.HasEditPermissions(moduleId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            string requestedItemId = Request.Params["ItemId"];
            if (!string.IsNullOrWhiteSpace(requestedItemId) &&
                !PortalNavigationPolicy.TryReadPositiveInt32(requestedItemId, out itemId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            if (itemId == 0)
            {
                return true;
            }

            item = LinkDB.GetSingleLink(itemId);
            if (item == null || item.ModuleId != moduleId)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            return true;
        }

        private bool TryNormalizeOptionalBrowseUrl(string value, out string normalizedUrl)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                normalizedUrl = string.Empty;
                return true;
            }

            return PortalNavigationPolicy.TryNormalizeBrowseUrl(value, Request, out normalizedUrl);
        }

        private void ShowValidationMessage(string message)
        {
            ValidationMessage.Text = message;
            ValidationMessage.Visible = true;
        }

        private string EncodeDisplayText(string value)
        {
            return Server.HtmlEncode(Server.HtmlDecode(value ?? string.Empty));
        }
    }
}
