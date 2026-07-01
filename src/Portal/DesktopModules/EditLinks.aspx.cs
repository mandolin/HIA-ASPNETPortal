using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class EditLinks : PortalPage<EditLinks>
    {
        // 存储链接项ID的私有字段
        private int itemId;
        // 存储模块ID的私有字段
        private int moduleId;

        // 依赖注入链接数据库接口
        [Dependency]
        public ILinksDb LinkDB { private get; set; }

        // 依赖注入门户安全接口
        [Dependency]
        public IPortalSecurity PortalSecurity { private get; set; }

        //****************************************************************
        //
        // 页面加载事件：用于获取要编辑的链接项ID。
        // 使用LinkDB组件填充页面上的编辑控件。
        //
        //****************************************************************

        protected void Page_Load(object sender, EventArgs e)
        {
            // 获取模块ID
            moduleId = int.Parse(Request.Params["Mid"]);

            // 验证当前用户是否有编辑此模块的权限
            if (!PortalSecurity.HasEditPermissions(moduleId))
            {
                // 如果没有权限，则重定向到无权访问页面
                Response.Redirect("~/Admin/EditAccessDenied.aspx");
            }

            // 获取链接项ID
            if (Request.Params["ItemId"] != null)
            {
                itemId = int.Parse(Request.Params["ItemId"]);
            }

            // 如果页面首次请求，并且指定了链接项ID，则填充页面内容
            if (!Page.IsPostBack)
            {
                if (itemId != 0)
                {
                    // 获取单个链接项的信息
                    ILinkItem item = LinkDB.GetSingleLink(itemId);

                    // 安全检查：验证itemid是否属于该模块
                    if (item.ModuleId != moduleId)
                    {
                        Response.Redirect("~/Admin/EditAccessDenied.aspx");
                    }

                    // 设置控件的值
                    TitleField.Text = item.Title;
                    DescriptionField.Text = item.Description;
                    UrlField.Text = item.Url;
                    MobileUrlField.Text = item.MobileUrl;
                    ViewOrderField.Text = item.ViewOrder.HasValue ? item.ViewOrder.Value.ToString() : string.Empty;
                    CreatedBy.Text = item.CreatedByUser;
                    CreatedDate.Text = item.CreatedDate.HasValue ? item.CreatedDate.Value.ToShortDateString() : string.Empty;
                }

                // 存储URL引用，以便返回到门户首页
                ViewState["UrlReferrer"] = Request.UrlReferrer?.ToString();
            }
        }

        //****************************************************************
        //
        // 更新按钮点击事件处理程序：用于创建或更新链接。
        // 使用LinkDB组件封装所有数据功能。
        //
        //****************************************************************

        protected void UpdateBtn_Click(Object sender, EventArgs e)
        {
            if (Page.IsValid)
            {
                // 根据itemId判断是添加还是更新链接
                if (itemId == 0)
                {
                    // 添加新的链接到Links表
                    LinkDB.AddLink(moduleId, Context.User.Identity.Name, TitleField.Text, UrlField.Text,
                                   MobileUrlField.Text, int.Parse(ViewOrderField.Text), DescriptionField.Text);
                }
                else
                {
                    // 更新现有的链接
                    LinkDB.UpdateLink(itemId, Context.User.Identity.Name, TitleField.Text, UrlField.Text,
                                      MobileUrlField.Text, int.Parse(ViewOrderField.Text), DescriptionField.Text);
                }

                // 重定向回门户首页
                Response.Redirect((string)ViewState["UrlReferrer"]);
            }
        }

        //****************************************************************
        //
        // 删除按钮点击事件处理程序：用于删除链接。
        // 使用LinkDB组件封装所有数据功能。
        //
        //****************************************************************

        protected void DeleteBtn_Click(Object sender, EventArgs e)
        {
            // 只有在是现有项时才尝试删除（新项的“ItemId”为0）
            if (itemId != 0)
            {
                LinkDB.DeleteLink(itemId);
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