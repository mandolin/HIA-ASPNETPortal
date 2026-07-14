using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：编辑事件模块条目的页面。
    ///
    /// English: Page for editing event-module items.
    /// </summary>
    public partial class EditEvents : PortalPage<EditEvents>
    {
        private int itemId;
        private int moduleId;

        /// <summary>
        /// 中文：事件数据访问服务。English: Event data-access service.
        /// </summary>
        [Dependency]
        public IEventsDb EventsDB { private get; set; }

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
            IEventItem item;
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
                    ExpireField.Text = item.ExpireDate.HasValue ? item.ExpireDate.Value.ToShortDateString() : string.Empty;
                    CreatedBy.Text = EncodeDisplayText(item.CreatedByUser);
                    WhereWhenField.Text = item.WhereWhen;
                    CreatedDate.Text = item.CreatedDate.HasValue ? item.CreatedDate.Value.ToShortDateString() : string.Empty;
                }

                ViewState["UrlReferrer"] = PortalNavigationPolicy.GetSafeReturnUrl(Request);
            }
        }

        /// <summary>
        /// 中文：创建或更新已授权事件，并回跳到当前应用内的安全地址。
        ///
        /// English: Creates or updates an authorized event and returns to a safe URL inside the current application.
        /// </summary>
        protected void UpdateBtn_Click(object sender, EventArgs e)
        {
            IEventItem item;
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

            if (itemId == 0)
            {
                EventsDB.AddEvent(moduleId, Context.User.Identity.Name, TitleField.Text, expireDate,
                    DescriptionField.Text, WhereWhenField.Text);
            }
            else
            {
                EventsDB.UpdateEvent(itemId, Context.User.Identity.Name, TitleField.Text, expireDate,
                    DescriptionField.Text, WhereWhenField.Text);
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// 中文：删除已核验归属的事件。English: Deletes an event whose ownership has been verified.
        /// </summary>
        protected void DeleteBtn_Click(object sender, EventArgs e)
        {
            IEventItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            if (itemId != 0)
            {
                EventsDB.DeleteEvent(itemId);
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// 中文：放弃编辑并返回安全地址。English: Cancels editing and returns to a safe URL.
        /// </summary>
        protected void CancelBtn_Click(object sender, EventArgs e)
        {
            IEventItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        private bool TryInitializeRequest(out IEventItem item)
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

            item = EventsDB.GetSingleEvent(itemId);
            if (item == null || item.ModuleId != moduleId)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            return true;
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
