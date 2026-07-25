using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>显示图片模块的受限图片资源。</zh-CN>
    ///   <en>Renders the image module's constrained image resource.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该控件只消费模块设置中的图片地址和尺寸，不负责处理图片上传、外链白名单或二进制资源存储。地址必须先通过站内导航策略归一化，尺寸只能在可解析且非负时写入控件。</zh-CN>
    ///   <en>This control only consumes the image URL and dimensions stored in module settings. It does not handle uploads, external-link allow lists, or binary storage. The URL must pass portal navigation normalization, and dimensions are applied only when they parse to non-negative values.</en>
    /// </lang>
    /// </remarks>
    public partial class ImageModule : PortalModuleControl<ImageModule>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>读取模块设置并仅渲染安全地址与可解析尺寸。</zh-CN>
        ///   <en>Reads module settings and renders only a safe URL and parseable dimensions.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <lang>
        ///   <zh-CN>触发页面加载事件的控件实例；当前逻辑不依赖该值。</zh-CN>
        ///   <en>The control instance that raised the page-load event; the current logic does not depend on it.</en>
        /// </lang>
        /// </param>
        /// <param name="e">
        /// <lang>
        ///   <zh-CN>页面加载事件参数；当前逻辑不读取额外事件状态。</zh-CN>
        ///   <en>The page-load event arguments; no additional event state is read.</en>
        /// </lang>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>如果图片地址不符合站内浏览策略，控件会隐藏图片而不是回显原始配置值，降低错误配置泄露路径或外链信息的风险。</zh-CN>
        ///   <en>If the configured image URL fails the portal browse-url policy, the image is hidden instead of echoing the raw setting value, reducing the chance of leaking paths or external-link details.</en>
        /// </lang>
        /// </remarks>
        protected void Page_Load(object sender, EventArgs e)
        {
            string imageUrl;
            if (!PortalNavigationPolicy.TryNormalizeBrowseUrl(Settings["src"] as string, Context.Request, out imageUrl))
            {
                Image1.Visible = false;
                return;
            }

            Image1.ImageUrl = imageUrl;
            ApplyDimension(Settings["width"] as string, true);
            ApplyDimension(Settings["height"] as string, false);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把模块设置中的宽度或高度应用到图片控件。</zh-CN>
        ///   <en>Applies a configured width or height value to the image control.</en>
        /// </lang>
        /// </summary>
        /// <param name="configuredValue">
        /// <lang>
        ///   <zh-CN>来自模块设置的原始尺寸文本。</zh-CN>
        ///   <en>The raw dimension text read from module settings.</en>
        /// </lang>
        /// </param>
        /// <param name="isWidth">
        /// <lang>
        ///   <zh-CN>`true` 表示写入宽度，`false` 表示写入高度。</zh-CN>
        ///   <en>`true` applies the value as width; `false` applies it as height.</en>
        /// </lang>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>无法解析或小于 0 的值会被静默忽略，保留 Web Forms 图片控件默认尺寸，避免旧配置造成异常或布局破坏。</zh-CN>
        ///   <en>Values that do not parse or are below zero are ignored, preserving the Web Forms image control defaults and preventing legacy settings from causing exceptions or layout damage.</en>
        /// </lang>
        /// </remarks>
        private void ApplyDimension(string configuredValue, bool isWidth)
        {
            int dimension;
            if (!int.TryParse(configuredValue, out dimension) || dimension < 0)
            {
                return;
            }

            if (isWidth)
            {
                Image1.Width = dimension;
            }
            else
            {
                Image1.Height = dimension;
            }
        }
    }
}
