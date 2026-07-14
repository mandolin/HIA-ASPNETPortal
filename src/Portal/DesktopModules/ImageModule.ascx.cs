using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：显示图片模块的受限图片资源。
    ///
    /// English: Renders the image module's constrained image resource.
    /// </summary>
    public partial class ImageModule : PortalModuleControl<ImageModule>
    {
        /// <summary>
        /// 中文：读取模块设置并仅渲染安全地址与可解析尺寸。
        ///
        /// English: Reads module settings and renders only a safe URL and parseable dimensions.
        /// </summary>
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
