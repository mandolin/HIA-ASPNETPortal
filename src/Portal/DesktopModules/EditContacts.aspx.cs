using System;
using System.Web;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 编辑联系人信息的页面。
    /// </summary>
    public partial class EditContacts : PortalPage<EditContacts>
    {
        private int itemId;
        private int moduleId;

        [Dependency]
        public IContactsDb ContactsDB { private get; set; } // 用于与数据库交互的接口

        [Dependency]
        public IPortalSecurity PortalSecurity { private get; set; } // 用于安全检查的接口

        /// <summary>
        /// 页面加载事件处理程序，用于获取模块ID和项目ID，并填充页面上的编辑控件。
        /// </summary>
        /// <param name="sender">事件源对象。</param>
        /// <param name="e">事件参数。</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // 解析请求中的模块ID参数
            moduleId = int.Parse(Request.Params["Mid"]);

            // 验证当前用户是否有权限编辑此模块
            if (!PortalSecurity.HasEditPermissions(moduleId))
            {
                Response.Redirect("~/Admin/EditAccessDenied.aspx"); // 重定向到无权限访问页面
            }

            // 解析请求中的项目ID参数
            if (Request.Params["ItemId"] != null)
            {
                itemId = int.Parse(Request.Params["ItemId"]);
            }

            // 如果页面不是回发请求，且项目ID不为0，则填充页面内容
            if (!Page.IsPostBack && itemId != 0)
            {
                // 从数据库获取单个联系人信息
                var item = ContactsDB.GetSingleContact(itemId);

                // 安全检查，确保项目ID属于当前模块
                if (item.ModuleId != moduleId)
                {
                    Response.Redirect("~/Admin/EditAccessDenied.aspx"); // 重定向到无权限访问页面
                }

                // 填充表单控件
                NameField.Text = item.Name;
                RoleField.Text = item.Role;
                EmailField.Text = item.Email;
                Contact1Field.Text = item.Contact1;
                Contact2Field.Text = item.Contact2;
                CreatedBy.Text = item.CreatedByUser;
                CreatedDate.Text = item.CreatedDate.Value.ToShortDateString();
            }

            // 存储URL引用以返回门户首页
            ViewState["UrlReferrer"] = Request.UrlReferrer?.ToString();
        }

        /// <summary>
        /// 更新按钮点击事件处理程序，用于创建或更新联系人信息。
        /// </summary>
        /// <param name="sender">事件源对象。</param>
        /// <param name="e">事件参数。</param>
        protected void UpdateBtn_Click(object sender, EventArgs e)
        {
            // 只有当输入数据有效时才进行更新
            if (Page.IsValid)
            {
                if (itemId == 0)
                {
                    // 添加新的联系人记录
                    ContactsDB.AddContact(moduleId, HttpContext.Current.User.Identity.Name, NameField.Text, RoleField.Text,
                                         EmailField.Text, Contact1Field.Text, Contact2Field.Text);
                }
                else
                {
                    // 更新现有的联系人记录
                    ContactsDB.UpdateContact(itemId, HttpContext.Current.User.Identity.Name, NameField.Text,
                                            RoleField.Text, EmailField.Text, Contact1Field.Text, Contact2Field.Text);
                }

                // 重定向回到门户首页
                Response.Redirect((string)ViewState["UrlReferrer"]);
            }
        }

        /// <summary>
        /// 删除按钮点击事件处理程序，用于删除联系人信息。
        /// </summary>
        /// <param name="sender">事件源对象。</param>
        /// <param name="e">事件参数。</param>
        protected void DeleteBtn_Click(object sender, EventArgs e)
        {
            // 只有当项目ID不为0（即存在记录）时尝试删除
            if (itemId != 0)
            {
                ContactsDB.DeleteContact(itemId); // 删除联系人记录
            }

            // 重定向回到门户首页
            Response.Redirect((string)ViewState["UrlReferrer"]);
        }

        /// <summary>
        /// 取消按钮点击事件处理程序，用于取消编辑并返回门户首页。
        /// </summary>
        /// <param name="sender">事件源对象。</param>
        /// <param name="e">事件参数。</param>
        protected void CancelBtn_Click(object sender, EventArgs e)
        {
            // 重定向回到门户首页
            Response.Redirect((string)ViewState["UrlReferrer"]);
        }
    }
}