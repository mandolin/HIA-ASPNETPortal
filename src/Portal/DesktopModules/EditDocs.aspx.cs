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
            // <lang>
            //   <zh-CN>每次请求都重新建立并校验编辑上下文；无效上下文立即停止，首次请求才绑定文档和安全回跳。</zh-CN>
            //   <en>Rebuild and validate the edit context on every request; stop immediately for invalid context, and bind the document and safe return URL only on the first request.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>保存前要求编辑上下文和页面校验都通过，并显式拒绝已停用的数据库文件存储路线。</zh-CN>
            //   <en>Require a valid edit context and page validation before saving, and explicitly reject the disabled database-file-storage route.</en>
            // </lang>
            if (!_hasValidEditContext || !Page.IsValid)
            {
                return;
            }

            if (storeInDatabase.Checked)
            {
                UploadMessage.Text = "数据库文件存储暂未启用，请选择上传到服务器或填写浏览地址。";
                return;
            }

            // <lang>
            //   <zh-CN>仅跟踪本次请求新建的物理文件，数据库保存失败时用于定向清理。</zh-CN>
            //   <en>Track only the physical file created by this request so it can be targeted for cleanup if database persistence fails.</en>
            // </lang>
            string savedPhysicalPath = null;
            try
            {
                if (Upload.Checked)
                {
                    // <lang>
                    //   <zh-CN>上传路径必须先确认存在文件，再经过统一大小/扩展名策略校验。</zh-CN>
                    //   <en>The upload path must confirm a file exists before applying the shared size and extension policy.</en>
                    // </lang>
                    if (!HasUploadedFile())
                    {
                        UploadMessage.Text = "已选择上传到服务器，请选择要上传的文件。";
                        return;
                    }

                    // <lang>
                    //   <zh-CN>上传 helper 同时返回应用内虚拟路径和新建物理路径，前者入库、后者只用于失败清理。</zh-CN>
                    //   <en>The upload helper returns an application virtual path for persistence and a new physical path only for failure cleanup.</en>
                    // </lang>
                    string virtualPath;
                    if (!TrySaveUploadedFile(out virtualPath, out savedPhysicalPath))
                    {
                        return;
                    }

                    PathField.Text = virtualPath;
                }
                else
                {
                    // <lang>
                    //   <zh-CN>非上传路径只接受应用内相对地址或 http/https 浏览地址，避免写入任意本地路径。</zh-CN>
                    //   <en>The non-upload path accepts only an application-relative or http/https browse address, preventing arbitrary local paths from being stored.</en>
                    // </lang>
                    string normalizedUrl;
                    if (!PortalNavigationPolicy.TryNormalizeBrowseUrl(PathField.Text, Request, out normalizedUrl))
                    {
                        UploadMessage.Text = "请输入应用内相对地址或 http/https 浏览地址。";
                        return;
                    }

                    PathField.Text = normalizedUrl;
                }

                // <lang>
                //   <zh-CN>文档服务持久化已验证模块上下文和页面输入；数据库二进制参数保持历史兼容空值。</zh-CN>
                //   <en>The document service persists the validated module context and page input while retaining the historical empty database-binary arguments.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>删除仍依赖每次请求的编辑上下文，且只删除记录，不删除可能被历史记录共享的物理文件。</zh-CN>
            //   <en>Deletion still requires the per-request edit context and removes only the record, not a physical file that legacy records may share.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>取消不写入数据，只在上下文有效时回到已校验的门户内地址。</zh-CN>
            //   <en>Cancel performs no data write and returns to a verified in-Portal address only when the context is valid.</en>
            // </lang>
            if (!_hasValidEditContext)
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取模块和项目标识，校验模块编辑权限及已有项目归属。</zh-CN>
        ///   <en>Reads module and item identifiers and validates module-edit permission plus existing-item ownership.</en>
        /// </lang>
        /// </summary>
        private bool TryInitializeEditContext()
        {
            // <lang>
            //   <zh-CN>模块标识是编辑权限根；缺失、非法或无权时不继续读取项目。</zh-CN>
            //   <en>The module identifier roots edit authorization; missing, invalid, or unauthorized values must not continue to item reads.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>已有项目必须真实存在并属于当前模块，防止跨模块编辑。</zh-CN>
            //   <en>An existing item must exist and belong to the current module to prevent cross-module editing.</en>
            // </lang>
            _currentItem = DocumentDB.GetSingleDocument(_itemId);
            if (_currentItem == null || _currentItem.ModuleId != _moduleId)
            {
                RedirectToEditAccessDenied();
                return false;
            }

            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把已验证文档投影到编辑控件和只读元数据字段。</zh-CN>
        ///   <en>Projects a validated document into edit controls and read-only metadata fields.</en>
        /// </lang>
        /// </summary>
        private void BindDocument(IDocumentItem item)
        {
            NameField.Text = item.FileFriendlyName;
            PathField.Text = item.FileNameUrl;
            CategoryField.Text = item.Category;
            CreatedBy.Text = item.CreatedByUser;
            CreatedDate.Text = item.CreatedDate.HasValue ? item.CreatedDate.Value.ToShortDateString() : string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将页面展示与当前上传策略同步，并强制停用数据库二进制存储控件。</zh-CN>
        ///   <en>Synchronizes page presentation with the current upload policy and forces the database-binary storage control off.</en>
        /// </lang>
        /// </summary>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证上传、创建唯一物理文件并返回应用内虚拟路径；名称冲突只重试，不覆盖已有文件。</zh-CN>
        ///   <en>Validates an upload, creates a unique physical file, and returns an application virtual path; name collisions retry without overwriting existing files.</en>
        /// </lang>
        /// </summary>
        private bool TrySaveUploadedFile(out string virtualPath, out string savedPhysicalPath)
        {
            // <lang>
            //   <zh-CN>输出先初始化为空，确保验证失败或异常路径不泄漏上一次请求的路径。</zh-CN>
            //   <en>Initialize outputs to empty values so validation failure or exception paths cannot leak a prior request's paths.</en>
            // </lang>
            virtualPath = string.Empty;
            savedPhysicalPath = null;

            // <lang>
            //   <zh-CN>统一策略同时检查文件大小、扩展名和上传对象存在性。</zh-CN>
            //   <en>The shared policy checks file presence, size, and extension together.</en>
            // </lang>
            string errorMessage;
            if (!PortalDocumentPolicy.TryValidateUpload(FileUpload.PostedFile, out errorMessage))
            {
                UploadMessage.Text = errorMessage;
                return false;
            }

            // <lang>
            //   <zh-CN>物理目录由受治理的虚拟目录映射，不接受用户输入作为目录来源。</zh-CN>
            //   <en>Map the physical directory from the governed virtual directory; user input never supplies the directory.</en>
            // </lang>
            string uploadDirectory = Server.MapPath(PortalDocumentPolicy.UploadVirtualDirectory);
            Directory.CreateDirectory(uploadDirectory);

            // <lang>
            //   <zh-CN>有限重试处理极低概率名称冲突，达到上限仍抛出原异常。</zh-CN>
            //   <en>Use bounded retries for rare name collisions and rethrow the original exception after the limit.</en>
            // </lang>
            for (int attempt = 0; attempt < 5; attempt++)
            {
                // <lang>
                //   <zh-CN>存储文件名由统一策略生成，与原始上传文件名解耦。</zh-CN>
                //   <en>Generate the storage filename through the shared policy, decoupling it from the original upload name.</en>
                // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取可选正整数标识，空白值表示新建上下文。</zh-CN>
        ///   <en>Reads an optional positive identifier, with blank input representing a new-item context.</en>
        /// </lang>
        /// </summary>
        private static bool TryReadOptionalPositiveId(string rawValue, out int value)
        {
            // <lang>
            //   <zh-CN>输出默认为零，非法或缺失值不会复用旧请求状态。</zh-CN>
            //   <en>Default the output to zero so missing or invalid values cannot reuse prior request state.</en>
            // </lang>
            value = 0;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            return int.TryParse(rawValue, out value) && value > 0;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将编辑失败统一重定向到受控拒绝页并结束当前请求。</zh-CN>
        ///   <en>Redirects edit failures to the controlled denial page and completes the current request.</en>
        /// </lang>
        /// </summary>
        private void RedirectToEditAccessDenied()
        {
            Response.Redirect("~/Admin/EditAccessDenied.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断上传控件是否包含非空文件。</zh-CN>
        ///   <en>Determines whether the upload control contains a non-empty file.</en>
        /// </lang>
        /// </summary>
        private bool HasUploadedFile()
        {
            return FileUpload.PostedFile != null && FileUpload.PostedFile.ContentLength > 0;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>尽力删除本次请求创建的物理文件，不覆盖原始保存异常。</zh-CN>
        ///   <en>Best-effort deletes a physical file created by the current request without masking the original save exception.</en>
        /// </lang>
        /// </summary>
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
