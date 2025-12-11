using System;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class EditEvents : PortalPage<EditEvents>
    {
        // 控件引用
        protected RequiredFieldValidator RequiredFieldValidator1;
        protected RequiredFieldValidator RequiredFieldValidator2;
        protected RequiredFieldValidator RequiredFieldValidator3;

        // 事件项ID
        private int itemId;

        // 模块ID
        private int moduleId;

        // 依赖注入
        [Dependency]
        public IEventsDb EventsDB { private get; set; }

        // 门户安全服务依赖注入
        [Dependency]
        public IPortalSecurity PortalSecurity { private get; set; }

        //****************************************************************
        //
        // 页面加载事件：用于获取要编辑的模块ID和事件项ID。
        // 使用EventsDB组件填充页面上的编辑控件。
        //
        //****************************************************************

        protected void Page_Load(object sender, EventArgs e)
        {
            // 获取事件模块的模块ID
            moduleId = int.Parse(Request.Params["Mid"]);

            // 验证当前用户是否有权限编辑此模块
            if (!PortalSecurity.HasEditPermissions(moduleId))
            {
                Response.Redirect("~/Admin/EditAccessDenied.aspx");
            }

            // 获取要更新的事件项ID
            if (Request.Params["ItemId"] != null)
            {
                itemId = int.Parse(Request.Params["ItemId"]);
            }

            // 如果页面首次加载，检查是否有指定的事件项ID，如果有则填充页面内容
            if (!Page.IsPostBack)
            {
                if (itemId != 0)
                {
                    // 获取单个事件项信息
                    var item = EventsDB.GetSingleEvent(itemId);

                    // 安全检查：验证itemid是否属于该模块
                    if (item.ModuleId != moduleId)
                    {
                        Response.Redirect("~/Admin/EditAccessDenied.aspx");
                    }

                    // 将事件项的数据填充到页面控件中
                    TitleField.Text = item.Title;
                    DescriptionField.Text = item.Description;
                    ExpireField.Text = item.ExpireDate?.ToShortDateString() ?? string.Empty;
                    CreatedBy.Text = item.CreatedByUser;
                    WhereWhenField.Text = item.WhereWhen;
                    CreatedDate.Text = item.CreatedDate?.ToShortDateString() ?? string.Empty;
                }

                // 存储URL引用，以便返回到门户首页
                ViewState["UrlReferrer"] = Request.UrlReferrer?.ToString();
            }
        }

        //****************************************************************
        //
        // 更新按钮点击事件处理程序：用于创建或更新事件。
        // 使用EventsDB组件封装所有数据功能。
        //
        //****************************************************************

        protected void UpdateBtn_Click(Object sender, EventArgs e)
        {
            // 只有当输入的数据有效时才进行更新
            if (Page.IsValid)
            {
                if (itemId == 0)
                {
                    // 添加新事件到数据库表中
                    EventsDB.AddEvent(moduleId, Context.User.Identity.Name, TitleField.Text,
                                      DateTime.Parse(ExpireField.Text), DescriptionField.Text, WhereWhenField.Text);
                }
                else
                {
                    // 更新现有的事件
                    EventsDB.UpdateEvent(itemId, Context.User.Identity.Name, TitleField.Text,
                                         DateTime.Parse(ExpireField.Text), DescriptionField.Text, WhereWhenField.Text);
                }

                // 重定向回门户首页
                Response.Redirect((string)ViewState["UrlReferrer"]);
            }
        }

        //****************************************************************
        //
        // 删除按钮点击事件处理程序：用于删除事件。
        // 使用EventsDB组件封装所有数据功能。
        //
        //****************************************************************

        protected void DeleteBtn_Click(Object sender, EventArgs e)
        {
            // 只有在是现有项目（新项目ItemId为0）时尝试删除
            if (itemId != 0)
            {
                EventsDB.DeleteEvent(itemId);
            }

            // 重定向回门户首页
            Response.Redirect((string)ViewState["UrlReferrer"]);
        }

        //****************************************************************
        //
        // 取消按钮点击事件处理程序：用于取消编辑并返回到门户首页。
        //
        //****************************************************************

        protected void CancelBtn_Click(Object sender, EventArgs e)
        {
            // 重定向回门户首页
            Response.Redirect((string)ViewState["UrlReferrer"]);
        }
    }
}