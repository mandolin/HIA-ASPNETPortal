using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：编辑公告模块条目的页面。
    ///
    /// English: Page for editing announcement-module items.
    /// </summary>
    public partial class EditAnnouncements : PortalPage<EditAnnouncements>
    {
        private int itemId;
        private int moduleId;

        /// <summary>
        /// 中文：公告数据访问服务。English: Announcement data-access service.
        /// </summary>
        [Dependency]
        public IAnnouncementsDb AnnouncementsDB { private get; set; }

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
            IAnnouncementItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                if (item != null)
                {
                    TitleField.Text = item.Title;
                    MoreLinkField.Text = item.MoreLink;
                    MobileMoreField.Text = item.MobileMoreLink;
                    DescriptionField.Text = item.Description;
                    ExpireField.Text = item.ExpireDate.HasValue ? item.ExpireDate.Value.ToShortDateString() : string.Empty;
                    CreatedBy.Text = EncodeDisplayText(item.CreatedByUser);
                    CreatedDate.Text = item.CreatedDate.HasValue ? item.CreatedDate.Value.ToShortDateString() : string.Empty;
                }

                ViewState["UrlReferrer"] = PortalNavigationPolicy.GetSafeReturnUrl(Request);
            }
        }

        /// <summary>
        /// 中文：创建或更新已授权公告，并回跳到当前应用内的安全地址。
        ///
        /// English: Creates or updates an authorized announcement and returns to a safe URL inside the current application.
        /// </summary>
        protected void UpdateBtn_Click(object sender, EventArgs e)
        {
            IAnnouncementItem item;
            if (!TryInitializeRequest(out item) || !Page.IsValid)
            {
                return;
            }

            DateTime expireDate;
            if (!DateTime.TryParse(ExpireField.Text, out expireDate))
            {
                ShowValidationMessage("请输入有效的到期日期。");
                return;
            }

            string moreLink;
            string mobileMoreLink;
            if (!TryNormalizeOptionalBrowseUrl(MoreLinkField.Text, out moreLink) ||
                !TryNormalizeOptionalBrowseUrl(MobileMoreField.Text, out mobileMoreLink))
            {
                ShowValidationMessage("“查看更多”链接只能使用站内地址或 HTTP(S) 地址。");
                return;
            }

            if (itemId == 0)
            {
                AnnouncementsDB.AddAnnouncement(moduleId, Context.User.Identity.Name, TitleField.Text, expireDate,
                    DescriptionField.Text, moreLink, mobileMoreLink);
            }
            else
            {
                AnnouncementsDB.UpdateAnnouncement(itemId, Context.User.Identity.Name, TitleField.Text, expireDate,
                    DescriptionField.Text, moreLink, mobileMoreLink);
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// 中文：删除已核验归属的公告。English: Deletes an announcement whose ownership has been verified.
        /// </summary>
        protected void DeleteBtn_Click(object sender, EventArgs e)
        {
            IAnnouncementItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            if (itemId != 0)
            {
                AnnouncementsDB.DeleteAnnouncement(itemId);
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// 中文：放弃编辑并返回安全地址。English: Cancels editing and returns to a safe URL.
        /// </summary>
        protected void CancelBtn_Click(object sender, EventArgs e)
        {
            IAnnouncementItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        private bool TryInitializeRequest(out IAnnouncementItem item)
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

            item = AnnouncementsDB.GetSingleAnnouncement(itemId);
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
