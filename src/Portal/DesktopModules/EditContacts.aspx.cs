using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：编辑联系人模块条目的页面。
    ///
    /// English: Page for editing contact-module items.
    /// </summary>
    public partial class EditContacts : PortalPage<EditContacts>
    {
        private int itemId;
        private int moduleId;

        /// <summary>
        /// 中文：联系人数据访问服务。English: Contact data-access service.
        /// </summary>
        [Dependency]
        public IContactsDb ContactsDB { private get; set; }

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
            IContactItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                if (item != null)
                {
                    NameField.Text = item.Name;
                    RoleField.Text = item.Role;
                    EmailField.Text = item.Email;
                    Contact1Field.Text = item.Contact1;
                    Contact2Field.Text = item.Contact2;
                    CreatedBy.Text = EncodeDisplayText(item.CreatedByUser);
                    CreatedDate.Text = item.CreatedDate.HasValue ? item.CreatedDate.Value.ToShortDateString() : string.Empty;
                }

                ViewState["UrlReferrer"] = PortalNavigationPolicy.GetSafeReturnUrl(Request);
            }
        }

        /// <summary>
        /// 中文：创建或更新已授权联系人，并回跳到当前应用内的安全地址。
        ///
        /// English: Creates or updates an authorized contact and returns to a safe URL inside the current application.
        /// </summary>
        protected void UpdateBtn_Click(object sender, EventArgs e)
        {
            IContactItem item;
            if (!TryInitializeRequest(out item) || !Page.IsValid)
            {
                return;
            }

            if (itemId == 0)
            {
                ContactsDB.AddContact(moduleId, Context.User.Identity.Name, NameField.Text, RoleField.Text,
                    EmailField.Text, Contact1Field.Text, Contact2Field.Text);
            }
            else
            {
                ContactsDB.UpdateContact(itemId, Context.User.Identity.Name, NameField.Text, RoleField.Text,
                    EmailField.Text, Contact1Field.Text, Contact2Field.Text);
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// 中文：删除已核验归属的联系人。English: Deletes a contact whose ownership has been verified.
        /// </summary>
        protected void DeleteBtn_Click(object sender, EventArgs e)
        {
            IContactItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            if (itemId != 0)
            {
                ContactsDB.DeleteContact(itemId);
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// 中文：放弃编辑并返回安全地址。English: Cancels editing and returns to a safe URL.
        /// </summary>
        protected void CancelBtn_Click(object sender, EventArgs e)
        {
            IContactItem item;
            if (!TryInitializeRequest(out item))
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        private bool TryInitializeRequest(out IContactItem item)
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

            item = ContactsDB.GetSingleContact(itemId);
            if (item == null || item.ModuleId != moduleId)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            return true;
        }

        private string EncodeDisplayText(string value)
        {
            return Server.HtmlEncode(Server.HtmlDecode(value ?? string.Empty));
        }
    }
}
