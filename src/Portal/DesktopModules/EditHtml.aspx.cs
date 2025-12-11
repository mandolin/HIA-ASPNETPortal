using System;
using ASPNET.StarterKit.Portal;
using System.Web.UI;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class EditHtml : PortalPage<EditHtml>
    {
        // 存储模块ID的私有字段
        private int moduleId;

        // 依赖注入HTML文本数据库接口
        [Dependency]
        public IHtmlTextsDb HtmlTextDB { private get; set; }

        // 依赖注入门户安全接口
        [Dependency]
        public IPortalSecurity PortalSecurity { private get; set; }

        //****************************************************************
        //
        // 页面加载事件：用于获取要编辑的模块ID。
        // 使用HtmlTextDB组件填充页面上的编辑控件。
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

            // 如果不是回发请求
            if (!Page.IsPostBack)
            {
                // 获取单行文本信息
                IHtmlTextItem item = HtmlTextDB.GetHtmlText(moduleId);

                try
                {
                    // 解码并设置桌面版HTML内容
                    DesktopText.Text = Server.HtmlDecode(item.DesktopHtml);
                    // 解码并设置移动端摘要内容
                    MobileSummary.Text = Server.HtmlDecode(item.MobileSummary);
                    // 解码并设置移动端详情内容
                    MobileDetails.Text = Server.HtmlDecode(item.MobileDetails);
                }
                catch
                {
                    // 如果出现异常，则设置默认提示信息
                    DesktopText.Text = "Todo: Add Content...";
                    MobileSummary.Text = "Todo: Add Content...";
                    MobileDetails.Text = "Todo: Add Content...";
                }

                // 存储URL引用，以便返回到门户首页
                ViewState["UrlReferrer"] = Request.UrlReferrer?.ToString();
            }
        }

        //****************************************************************
        //
        // 更新按钮点击事件处理程序：用于将文本更改保存到数据库。
        //
        //****************************************************************

        protected void UpdateBtn_Click(Object sender, EventArgs e)
        {
            // 更新数据库中的文本
            HtmlTextDB.UpdateHtmlText(
                moduleId,
                Server.HtmlEncode(DesktopText.Text), // 编码桌面版HTML内容
                Server.HtmlEncode(MobileSummary.Text), // 编码移动端摘要内容
                Server.HtmlEncode(MobileDetails.Text) // 编码移动端详情内容
            );

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