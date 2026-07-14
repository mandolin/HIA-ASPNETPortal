using System;
using System.Web.UI;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：按历史兼容约定渲染受信任管理员配置的原始 HTML。
    ///
    /// English: Renders raw HTML configured by a trusted administrator under the historical compatibility convention.
    /// </summary>
    /// <remarks>
    /// 中文：这是普通模块“展示时 HTML 编码”规则的明确例外。该模块不适合普通用户输入，未来需要由细粒度的原始 HTML 权限保护。
    ///
    /// English: This is an explicit exception to the normal module rule of HTML-encoding at display time. It is not suitable for general-user input and requires a future granular Raw HTML permission.
    /// </remarks>
    public partial class HtmlModule : PortalModuleControl<HtmlModule>
    {
        /// <summary>
        /// 中文：HTML 文本数据访问服务。English: HTML-text data-access service.
        /// </summary>
        [Dependency]
        public IHtmlTextsDb HtmlTextDB { private get; set; }

        /// <summary>
        /// 中文：解码并渲染已编码存储的受信任 HTML；缺失记录不输出内容。
        ///
        /// English: Decodes and renders trusted HTML stored in encoded form; emits no content when its record is absent.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            IHtmlTextItem item = HtmlTextDB.GetHtmlText(ModuleId);
            if (item == null || string.IsNullOrEmpty(item.DesktopHtml))
            {
                return;
            }

            HtmlHolder.Controls.Add(new LiteralControl(Server.HtmlDecode(item.DesktopHtml)));
        }
    }
}
