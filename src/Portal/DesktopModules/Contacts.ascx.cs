using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：显示联系人列表。
    ///
    /// English: Renders the contact list.
    /// </summary>
    public partial class Contacts : PortalModuleControl<Contacts>
    {
        /// <summary>
        /// 中文：联系人数据访问服务。English: Contact data-access service.
        /// </summary>
        [Dependency]
        public IContactsDb ContactsDB { private get; set; }

        /// <summary>
        /// 中文：读取并绑定当前模块联系人。普通字段在标记中通过编码数据绑定输出。
        ///
        /// English: Reads and binds contacts for the current module. Ordinary fields are emitted through encoded data binding in the markup.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            myDataGrid.DataSource = ContactsDB.GetContacts(ModuleId);
            myDataGrid.DataBind();
        }

        /// <summary>
        /// 中文：构建邮件地址链接；空值不会生成链接。English: Builds a mailto link; an empty value produces no link.
        /// </summary>
        protected string GetMailToUrl(object value)
        {
            string email = Convert.ToString(value);
            return string.IsNullOrWhiteSpace(email) ? string.Empty : "mailto:" + email.Trim();
        }

        /// <summary>
        /// 中文：判断是否应显示邮件链接。English: Determines whether the mail link should be shown.
        /// </summary>
        protected bool HasEmail(object value)
        {
            return !string.IsNullOrEmpty(GetMailToUrl(value));
        }
    }
}
