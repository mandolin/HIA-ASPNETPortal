using System;
using System.Collections;
using System.Globalization;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>编辑图片模块设置的页面。</zh-CN>
    ///   <en>Page for editing image-module settings.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该页面只维护图片模块的文本设置，不处理二进制上传。图片地址沿用站内或 HTTP(S) 浏览地址策略，尺寸字段保持旧模块的字符串设置形态。</zh-CN>
    ///   <en>This page only maintains text settings for the image module and does not handle binary uploads. Image URLs reuse the in-app or HTTP(S) browse-URL policy, while dimension fields keep the legacy module's string-setting shape.</en>
    /// </lang>
    /// </remarks>
    public partial class EditImage : PortalPage<EditImage>
    {
        private int moduleId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>模块设置数据访问服务。</zh-CN>
        ///   <en>Module-settings data-access service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IModulesDb ModulesConfig { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>模块编辑权限服务。</zh-CN>
        ///   <en>Module edit-authorization service.</en>
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
                //   <zh-CN>首次加载只在模块编辑权限通过后读取模块设置，避免未授权用户探测图片资源配置。</zh-CN>
                //   <en>Initial binding reads module settings only after edit permission succeeds, avoiding unauthorized probing of image-resource configuration.</en>
                // </lang>
                Hashtable settings = ModulesConfig.GetModuleSettings(moduleId);
                Src.Text = settings["src"] as string;
                Width.Text = settings["width"] as string;
                Height.Text = settings["height"] as string;
                ApplyImagePreview(Src.Text);

                // <lang>
                //   <zh-CN>回跳地址只保存经策略清洗后的站内地址，供保存和取消共用。</zh-CN>
                //   <en>The return URL stores only a policy-cleaned in-app address shared by save and cancel actions.</en>
                // </lang>
                ViewState["UrlReferrer"] = PortalNavigationPolicy.GetSafeReturnUrl(Request);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>保存经验证的图片地址和尺寸设置。</zh-CN>
        ///   <en>Saves validated image-address and dimension settings.</en>
        /// </lang>
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
                // <lang>
                //   <zh-CN>图片地址失败时只展示低敏提示，并清空预览，避免把不可接受地址继续渲染到页面。</zh-CN>
                //   <en>When image URL validation fails, show only a low-sensitivity message and clear the preview so rejected addresses are not rendered back to the page.</en>
                // </lang>
                ShowValidationMessage("图片地址只能使用站内地址或 HTTP(S) 地址。");
                ApplyImagePreview(string.Empty);
                return;
            }

            if (!TryNormalizeDimension(Width.Text, out width) || !TryNormalizeDimension(Height.Text, out height))
            {
                // <lang>
                //   <zh-CN>尺寸字段保持旧模块的“空值表示不限制”语义，只接受非负整数以避免样式注入。</zh-CN>
                //   <en>Dimension fields keep the legacy "blank means unrestricted" semantics and accept only non-negative integers to avoid style injection.</en>
                // </lang>
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
        ///   <zh-CN>读取模块标识并核验模块编辑权限。</zh-CN>
        ///   <en>Reads the module id and verifies module edit permission.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>请求可继续处理时返回 <c>true</c>；非法参数或权限失败时已跳转。</zh-CN>
        ///   <en><c>true</c> when the request may continue; invalid parameters or authorization failures have already redirected.</en>
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
        ///   <zh-CN>根据当前地址策略刷新图片预览。</zh-CN>
        ///   <en>Refreshes the image preview using the current URL policy.</en>
        /// </lang>
        /// </summary>
        /// <param name="rawSource">
        /// <l>
        ///   <zh-CN>管理员输入或数据库保存的图片地址。</zh-CN>
        ///   <en>Image URL entered by the administrator or stored in the database.</en>
        /// </l>
        /// </param>
        private void ApplyImagePreview(string rawSource)
        {
            // <lang>
            //   <zh-CN>预览只复用已允许的普通浏览地址规则，不为图片模块额外打开脚本、文件或任意物理路径能力。</zh-CN>
            //   <en>The preview reuses the allowed browse-URL rule and does not open script, file, or arbitrary physical-path capabilities for image modules.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>归一化可为空的图片浏览地址。</zh-CN>
        ///   <en>Normalizes an optional image browse URL.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>管理员输入的地址文本。</zh-CN>
        ///   <en>URL text entered by the administrator.</en>
        /// </l>
        /// </param>
        /// <param name="normalizedUrl">
        /// <l>
        ///   <zh-CN>归一化后的站内或 HTTP(S) 地址。</zh-CN>
        ///   <en>Normalized in-app or HTTP(S) URL.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>空值或符合当前导航策略时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the value is empty or accepted by the current navigation policy.</en>
        /// </l>
        /// </returns>
        private bool TryNormalizeOptionalBrowseUrl(string value, out string normalizedUrl)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                normalizedUrl = string.Empty;
                return true;
            }

            return PortalNavigationPolicy.TryNormalizeBrowseUrl(value, Request, out normalizedUrl);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>归一化图片宽高设置。</zh-CN>
        ///   <en>Normalizes an image width or height setting.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>管理员输入的尺寸文本。</zh-CN>
        ///   <en>Dimension text entered by the administrator.</en>
        /// </l>
        /// </param>
        /// <param name="normalizedValue">
        /// <l>
        ///   <zh-CN>用于保存的 invariant-culture 数字文本；空值表示不限制。</zh-CN>
        ///   <en>Invariant-culture numeric text to persist; empty means unrestricted.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>空值或非负整数时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the value is empty or a non-negative integer.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>显示页面级低敏校验提示。</zh-CN>
        ///   <en>Displays a low-sensitivity page-level validation message.</en>
        /// </lang>
        /// </summary>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>提示文本。</zh-CN>
        ///   <en>Message text.</en>
        /// </l>
        /// </param>
        private void ShowValidationMessage(string message)
        {
            ValidationMessage.Text = message;
            ValidationMessage.Visible = true;
        }
    }
}
