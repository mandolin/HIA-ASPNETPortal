using System;
using System.IO;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>编辑文档模块项并处理受限服务器文件上传的页面。</zh-CN>
    ///   <en>Page that edits document-module items and handles restricted server-file uploads.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>每个请求都会重新验证模块编辑权限和既有项目归属。服务器上传只接受 <see cref="PortalDocumentPolicy"/> 允许的大小与扩展名，并生成新的物理文件名； 本页不重新启用数据库文件存储，也不提供私有文件下载授权。</zh-CN>
    ///   <en>Every request revalidates module-edit permission and existing-item ownership. Server uploads accept only the size and extensions allowed by <see cref="PortalDocumentPolicy"/> and receive a new physical filename; this page does not re-enable database file storage or provide private-file download authorization.</en>
    /// </lang>
    /// </remarks>
    public partial class EditDocs : PortalPage<EditDocs>
    {
        private int _itemId;
        private int _moduleId;
        private bool _hasValidEditContext;
        private IDocumentItem _currentItem;

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取和更新文档模块项目的数据访问依赖。</zh-CN>
        ///   <en>Data-access dependency used to read and update document-module items.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IDocumentsDb DocumentDB { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证父 Tab 与模块组合编辑权限的安全服务依赖。</zh-CN>
        ///   <en>Security-service dependency that validates combined parent-tab and module edit permission.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IPortalSecurity PortalSecurity { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化编辑上下文，并在首次请求时绑定已有文档或安全回跳地址。</zh-CN>
        ///   <en>Initializes the edit context and, on the first request, binds an existing document or a safe return URL.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            _hasValidEditContext = TryInitializeEditContext();
            if (!_hasValidEditContext)
            {
                return;
            }

            ApplyUploadPolicyPresentation();

            if (!Page.IsPostBack)
            {
                if (_currentItem != null)
                {
                    BindDocument(_currentItem);
                }

                // <lang>
                //   <zh-CN>只保存已经验证在当前应用内的回跳地址，后续仍会再次校验。</zh-CN>
                //   <en>Store only a return URL already verified as inside the current application; it is revalidated later.</en>
                // </lang>
                ViewState["UrlReferrer"] = PortalNavigationPolicy.GetSafeReturnUrl(Request);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>保存新建或已有文档项目，并在服务器上传时生成唯一物理文件名。</zh-CN>
        ///   <en>Saves a new or existing document item and generates a unique physical filename for a server upload.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void UpdateBtn_Click(object sender, EventArgs e)
        {
            if (!_hasValidEditContext || !Page.IsValid)
            {
                return;
            }

            if (storeInDatabase.Checked)
            {
                UploadMessage.Text = "数据库文件存储暂未启用，请选择上传到服务器或填写浏览地址。";
                return;
            }

            string savedPhysicalPath = null;
            try
            {
                if (Upload.Checked)
                {
                    if (!HasUploadedFile())
                    {
                        UploadMessage.Text = "已选择上传到服务器，请选择要上传的文件。";
                        return;
                    }

                    string virtualPath;
                    if (!TrySaveUploadedFile(out virtualPath, out savedPhysicalPath))
                    {
                        return;
                    }

                    PathField.Text = virtualPath;
                }
                else
                {
                    string normalizedUrl;
                    if (!PortalNavigationPolicy.TryNormalizeBrowseUrl(PathField.Text, Request, out normalizedUrl))
                    {
                        UploadMessage.Text = "请输入应用内相对地址或 http/https 浏览地址。";
                        return;
                    }

                    PathField.Text = normalizedUrl;
                }

                DocumentDB.UpdateDocument(
                    _moduleId,
                    _itemId,
                    Context.User.Identity.Name,
                    NameField.Text,
                    PathField.Text,
                    CategoryField.Text,
                    new byte[0],
                    0,
                    string.Empty);
            }
            catch
            {
                // <lang>
                //   <zh-CN>数据库保存失败时删除本次新建文件，避免留下无记录的上传孤儿文件。</zh-CN>
                //   <en>Remove the file created by this request when database save fails, avoiding an unreferenced upload orphan.</en>
                // </lang>
                TryDeleteFile(savedPhysicalPath);
                throw;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除当前已验证归属的文档记录；不删除可能由历史记录共享的物理文件。</zh-CN>
        ///   <en>Deletes the current document record after ownership validation; it does not delete a physical file that legacy records may share.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void DeleteBtn_Click(object sender, EventArgs e)
        {
            if (!_hasValidEditContext)
            {
                return;
            }

            if (_itemId != 0)
            {
                DocumentDB.DeleteDocument(_itemId);
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>取消编辑并返回已验证的门户内地址。</zh-CN>
        ///   <en>Cancels editing and returns to a verified address inside the Portal.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void CancelBtn_Click(object sender, EventArgs e)
        {
            if (!_hasValidEditContext)
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        private bool TryInitializeEditContext()
        {
            if (!TryReadOptionalPositiveId(Request.Params["Mid"], out _moduleId) || _moduleId == 0 ||
                !PortalSecurity.HasEditPermissions(_moduleId))
            {
                RedirectToEditAccessDenied();
                return false;
            }

            if (!TryReadOptionalPositiveId(Request.Params["ItemId"], out _itemId))
            {
                RedirectToEditAccessDenied();
                return false;
            }

            if (_itemId == 0)
            {
                return true;
            }

            _currentItem = DocumentDB.GetSingleDocument(_itemId);
            if (_currentItem == null || _currentItem.ModuleId != _moduleId)
            {
                RedirectToEditAccessDenied();
                return false;
            }

            return true;
        }

        private void BindDocument(IDocumentItem item)
        {
            NameField.Text = item.FileFriendlyName;
            PathField.Text = item.FileNameUrl;
            CategoryField.Text = item.Category;
            CreatedBy.Text = item.CreatedByUser;
            CreatedDate.Text = item.CreatedDate.HasValue ? item.CreatedDate.Value.ToShortDateString() : string.Empty;
        }

        private void ApplyUploadPolicyPresentation()
        {
            // <lang>
            //   <zh-CN>数据库二进制存储路线本阶段不启用，页面层也强制清空避免旧提交值误入库。</zh-CN>
            //   <en>Database-binary storage is disabled in this phase; clear it at page level to avoid legacy posts.</en>
            // </lang>
            storeInDatabase.Checked = false;
            storeInDatabase.Enabled = false;
            UploadPolicyHint.Text = "单文件上限：" + PortalDocumentPolicy.GetMaximumUploadSizeDisplayText() +
                                    "；允许扩展名：" + PortalDocumentPolicy.GetAllowedExtensionsDisplayText() +
                                    "。服务器上传会重命名后保存到 " + PortalDocumentPolicy.UploadVirtualDirectory + "。";
        }

        private bool TrySaveUploadedFile(out string virtualPath, out string savedPhysicalPath)
        {
            virtualPath = string.Empty;
            savedPhysicalPath = null;

            string errorMessage;
            if (!PortalDocumentPolicy.TryValidateUpload(FileUpload.PostedFile, out errorMessage))
            {
                UploadMessage.Text = errorMessage;
                return false;
            }

            string uploadDirectory = Server.MapPath(PortalDocumentPolicy.UploadVirtualDirectory);
            Directory.CreateDirectory(uploadDirectory);

            for (int attempt = 0; attempt < 5; attempt++)
            {
                string fileName = PortalDocumentPolicy.CreateStorageFileName(FileUpload.PostedFile.FileName);
                string physicalPath = Path.Combine(uploadDirectory, fileName);
                try
                {
                    // <lang>
                    //   <zh-CN>CreateNew 避免即使出现极低概率名称冲突时也覆盖已有文件。</zh-CN>
                    //   <en>CreateNew avoids overwriting an existing file even in the unlikely event of a name collision.</en>
                    // </lang>
                    using (var output = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        FileUpload.PostedFile.InputStream.CopyTo(output);
                    }

                    virtualPath = PortalDocumentPolicy.GetUploadVirtualPath(fileName);
                    savedPhysicalPath = physicalPath;
                    return true;
                }
                catch (IOException)
                {
                    if (!File.Exists(physicalPath) || attempt == 4)
                    {
                        throw;
                    }
                }
            }

            return false;
        }

        private static bool TryReadOptionalPositiveId(string rawValue, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            return int.TryParse(rawValue, out value) && value > 0;
        }

        private void RedirectToEditAccessDenied()
        {
            Response.Redirect("~/Admin/EditAccessDenied.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private bool HasUploadedFile()
        {
            return FileUpload.PostedFile != null && FileUpload.PostedFile.ContentLength > 0;
        }

        private static void TryDeleteFile(string physicalPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(physicalPath) && File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }
            }
            catch
            {
                // <lang>
                //   <zh-CN>原异常比清理失败更有诊断价值，因此不覆盖原异常。</zh-CN>
                //   <en>The original exception is more diagnostic than cleanup failure, so do not mask it.</en>
                // </lang>
            }
        }
    }
}
