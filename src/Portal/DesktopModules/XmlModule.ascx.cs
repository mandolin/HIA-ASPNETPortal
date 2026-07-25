using System;
using System.IO;
using System.Web.UI;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>显示当前应用内已部署 XML 数据和可选 XSL/T 转换的模块控件。</zh-CN>
    ///   <en>Module control that renders deployed XML data and an optional XSL/T transform from the current application.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该控件只允许引用受信任部署目录中的站内资源，不自动加载外部 XML、远程样式表或脚本。路径校验和物理文件存在性检查都在设置值进入 `Xml` 控件前完成。</zh-CN>
    ///   <en>This control only references in-site resources from trusted deployment locations. It does not auto-load external XML, remote stylesheets, or scripts. Path validation and physical-file existence checks complete before settings are assigned to the Web Forms `Xml` control.</en>
    /// </lang>
    /// </remarks>
    public partial class XmlModule : PortalModuleControl<XmlModule>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>加载经路径策略验证且实际存在的 XML/XSL 资源；无效配置只显示中性提示，不回显原始路径。</zh-CN>
        ///   <en>Loads XML/XSL resources that pass path policy and exist on disk. Invalid configuration shows a neutral notice without echoing the raw path.</en>
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
        ///   <zh-CN>XML 与 XSL/T 分别校验，便于只配置 XML 数据或在转换资源缺失时保留低敏错误提示。原始配置路径不写入页面输出。</zh-CN>
        ///   <en>XML and XSL/T paths are validated independently, allowing XML-only configuration and neutral feedback when a transform is unavailable. Raw configured paths are not written to page output.</en>
        /// </lang>
        /// </remarks>
        protected void Page_Load(object sender, EventArgs e)
        {
            string xmlPath;
            if (TryGetExistingResource(Settings["xmlsrc"] as string, out xmlPath))
            {
                xml1.DocumentSource = xmlPath;
            }
            else if (!string.IsNullOrWhiteSpace(Settings["xmlsrc"] as string))
            {
                AddConfigurationMessage("XML 数据文件当前不可用。");
            }

            string xslPath;
            if (TryGetExistingResource(Settings["xslsrc"] as string, out xslPath))
            {
                xml1.TransformSource = xslPath;
            }
            else if (!string.IsNullOrWhiteSpace(Settings["xslsrc"] as string))
            {
                AddConfigurationMessage("XSL/T 转换文件当前不可用。");
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>校验模块设置中的资源路径，并确认对应物理文件存在。</zh-CN>
        ///   <en>Validates a configured resource path and confirms that the mapped physical file exists.</en>
        /// </lang>
        /// </summary>
        /// <param name="configuredPath">
        /// <lang>
        ///   <zh-CN>模块设置中的原始 XML 或 XSL/T 路径。</zh-CN>
        ///   <en>The raw XML or XSL/T path from module settings.</en>
        /// </lang>
        /// </param>
        /// <param name="normalizedPath">
        /// <lang>
        ///   <zh-CN>通过受信任部署资源策略后的应用相对路径；失败时为空字符串。</zh-CN>
        ///   <en>The application-relative path after trusted-deployment resource normalization; empty when validation fails.</en>
        /// </lang>
        /// </param>
        /// <returns>
        /// <lang>
        ///   <zh-CN>路径满足策略且物理文件存在时返回 `true`。</zh-CN>
        ///   <en>Returns `true` when the path passes policy checks and the physical file exists.</en>
        /// </lang>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该方法集中处理路径策略和 `Server.MapPath` 文件存在性检查，调用方只根据布尔结果决定是否显示中性提示。</zh-CN>
        ///   <en>This method centralizes path-policy checks and `Server.MapPath` file-existence checks; callers only use the Boolean result to decide whether to show a neutral notice.</en>
        /// </lang>
        /// </remarks>
        private bool TryGetExistingResource(string configuredPath, out string normalizedPath)
        {
            normalizedPath = string.Empty;
            if (!PortalNavigationPolicy.TryNormalizeTrustedDeploymentResourcePath(configuredPath, Context.Request, out normalizedPath))
            {
                return false;
            }

            return File.Exists(Server.MapPath(normalizedPath));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>向控件树追加低敏配置提示。</zh-CN>
        ///   <en>Adds a low-sensitivity configuration notice to the control tree.</en>
        /// </lang>
        /// </summary>
        /// <param name="message">
        /// <lang>
        ///   <zh-CN>面向管理员或内容维护者的提示文本。</zh-CN>
        ///   <en>The notice text shown to administrators or content maintainers.</en>
        /// </lang>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>提示文本仍经过 HTML 编码；调用方不得传入原始物理路径、连接串或异常堆栈。</zh-CN>
        ///   <en>The notice text is HTML-encoded; callers must not pass raw physical paths, connection strings, or exception stacks.</en>
        /// </lang>
        /// </remarks>
        private void AddConfigurationMessage(string message)
        {
            Controls.Add(new LiteralControl("<br><span class=\"NormalRed\">" + Server.HtmlEncode(message) + "</span>"));
        }
    }
}
