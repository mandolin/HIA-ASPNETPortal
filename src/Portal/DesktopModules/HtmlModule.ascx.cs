using System;
using System.Web.UI;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>按历史兼容约定渲染受信任管理员配置的原始 HTML。</zh-CN>
    ///   <en>Renders raw HTML configured by a trusted administrator under the historical compatibility convention.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>这是普通模块“展示时 HTML 编码”规则的明确例外。内容必须来自受信任管理员或受控部署流程；本控件在渲染时不做净化，不适合承载普通用户输入。权限体系完善后，应由独立的“原始 HTML”能力保护此入口。</zh-CN>
    ///   <en>This is an explicit exception to the normal module rule of HTML-encoding at display time. Content must come from trusted administrators or a controlled deployment flow; this control does not sanitize during rendering and is not suitable for general-user input. Once the permission system is expanded, this entry should be protected by a dedicated Raw HTML capability.</en>
    /// </lang>
    /// </remarks>
    public partial class HtmlModule : PortalModuleControl<HtmlModule>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>HTML 文本数据访问服务，用于读取当前模块保存的桌面端 HTML 片段。</zh-CN>
        ///   <en>HTML-text data-access service used to read the desktop HTML fragment stored for the current module.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IHtmlTextsDb HtmlTextDB { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解码并渲染已编码存储的受信任 HTML；缺失记录不输出内容。</zh-CN>
        ///   <en>Decodes and renders trusted HTML stored in encoded form; emits no content when its record is absent.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发页面加载的控件实例；当前逻辑不依赖该值。</zh-CN>
        ///   <en>The control instance that raised page loading; the current logic does not depend on it.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>页面加载事件参数；当前逻辑不读取额外事件状态。</zh-CN>
        ///   <en>The page-load event arguments; no additional event state is read.</en>
        /// </l>
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>历史编辑页会先编码再入库；展示时必须显式解码，才能保持旧 HTML 模块的可视效果。</zh-CN>
            //   <en>The legacy edit page encodes before persistence; display must explicitly decode to preserve the visual behavior of the old HTML module.</en>
            // </lang>
            IHtmlTextItem item = HtmlTextDB.GetHtmlText(ModuleId);
            if (item == null || string.IsNullOrEmpty(item.DesktopHtml))
            {
                // <lang>
                //   <zh-CN>缺失记录和空 HTML 都保持静默输出，避免在普通门户页面暴露配置状态。</zh-CN>
                //   <en>Missing records and empty HTML both remain silent, avoiding disclosure of configuration state on the ordinary portal page.</en>
                // </lang>
                return;
            }

            // <lang>
            //   <zh-CN>这里故意使用 LiteralControl 输出受信任 HTML；不要把普通用户提交内容接入此路径。</zh-CN>
            //   <en>This intentionally uses LiteralControl for trusted HTML output; do not route general-user submissions into this path.</en>
            // </lang>
            HtmlHolder.Controls.Add(new LiteralControl(Server.HtmlDecode(item.DesktopHtml)));
        }
    }
}
