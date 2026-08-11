using System;
using System.Collections;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>编辑 XML 模块部署资源设置的页面。</zh-CN>
    ///   <en>Page for editing XML-module deployed-resource settings.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该页只保存当前应用内已部署的 XML/XSL 虚拟路径，不接收上传文件、远程 URL、任意物理路径或脚本加载配置。保存后统一通过安全返回策略回到来源页，避免开放重定向。</zh-CN>
    ///   <en>This page stores only deployed XML/XSL virtual paths inside the current application; it does not accept uploaded files, remote URLs, arbitrary physical paths, or script-loading settings. After saving, it always returns through the safe-return policy to avoid open redirects.</en>
    /// </lang>
    /// </remarks>
    public partial class EditXml : PortalPage<EditXml>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>当前请求中的模块实例标识，是 XML/XSL 设置读取、保存和编辑权限校验的共同边界。</zh-CN>
        ///   <en>Module instance identifier for the current request, forming the shared boundary for XML/XSL setting reads, saves, and edit permission checks.</en>
        /// </lang>
        /// </summary>
        private int moduleId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取模块设置数据访问服务。</zh-CN>
        ///   <en>Gets the module-settings data-access service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IModulesDb ModulesConfig { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取模块编辑权限服务。</zh-CN>
        ///   <en>Gets the module edit-authorization service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IPortalSecurity PortalSecurity { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化模块编辑请求，并在首次访问时绑定现有设置。</zh-CN>
        ///   <en>Initializes the module-edit request and binds existing settings on the first request.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发页面加载的 Web Forms 事件源。</zh-CN>
        ///   <en>The Web Forms event source that triggered page loading.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>页面加载事件参数；当前实现不读取其内容。</zh-CN>
        ///   <en>The page-load event arguments; the current implementation does not read them.</en>
        /// </l>
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                // <lang>
                //   <zh-CN>首次进入时只读取当前模块既有设置；权限校验已经在 TryInitializeRequest 中完成。</zh-CN>
                //   <en>On the first request, read only the current module's existing settings; authorization has already completed in TryInitializeRequest.</en>
                // </lang>
                // <lang>
                //   <zh-CN>settings 是当前模块的 XML/XSL 文本设置快照，只在首次加载时回填表单。</zh-CN>
                //   <en>settings is the XML/XSL text-setting snapshot for the current module and is used only to fill the form on first load.</en>
                // </lang>
                Hashtable settings = ModulesConfig.GetModuleSettings(moduleId);
                XmlDataSrc.Text = settings["xmlsrc"] as string;
                XslTransformSrc.Text = settings["xslsrc"] as string;
                ViewState["UrlReferrer"] = PortalNavigationPolicy.GetSafeReturnUrl(Request);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>保存受限于当前应用部署目录的 XML/XSL 路径。</zh-CN>
        ///   <en>Saves XML/XSL paths constrained to the current application's deployed directory.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发保存命令的提交控件。</zh-CN>
        ///   <en>The submit control that raised the save command.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>保存命令事件参数；当前实现不读取额外状态。</zh-CN>
        ///   <en>The save-command event arguments; no additional state is read by the current implementation.</en>
        /// </l>
        /// </param>
        protected void UpdateBtn_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>保存前重新核验模块编辑权限，避免凭首次加载状态写入模块设置。</zh-CN>
            //   <en>Module edit permission is rechecked before saving so module settings are not written based on first-load state.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            // <lang>
            //   <zh-CN>xmlPath 与 xslPath 保存通过部署资源策略后的应用内虚拟路径；空输入表示清空对应设置。</zh-CN>
            //   <en>xmlPath and xslPath hold in-application virtual paths after deployment-resource policy validation; blank input clears the corresponding setting.</en>
            // </lang>
            string xmlPath;
            string xslPath;
            if (!TryNormalizeOptionalDeploymentPath(XmlDataSrc.Text, out xmlPath) ||
                !TryNormalizeOptionalDeploymentPath(XslTransformSrc.Text, out xslPath))
            {
                // <lang>
                //   <zh-CN>路径策略会拒绝应用外路径、远程地址和不存在的资源；页面只返回低敏提示，不暴露物理路径。</zh-CN>
                //   <en>The path policy rejects outside-application paths, remote addresses, and missing resources; the page returns only low-sensitivity guidance and does not expose physical paths.</en>
                // </lang>
                ShowValidationMessage("XML 和 XSL/T 文件必须是当前应用目录内已部署的资源路径。");
                return;
            }

            // <lang>
            //   <zh-CN>只在路径已被标准化后写入模块设置，避免之后的渲染阶段再处理不可信输入。</zh-CN>
            //   <en>Write module settings only after paths are normalized so later rendering stages do not process untrusted input.</en>
            // </lang>
            ModulesConfig.UpdateModuleSetting(moduleId, "xmlsrc", xmlPath);
            ModulesConfig.UpdateModuleSetting(moduleId, "xslsrc", xslPath);
            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>放弃编辑并返回安全地址。</zh-CN>
        ///   <en>Cancels editing and returns to a safe URL.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发取消命令的提交控件。</zh-CN>
        ///   <en>The submit control that raised the cancel command.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>取消命令事件参数；当前实现不读取额外状态。</zh-CN>
        ///   <en>The cancel-command event arguments; no additional state is read by the current implementation.</en>
        /// </l>
        /// </param>
        protected void CancelBtn_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>取消不写入 XML/XSL 设置，但仍确认请求合法后才使用安全回跳地址。</zh-CN>
            //   <en>Cancel does not write XML/XSL settings, but still confirms the request is valid before using the safe return URL.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取模块标识并确认当前用户具备该模块编辑权限。</zh-CN>
        ///   <en>Reads the module identifier and confirms that the current user can edit the module.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>请求有效且授权通过时返回 <c>true</c>；否则跳转到编辑拒绝页并返回 <c>false</c>。</zh-CN>
        ///   <en>Returns <c>true</c> when the request is valid and authorized; otherwise redirects to the edit-denied page and returns <c>false</c>.</en>
        /// </l>
        /// </returns>
        private bool TryInitializeRequest()
        {
            // <lang>
            //   <zh-CN>模块标识决定设置记录和权限范围；非法或无权限请求统一进入编辑拒绝页。</zh-CN>
            //   <en>The module identifier determines the setting row and permission scope; invalid or unauthorized requests go uniformly to the edit-denied page.</en>
            // </lang>
            if (!PortalNavigationPolicy.TryReadPositiveInt32(Request.Params["Mid"], out moduleId) ||
                !PortalSecurity.HasEditPermissions(moduleId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把可选 XML/XSL 输入标准化为受信任部署资源路径。</zh-CN>
        ///   <en>Normalizes optional XML/XSL input to a trusted deployed-resource path.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>管理员输入的虚拟路径；空值表示清空设置。</zh-CN>
        ///   <en>The administrator-entered virtual path; an empty value clears the setting.</en>
        /// </l>
        /// </param>
        /// <param name="normalizedPath">
        /// <l>
        ///   <zh-CN>标准化后的应用内虚拟路径。</zh-CN>
        ///   <en>The normalized in-application virtual path.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>路径为空或通过可信部署资源校验时返回 <c>true</c>。</zh-CN>
        ///   <en>Returns <c>true</c> when the path is empty or passes trusted deployed-resource validation.</en>
        /// </l>
        /// </returns>
        private bool TryNormalizeOptionalDeploymentPath(string value, out string normalizedPath)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // <lang>
                //   <zh-CN>XML 或 XSL 路径允许清空；空输入保存为空字符串且不触发文件探测。</zh-CN>
                //   <en>XML or XSL paths may be cleared; blank input persists as an empty string and does not trigger file probing.</en>
                // </lang>
                normalizedPath = string.Empty;
                return true;
            }

            // <lang>
            //   <zh-CN>非空路径必须通过可信部署资源策略，拒绝应用外路径、远程 URL 和不受信资源。</zh-CN>
            //   <en>Non-empty paths must pass the trusted deployment-resource policy, rejecting outside-application paths, remote URLs, and untrusted resources.</en>
            // </lang>
            return PortalNavigationPolicy.TryNormalizeTrustedDeploymentResourcePath(value, Request, out normalizedPath);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>显示低敏校验提示。</zh-CN>
        ///   <en>Shows a low-sensitivity validation message.</en>
        /// </lang>
        /// </summary>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>面向管理员显示的提示文本，不应包含物理路径、异常堆栈或连接信息。</zh-CN>
        ///   <en>The administrator-facing message text, which should not contain physical paths, exception stacks, or connection details.</en>
        /// </l>
        /// </param>
        private void ShowValidationMessage(string message)
        {
            // <lang>
            //   <zh-CN>提示文本来自固定服务器分支，不包含物理路径、异常堆栈或原始输入。</zh-CN>
            //   <en>The message text comes from fixed server-side branches and contains no physical paths, exception stacks, or raw input.</en>
            // </lang>
            ValidationMessage.Text = message;
            ValidationMessage.Visible = true;
        }
    }
}
