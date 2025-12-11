using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class EditXml : PortalPage<EditXml>
    {
        // 存储模块ID的私有字段
        private int moduleId;

        // 依赖注入模块数据库接口
        [Dependency]
        public IModulesDb ModulesConfig { private get; set; }

        // 依赖注入门户安全接口
        [Dependency]
        public IPortalSecurity PortalSecurity { private get; set; }

        //****************************************************************
        //
        // 页面加载事件：用于获取要编辑的模块ID。
        // 使用配置系统填充页面上的编辑控件。
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
                // 确保模块ID大于0，即确保是有效的模块
                if (moduleId > 0)
                {
                    // 从数据库获取设置
                    Hashtable settings = ModulesConfig.GetModuleSettings(moduleId);

                    // 设置XML数据源路径
                    XmlDataSrc.Text = (String)settings["xmlsrc"];
                    // 设置XSL/T转换文件路径
                    XslTransformSrc.Text = (String)settings["xslsrc"];
                }

                // 存储URL引用，以便返回到门户首页
                ViewState["UrlReferrer"] = Request.UrlReferrer?.ToString();
            }
        }

        //****************************************************************
        //
        // 更新按钮点击事件处理程序：用于保存设置到数据库。
        //
        //****************************************************************

        protected void UpdateBtn_Click(Object sender, EventArgs e)
        {
            // 更新数据库中的设置
            ModulesConfig.UpdateModuleSetting(moduleId, "xmlsrc", XmlDataSrc.Text);
            ModulesConfig.UpdateModuleSetting(moduleId, "xslsrc", XslTransformSrc.Text);

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