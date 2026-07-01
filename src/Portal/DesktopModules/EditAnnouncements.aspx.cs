using System;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class EditAnnouncements : PortalPage<EditAnnouncements>
    {
        private int itemId;
        private int moduleId;

        [Dependency]
        public IAnnouncementsDb AnnouncementsDB { private get; set; } // 依赖注入：公告数据库访问接口

        [Dependency]
        public IPortalSecurity PortalSecurity { private get; set; } // 依赖注入：门户安全接口

        // Page_Load 事件处理程序用于获取要编辑的模块和公告的 ID
        // 并使用 ASPNET.StarterKit.Portal.AnnouncementsDB 组件填充页面的编辑控件
        protected void Page_Load(object sender, EventArgs e)
        {
            // 获取公告模块的 ModuleId
            moduleId = Int32.Parse(Request.Params["Mid"]);

            // 验证当前用户是否有权限编辑此模块
            if (!PortalSecurity.HasEditPermissions(moduleId))
            {
                Response.Redirect("~/Admin/EditAccessDenied.aspx"); // 如果没有权限，则重定向到无权限页面
            }

            // 获取要更新的公告的 ItemId
            if (Request.Params["ItemId"] != null)
            {
                itemId = Int32.Parse(Request.Params["ItemId"]);
            }

            // 如果页面不是回发请求，则检查是否有 ItemId，如果有则填充页面内容
            if (!Page.IsPostBack)
            {
                if (itemId != 0)
                {
                    // 获取单个公告的信息
                    var item = AnnouncementsDB.GetSingleAnnouncement(itemId);

                    // 安全检查：验证 ItemId 是否属于当前模块
                    if (item.ModuleId != moduleId)
                    {
                        Response.Redirect("~/Admin/EditAccessDenied.aspx"); // 如果 ItemId 不属于当前模块，则重定向到无权限页面
                    }

                    // 填充页面上的控件
                    TitleField.Text = item.Title;
                    MoreLinkField.Text = item.MoreLink;
                    MobileMoreField.Text = item.MobileMoreLink;
                    DescriptionField.Text = item.Description;
                    ExpireField.Text = item.ExpireDate.Value.ToShortDateString();
                    CreatedBy.Text = item.CreatedByUser;
                    CreatedDate.Text = item.CreatedDate.Value.ToShortDateString();
                }

                // 存储返回地址以便用户返回到门户主页
                ViewState["UrlReferrer"] = Request.UrlReferrer?.ToString();
            }
        }

        // UpdateBtn_Click 事件处理程序用于创建或更新公告
        // 使用 ASPNET.StarterKit.Portal.AnnouncementsDB 组件封装所有数据操作功能
        protected void UpdateBtn_Click(Object sender, EventArgs e)
        {
            // 只有当输入的数据有效时才进行更新操作
            if (Page.IsValid)
            {
                // 如果 ItemId 为 0，则添加新公告；否则更新现有公告
                if (itemId == 0)
                {
                    // 添加公告到数据库
                    AnnouncementsDB.AddAnnouncement(moduleId, Context.User.Identity.Name, TitleField.Text, DateTime.Parse(ExpireField.Text), DescriptionField.Text, MoreLinkField.Text, MobileMoreField.Text);
                }
                else
                {
                    // 更新公告
                    AnnouncementsDB.UpdateAnnouncement(itemId, Context.User.Identity.Name, TitleField.Text, DateTime.Parse(ExpireField.Text), DescriptionField.Text, MoreLinkField.Text, MobileMoreField.Text);
                }

                // 重定向回存储的返回地址
                Response.Redirect((String)ViewState["UrlReferrer"]);
            }
        }

        // DeleteBtn_Click 事件处理程序用于删除公告
        // 使用 ASPNET.StarterKit.Portal.AnnouncementsDB 组件封装所有数据操作功能
        protected void DeleteBtn_Click(Object sender, EventArgs e)
        {
            // 只有当 ItemId 不为 0 时才尝试删除公告（新公告的 ItemId 为 0）
            if (itemId != 0)
            {
                // 删除公告
                AnnouncementsDB.DeleteAnnouncement(itemId);
            }

            // 重定向回存储的返回地址
            Response.Redirect((String)ViewState["UrlReferrer"]);
        }

        // CancelBtn_Click 事件处理程序用于取消当前操作并返回门户主页
        protected void CancelBtn_Click(Object sender, EventArgs e)
        {
            // 重定向回存储的返回地址
            Response.Redirect((String)ViewState["UrlReferrer"]);
        }
    }
}