using System;
using System.Collections;
using System.Globalization;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：编辑图片模块设置的页面。
    ///
    /// English: Page for editing image-module settings.
    /// </summary>
    public partial class EditImage : PortalPage<EditImage>
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
                Src.Text = settings["src"] as string;
                Width.Text = settings["width"] as string;
                Height.Text = settings["height"] as string;
                ApplyImagePreview(Src.Text);
                ViewState["UrlReferrer"] = PortalNavigationPolicy.GetSafeReturnUrl(Request);
            }
        }

        /// <summary>
        /// 中文：保存经验证的图片地址和尺寸设置。English: Saves validated image-address and dimension settings.
        /// </summary>
        protected void UpdateBtn_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            string imageUrl;
            string width;
            string height;
            if (!TryNormalizeOptionalBrowseUrl(Src.Text, out imageUrl))
            {
                ShowValidationMessage("图片地址只能使用站内地址或 HTTP(S) 地址。");
                ApplyImagePreview(string.Empty);
                return;
            }

            if (!TryNormalizeDimension(Width.Text, out width) || !TryNormalizeDimension(Height.Text, out height))
            {
                ShowValidationMessage("图片宽度和高度必须是非负整数，留空表示不限制该尺寸。");
                ApplyImagePreview(imageUrl);
                return;
            }

            ModulesConfig.UpdateModuleSetting(moduleId, "src", imageUrl);
            ModulesConfig.UpdateModuleSetting(moduleId, "height", height);
            ModulesConfig.UpdateModuleSetting(moduleId, "width", width);
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

        private void ApplyImagePreview(string rawSource)
        {
            // 中文：预览只复用已允许的普通浏览地址规则，不为图片模块额外打开脚本、文件或任意物理路径能力。
            // English: The preview reuses the allowed browse-URL rule and does not open script, file, or arbitrary physical-path capabilities for image modules.
            string normalizedUrl;
            if (TryNormalizeOptionalBrowseUrl(rawSource, out normalizedUrl) && !string.IsNullOrWhiteSpace(normalizedUrl))
            {
                ImagePreview.ImageUrl = normalizedUrl;
                ImagePreviewPanel.Visible = true;
                return;
            }

            ImagePreview.ImageUrl = string.Empty;
            ImagePreviewPanel.Visible = false;
        }

        private bool TryNormalizeOptionalBrowseUrl(string value, out string normalizedUrl)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                normalizedUrl = string.Empty;
                return true;
            }

            return PortalNavigationPolicy.TryNormalizeBrowseUrl(value, Request, out normalizedUrl);
        }

        private static bool TryNormalizeDimension(string value, out string normalizedValue)
        {
            normalizedValue = string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            int dimension;
            if (!int.TryParse(value, out dimension) || dimension < 0)
            {
                return false;
            }

            normalizedValue = dimension.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private void ShowValidationMessage(string message)
        {
            ValidationMessage.Text = message;
            ValidationMessage.Visible = true;
        }
    }
}
