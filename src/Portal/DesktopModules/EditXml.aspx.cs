using System;
using System.Collections;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：编辑 XML 模块部署资源设置的页面。
    ///
    /// English: Page for editing XML-module deployed-resource settings.
    /// </summary>
    public partial class EditXml : PortalPage<EditXml>
    {
        private int moduleId;

        /// <summary>
        /// 中文：模块设置数据访问服务。English: Module-settings data-access service.
        /// </summary>
        [Dependency]
        public IModulesDb ModulesConfig { private get; set; }

        /// <summary>
        /// 中文：模块编辑权限服务。English: Module edit-authorization service.
        /// </summary>
        [Dependency]
        public IPortalSecurity PortalSecurity { private get; set; }

        /// <summary>
        /// 中文：初始化模块编辑请求，并在首次访问时绑定现有设置。
        ///
        /// English: Initializes the module-edit request and binds existing settings on the first request.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                Hashtable settings = ModulesConfig.GetModuleSettings(moduleId);
                XmlDataSrc.Text = settings["xmlsrc"] as string;
                XslTransformSrc.Text = settings["xslsrc"] as string;
                ViewState["UrlReferrer"] = PortalNavigationPolicy.GetSafeReturnUrl(Request);
            }
        }

        /// <summary>
        /// 中文：保存受限于当前应用部署目录的 XML/XSL 路径。
        ///
        /// English: Saves XML/XSL paths constrained to the current application's deployed directory.
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
                ShowValidationMessage("XML 和 XSL/T 文件必须是当前应用目录内已部署的资源路径。");
                return;
            }

            ModulesConfig.UpdateModuleSetting(moduleId, "xmlsrc", xmlPath);
            ModulesConfig.UpdateModuleSetting(moduleId, "xslsrc", xslPath);
            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

        /// <summary>
        /// 中文：放弃编辑并返回安全地址。English: Cancels editing and returns to a safe URL.
        /// </summary>
        protected void CancelBtn_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ViewState["UrlReferrer"] as string);
        }

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

        private bool TryNormalizeOptionalDeploymentPath(string value, out string normalizedPath)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                normalizedPath = string.Empty;
                return true;
            }

            return PortalNavigationPolicy.TryNormalizeTrustedDeploymentResourcePath(value, Request, out normalizedPath);
        }

        private void ShowValidationMessage(string message)
        {
            ValidationMessage.Text = message;
            ValidationMessage.Visible = true;
        }
    }
}
