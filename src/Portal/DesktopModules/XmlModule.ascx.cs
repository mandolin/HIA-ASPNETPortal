using System;
using System.IO;
using System.Web.UI;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：显示当前应用内已部署 XML 数据和可选 XSL/T 转换的模块控件。
    ///
    /// English: Module control that renders deployed XML data and an optional XSL/T transform from the current application.
    /// </summary>
    public partial class XmlModule : PortalModuleControl<XmlModule>
    {
        /// <summary>
        /// 中文：加载经路径策略验证且实际存在的 XML/XSL 资源；无效配置只显示中性提示，不回显原始路径。
        ///
        /// English: Loads XML/XSL resources that pass path policy and exist on disk. Invalid configuration shows a neutral notice without echoing the raw path.
        /// </summary>
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

        private bool TryGetExistingResource(string configuredPath, out string normalizedPath)
        {
            normalizedPath = string.Empty;
            if (!PortalNavigationPolicy.TryNormalizeTrustedDeploymentResourcePath(configuredPath, Context.Request, out normalizedPath))
            {
                return false;
            }

            return File.Exists(Server.MapPath(normalizedPath));
        }

        private void AddConfigurationMessage(string message)
        {
            Controls.Add(new LiteralControl("<br><span class=\"NormalRed\">" + Server.HtmlEncode(message) + "</span>"));
        }
    }
}
