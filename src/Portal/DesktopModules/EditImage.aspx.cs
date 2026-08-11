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
        /// <summary>
        /// <lang>
        ///   <zh-CN>当前请求中的模块实例标识，是图片设置读取、保存和编辑权限校验的共同边界。</zh-CN>
        ///   <en>Module instance identifier for the current request, forming the shared boundary for image-setting reads, saves, and edit permission checks.</en>
        /// </lang>
        /// </summary>
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
                //   <zh-CN>首次加载只在模块编辑权限通过后读取模块设置，避免未授权用户探测图片资源配置。</zh-CN>
                //   <en>Initial binding reads module settings only after edit permission succeeds, avoiding unauthorized probing of image-resource configuration.</en>
                // </lang>
                // <lang>
                //   <zh-CN>settings 是当前模块的持久化文本设置快照，只在首次加载时回填表单。</zh-CN>
                //   <en>settings is the persisted text-setting snapshot for the current module and is used only to fill the form on first load.</en>
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
            //   <zh-CN>保存前重新校验模块权限，避免只凭首次加载状态写入设置。</zh-CN>
            //   <en>Module permission is checked again before saving so settings are not written based only on first-load state.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            // <lang>
            //   <zh-CN>imageUrl、width 和 height 都保存经过策略或格式校验后的文本值，不直接持久化原始输入。</zh-CN>
            //   <en>imageUrl, width, and height persist policy- or format-validated text values rather than raw input.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>三个设置分开写入沿用旧模块设置表契约；本页不处理图片二进制或上传目录。</zh-CN>
            //   <en>The three settings are written separately to keep the legacy module-settings table contract; this page does not handle image binaries or upload directories.</en>
            // </lang>
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
            //   <zh-CN>取消不会写入图片设置，但仍重新校验请求后才使用安全回跳地址。</zh-CN>
            //   <en>Cancel does not write image settings, but still revalidates the request before using the safe return URL.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>模块标识决定设置记录和编辑权限范围；非法模块或无权限请求统一拒绝。</zh-CN>
            //   <en>The module identifier determines the setting row and edit-permission scope; invalid or unauthorized requests are denied uniformly.</en>
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
                // <lang>
                //   <zh-CN>预览只使用已归一化地址，避免把被拒绝的原始输入写回 Image 控件。</zh-CN>
                //   <en>The preview uses only the normalized address so rejected raw input is not written back to the Image control.</en>
                // </lang>
                ImagePreview.ImageUrl = normalizedUrl;
                ImagePreviewPanel.Visible = true;
                return;
            }

            ImagePreview.ImageUrl = string.Empty;
            // <lang>
            //   <zh-CN>没有可用地址时隐藏预览面板，避免界面显示破损图片或泄露输入。</zh-CN>
            //   <en>When no usable address exists, the preview panel is hidden to avoid showing a broken image or leaking input.</en>
            // </lang>
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
                // <lang>
                //   <zh-CN>图片地址允许清空；空输入保存为空字符串并关闭预览。</zh-CN>
                //   <en>The image URL may be cleared; blank input persists as an empty string and disables preview.</en>
                // </lang>
                normalizedUrl = string.Empty;
                return true;
            }

            // <lang>
            //   <zh-CN>非空地址必须通过统一浏览 URL 策略，避免设置页绕过前台图片渲染限制。</zh-CN>
            //   <en>Non-empty addresses must pass the shared browse-URL policy so the settings page cannot bypass front-end image rendering constraints.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>输出值先清空，保证任何失败分支都不会保留旧解析结果。</zh-CN>
            //   <en>The output value is cleared first so no failure path retains an old parse result.</en>
            // </lang>
            normalizedValue = string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            // <lang>
            //   <zh-CN>dimension 是管理员输入解析出的像素数，只接受非负整数以保持旧 Web Forms 尺寸语义。</zh-CN>
            //   <en>dimension is the pixel count parsed from administrator input, accepting only non-negative integers to preserve legacy Web Forms sizing semantics.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>校验提示固定来自服务器端分支，不回显被拒绝的图片地址或尺寸文本。</zh-CN>
            //   <en>The validation notice comes from fixed server-side branches and does not echo rejected image URLs or dimension text.</en>
            // </lang>
            ValidationMessage.Text = message;
            ValidationMessage.Visible = true;
        }
    }
}
