using System;
using System.IO;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：编辑文档模块项并处理受限服务器文件上传的页面。
    ///
    /// English: Page that edits document-module items and handles restricted server-file uploads.
    /// </summary>
    /// <remarks>
    /// 中文：每个请求都会重新验证模块编辑权限和既有项目归属。服务器上传只接受
    /// <see cref="PortalDocumentPolicy"/> 允许的大小与扩展名，并生成新的物理文件名；
    /// 本页不重新启用数据库文件存储，也不提供私有文件下载授权。
    ///
    /// English: Every request revalidates module-edit permission and existing-item ownership. Server uploads accept only
    /// the size and extensions allowed by <see cref="PortalDocumentPolicy"/> and receive a new physical filename;
    /// this page does not re-enable database file storage or provide private-file download authorization.
    /// </remarks>
    public partial class EditDocs : PortalPage<EditDocs>
    {
        private int _itemId;
        private int _moduleId;
        private bool _hasValidEditContext;
        private IDocumentItem _currentItem;

        /// <summary>
        /// 中文：读取和更新文档模块项目的数据访问依赖。
        ///
        /// English: Data-access dependency used to read and update document-module items.
        /// </summary>
        [Dependency]
        public IDocumentsDb DocumentDB { private get; set; }

        /// <summary>
        /// 中文：验证父 Tab 与模块组合编辑权限的安全服务依赖。
        ///
        /// English: Security-service dependency that validates combined parent-tab and module edit permission.
        /// </summary>
        [Dependency]
        public IPortalSecurity PortalSecurity { private get; set; }

        /// <summary>
        /// 中文：初始化编辑上下文，并在首次请求时绑定已有文档或安全回跳地址。
        ///
        /// English: Initializes the edit context and, on the first request, binds an existing document or a safe return URL.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            _hasValidEditContext = TryInitializeEditContext();
            if (!_hasValidEditContext)
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                if (_currentItem != null)
                {
                    BindDocument(_currentItem);
                }

                // 中文：只保存已经验证在当前应用内的回跳地址，后续仍会再次校验。
                // English: Store only a return URL already verified as inside the current application; it is revalidated later.
                ViewState["UrlReferrer"] = PortalDocumentPolicy.GetSafeReturnUrl(Request);
            }
        }

        /// <summary>
        /// 中文：保存新建或已有文档项目，并在服务器上传时生成唯一物理文件名。
        ///
        /// English: Saves a new or existing document item and generates a unique physical filename for a server upload.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
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
                    if (!PortalDocumentPolicy.TryNormalizeBrowseUrl(PathField.Text, Request, out normalizedUrl))
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
                // 中文：数据库保存失败时删除本次新建文件，避免留下无记录的上传孤儿文件。
                // English: Remove the file created by this request when database save fails, avoiding an unreferenced upload orphan.
                TryDeleteFile(savedPhysicalPath);
                throw;
            }

            PortalDocumentPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// 中文：删除当前已验证归属的文档记录；不删除可能由历史记录共享的物理文件。
        ///
        /// English: Deletes the current document record after ownership validation; it does not delete a physical file that legacy records may share.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
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

            PortalDocumentPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// 中文：取消编辑并返回已验证的门户内地址。
        ///
        /// English: Cancels editing and returns to a verified address inside the Portal.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void CancelBtn_Click(object sender, EventArgs e)
        {
            if (!_hasValidEditContext)
            {
                return;
            }

            PortalDocumentPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
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
                    // 中文：CreateNew 避免即使出现极低概率名称冲突时也覆盖已有文件。
                    // English: CreateNew avoids overwriting an existing file even in the unlikely event of a name collision.
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
                // 中文：原异常比清理失败更有诊断价值，因此不覆盖原异常。
                // English: The original exception is more diagnostic than cleanup failure, so do not mask it.
            }
        }
    }
}
