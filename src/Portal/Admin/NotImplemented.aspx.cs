using System;
using System.Web.UI.HtmlControls;
using Resources;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>显示尚未实现目标内容的样例数据链接占位页。</zh-CN>
    ///   <en>Displays a placeholder page for sample-data links whose target content is not implemented yet.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>可选标题来自请求参数，按纯文本显示；此页面不提供原始 HTML 输入或预览语义。</zh-CN>
    ///   <en>The optional title comes from a request parameter and is displayed as plain text; this page does not provide raw-HTML input or preview semantics.</en>
    /// </lang>
    /// </remarks>
    public partial class NotImplemented : PortalPage<NotImplemented>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>占位页面中由服务器控制的标题元素。</zh-CN>
        ///   <en>Server-controlled title element in the placeholder page.</en>
        /// </lang>
        /// </summary>
        protected HtmlGenericControl title;

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取可选标题并作为纯文本写入占位页。</zh-CN>
        ///   <en>Reads the optional title and writes it to the placeholder page as plain text.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l zh-CN="事件源。" en="Event source." />
        /// </param>
        /// <param name="e">
        /// <l zh-CN="事件参数。" en="Event arguments." />
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>使用 <see cref="HtmlContainerControl.InnerText"/> 而非 <c>InnerHtml</c>，避免请求参数成为反射型 HTML 或脚本注入。</zh-CN>
        ///   <en>Use <see cref="HtmlContainerControl.InnerText"/> rather than <c>InnerHtml</c> so a request parameter cannot become reflected HTML or script injection.</en>
        /// </lang>
        /// </remarks>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>只有显式提供 title 时才更新服务器控制的标题元素，空缺时保留页面默认标题。</zh-CN>
            //   <en>Update the server-controlled title only when title is supplied; otherwise keep the page default.</en>
            // </lang>
            if (Request.Params["title"] != null)
            {
                // <lang>
                //   <zh-CN>使用 InnerText 强制纯文本输出，不把请求参数当作 HTML。</zh-CN>
                //   <en>Use InnerText to force plain-text output and never treat the request parameter as HTML.</en>
                // </lang>
                title.InnerText = Request.Params["title"];
            }
            else
            {
                // <lang>
                //   <zh-CN>未提供 title 时写入资源中的默认标题。默认标题不能写在标记里：该控件会被设置 InnerText，
                //         一旦标记内含代码块，其内容就不再是字面内容，赋值会抛"内容非字面"异常。</zh-CN>
                //   <en>Write the resource default title when no title is supplied. The default cannot live in markup:
                //         this control gets InnerText assigned, and a code block in the markup makes its contents
                //         non-literal, so the assignment throws a "contents are not literal" exception.</en>
                // </lang>
                title.InnerText = lang.Admin_NotImplemented_Title;
            }
        }
    }
}
