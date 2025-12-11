using System;
using System.IO;
using System.Web.UI;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// XML 模块控制，用于显示 XML 数据并通过 XSL/T 进行转换。
    /// </summary>
    public partial class XmlModule : PortalModuleControl<XmlModule>
    {
        /// <summary>
        /// 页面加载事件处理程序用于设置 XML 文档和 XSL/T 转换文件的位置。
        /// </summary>
        /// <param name="sender">事件源对象。</param>
        /// <param name="e">事件参数。</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // 获取 XML 文件路径设置
            string xmlsrc = Settings["xmlsrc"] as string;

            // 如果 XML 文件路径设置存在且非空
            if (!string.IsNullOrEmpty(xmlsrc))
            {
                // 将路径转换为服务器上的绝对路径
                string mappedXmlPath = Server.MapPath(xmlsrc);

                // 检查文件是否存在
                if (File.Exists(mappedXmlPath))
                {
                    // 设置 XML 控件的 DocumentSource 属性
                    xml1.DocumentSource = xmlsrc;
                }
                else
                {
                    // 如果文件不存在，则添加一个提示消息
                    Controls.Add(new LiteralControl($"<br><span class=\"NormalRed\">File {xmlsrc} not found.<br>"));
                }
            }

            // 获取 XSL/T 文件路径设置
            string xslsrc = Settings["xslsrc"] as string;

            // 如果 XSL/T 文件路径设置存在且非空
            if (!string.IsNullOrEmpty(xslsrc))
            {
                // 将路径转换为服务器上的绝对路径
                string mappedXslPath = Server.MapPath(xslsrc);

                // 检查文件是否存在
                if (File.Exists(mappedXslPath))
                {
                    // 设置 XML 控件的 TransformSource 属性
                    xml1.TransformSource = xslsrc;
                }
                else
                {
                    // 如果文件不存在，则添加一个提示消息
                    Controls.Add(new LiteralControl($"<br><span class=\"NormalRed\">File {xslsrc} not found.<br>"));
                }
            }
        }
    }
}