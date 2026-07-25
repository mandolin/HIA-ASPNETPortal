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
        protected void UpdateBtn_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

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
        protected void CancelBtn_Click(object sender, EventArgs e)
        {
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
                normalizedPath = string.Empty;
                return true;
            }

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
            ValidationMessage.Text = message;
            ValidationMessage.Visible = true;
        }
    }
}
