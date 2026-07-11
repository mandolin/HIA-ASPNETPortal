using System;
using System.Data;
using System.IO;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class EditDocs : PortalPage<EditDocs>
    {
        private int itemId;
        private int moduleId;

        [Dependency]
        public IDocumentsDb DocumentDB { private get; set; } // 依赖注入文档数据库接口

        [Dependency]
        public IPortalSecurity PortalSecurity { private get; set; } // 依赖注入门户安全接口

        // 页面加载事件
        // 在此事件中获取模块ID(ModuleId)和项目ID(ItemId)，并使用数据组件填充页面编辑控件。
        protected void Page_Load(object sender, EventArgs e)
        {
            // 获取公告模块的ModuleId
            moduleId = Int32.Parse(Request.Params["Mid"]);

            // 验证当前用户是否有编辑此模块的权限
            if (!PortalSecurity.HasEditPermissions(moduleId))
            {
                Response.Redirect("~/Admin/EditAccessDenied.aspx");
            }

            // 获取要更新的文档的ItemId
            if (Request.Params["ItemId"] != null)
            {
                itemId = Int32.Parse(Request.Params["ItemId"]);
            }

            // 如果页面不是回发，则检查是否指定了文档的ItemId值，
            // 如果有，则用文档详细信息填充页面内容。
            if (!Page.IsPostBack)
            {
                if (itemId != 0)
                {
                    // 获取单个文档的信息
                    IDocumentItem item = DocumentDB.GetSingleDocument(itemId);

                    // 安全检查：验证itemid是否属于该模块
                    if (item.ModuleId != moduleId)
                    {
                        Response.Redirect("~/Admin/EditAccessDenied.aspx");
                    }

                    // 将文档详细信息填充到页面控件中
                    NameField.Text = item.FileFriendlyName;
                    PathField.Text = item.FileNameUrl;
                    CategoryField.Text = item.Category;
                    CreatedBy.Text = item.CreatedByUser;
                    CreatedDate.Text = item.CreatedDate.Value.ToShortDateString();
                }

                // 将重定向地址存储在ViewState中，以便在保存后返回门户页面
                ViewState["UrlReferrer"] = Request.UrlReferrer?.ToString();
            }
        }

        // 更新按钮点击事件处理器
        // 用于创建或更新文档，使用数据组件封装所有数据功能。
        protected void UpdateBtn_Click(Object sender, EventArgs e)
        {
            // 只有当输入数据有效时才进行更新
            if (Page.IsValid)
            {
                if (!ValidateUploadedFile())
                {
                    return;
                }

                if (storeInDatabase.Checked)
                {
                    UploadMessage.Text = "数据库文件存储暂未启用，请选择上传到服务器。";
                    return;
                }

                // 判断是否有文件上传
                if (Upload.Checked && HasUploadedFile())
                {
                    // 计算新上传文件的虚拟路径
                    string virtualPath = "~/uploads/" + Path.GetFileName(FileUpload.PostedFile.FileName);

                    // 计算新上传文件的物理路径
                    string phyiscalPath = Server.MapPath(virtualPath);

                    Directory.CreateDirectory(Path.GetDirectoryName(phyiscalPath));

                    // 将文件保存到上传目录
                    FileUpload.PostedFile.SaveAs(phyiscalPath);

                    // 更新PathFile为上传的虚拟文件位置
                    PathField.Text = virtualPath;
                }

                // 更新文档
                DocumentDB.UpdateDocument(moduleId, itemId, Context.User.Identity.Name, NameField.Text,
                                          PathField.Text, CategoryField.Text, new byte[0], 0, "");

                // 重定向回门户首页
                Response.Redirect((string)ViewState["UrlReferrer"]);
            }
        }

        private bool ValidateUploadedFile()
        {
            if (!HasUploadedFile())
            {
                return true;
            }

            int maxBytes = GetMaxDocumentUploadBytes();
            if (FileUpload.PostedFile.ContentLength <= maxBytes)
            {
                return true;
            }

            UploadMessage.Text = "上传文件不能超过 " + FormatFileSize(maxBytes) + "。当前文件大小为 " +
                                 FormatFileSize(FileUpload.PostedFile.ContentLength) + "。";
            return false;
        }

        private bool HasUploadedFile()
        {
            return FileUpload.PostedFile != null && FileUpload.PostedFile.ContentLength > 0;
        }

        private static int GetMaxDocumentUploadBytes()
        {
            return PortalRuntimeSettings.GetInt32(PortalSettingsRegistry.MaxUploadBytes);
        }

        private static string FormatFileSize(int bytes)
        {
            return (bytes / 1024d / 1024d).ToString("0.##") + " MB";
        }

        // 删除按钮点击事件处理器
        // 用于删除文档，使用数据组件封装所有数据功能。
        protected void DeleteBtn_Click(Object sender, EventArgs e)
        {
            // 只有在存在现有项时尝试删除（新项的"ItemId"为0）
            if (itemId != 0)
            {
                DocumentDB.DeleteDocument(itemId);
            }

            // 重定向回门户首页
            Response.Redirect((string)ViewState["UrlReferrer"]);
        }

        // 取消按钮点击事件处理器
        // 用于取消当前页面操作，并将用户返回到门户首页。
        protected void CancelBtn_Click(Object sender, EventArgs e)
        {
            // 重定向回门户首页
            Response.Redirect((string)ViewState["UrlReferrer"]);
        }
    }
}
