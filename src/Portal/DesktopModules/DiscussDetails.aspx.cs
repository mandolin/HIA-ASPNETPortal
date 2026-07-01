using System;
using System.Data;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class DiscussDetails : PortalPage<DiscussDetails>
    {
        private int _itemId;
        private int _moduleId;

        [Dependency]
        public IDiscussionsDb DiscussionDB { private get; set; }

        [Dependency]
        public IPortalSecurity PortalSecurity { private get; set; }

        // Page_Load 事件处理器用于获取讨论列表的 ModuleId 和 ItemId，并显示消息内容
        protected void Page_Load(object sender, EventArgs e)
        {
            // 从查询字符串中获取 moduleId 和 ItemId
            _moduleId = int.Parse(Request.Params["Mid"]);

            if (Request.Params["ItemId"] != null)
            {
                _itemId = int.Parse(Request.Params["ItemId"]);
            }
            else
            {
                _itemId = 0;
                EditPanel.Visible = true;
                ButtonPanel.Visible = false;
            }

            // 如果这是第一次访问页面并且 ItemId 不为零，则填充数据
            if (!Page.IsPostBack && _itemId != 0)
            {
                BindData();
            }

            // 检查用户是否有编辑权限
            if (!PortalSecurity.HasEditPermissions(_moduleId))
            {
                if (_itemId == 0)
                {
                    Response.Redirect("~/Admin/EditAccessDenied.aspx");
                }
                else
                {
                    ReplyBtn.Visible = false;
                }
            }
        }

        // ReplyBtn_Click 事件处理器处理用户点击消息的“回复”按钮的情况
        protected void ReplyBtn_Click(Object Sender, EventArgs e)
        {
            EditPanel.Visible = true;
            ButtonPanel.Visible = false;
        }

        // UpdateBtn_Click 事件处理器处理用户点击“更新”按钮后提交对消息的响应
        protected void UpdateBtn_Click(Object sender, EventArgs e)
        {
            // 添加新消息（更新页面上的 itemId）
            _itemId = DiscussionDB.AddMessage(_moduleId, _itemId, User.Identity.Name,
                                             Server.HtmlEncode(TitleField.Text),
                                             Server.HtmlEncode(BodyField.Text));

            // 更新页面元素的可见性
            EditPanel.Visible = false;
            ButtonPanel.Visible = true;

            // 重新加载页面内容以显示新消息
            BindData();
        }

        // CancelBtn_Click 事件处理器处理用户点击“取消”按钮的情况，用于放弃消息发布并退出编辑模式
        protected void CancelBtn_Click(Object sender, EventArgs e)
        {
            // 更新页面元素的可见性
            EditPanel.Visible = false;
            ButtonPanel.Visible = true;
        }

        // BindData 方法用于从 Discussion 表中获取消息详情，并更新页面上的消息内容
        private void BindData()
        {
            IDiscussionItem item = DiscussionDB.GetSingleMessage(_itemId);

            if (item == null || item.ModuleID != _moduleId)
            {
                Response.Redirect("~/Admin/EditAccessDenied.aspx");
            }

            Subject.Text = item.Title ?? "";
            Body.Text = item.Body ?? "";
            CreatedByUser.Text = item.CreatedByUser ?? "匿名";
            CreatedDate.Text = item.CreatedDate.HasValue
                ? item.CreatedDate.Value.ToString("d")
                : "未知时间";

            TitleField.Text = ReTitle(Subject.Text);

            // 暂时关闭上下篇导航  #note 后期去掉此处及相关按钮
            prevItem.Visible = false;
            nextItem.Visible = false;
        }

        // ReTitle 辅助方法用于创建消息回复的主题行
        private string ReTitle(string title)
        {
            if (!string.IsNullOrEmpty(title) && !title.StartsWith("Re: "))
            {
                title = "Re: " + title;
            }

            return title;
        }
    }
}