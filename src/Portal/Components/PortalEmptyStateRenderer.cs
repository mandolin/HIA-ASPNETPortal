using System.Globalization;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>把"列表零条"这一状态渲染为表格内的单行提示。</zh-CN>
    ///   <en>Renders the "list has zero rows" state as a single in-table hint row.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P74.3 起供业务前后台列表复用。设计约束：① 空态必须是**表格内的一行**（表头保留、视觉连续），故输出 `<c>tr</c>/<c>td</c>` 而非独立区块；② 样式类 `portal-empty-state` 作用于 <c>td</c> **内部的 <c>div</c>** —— 直接放在 <c>td</c> 上会与表格既有的 `<c>.xxx td { text-align: left }</c>` 同属性竞争，而二者特异性相同或后者更高，居中会失效（真实渲染实测）；③ 调用方负责判断"空"与"筛选无结果"的**文案差异**，以及"读取失败"**不得**退化为空态。有数据时返回空字符串，使调用方无需分支。</zh-CN>
    ///   <en>Reused by front-end and Admin business lists starting with P74.3. Design constraints: (1) the empty state must be a **row inside the table** so the header stays and the list reads continuously, hence `<c>tr</c>/<c>td</c>` output rather than a detached block; (2) the `portal-empty-state` class applies to a <c>div</c> **inside** the <c>td</c> — putting it on the <c>td</c> itself competes with existing `<c>.xxx td { text-align: left }</c> rules at equal or higher specificity, which breaks centering (measured in a real render); (3) the caller owns the **wording difference** between "no data" and "no matches", and must never degrade a **read failure** into an empty state. When rows exist the method returns an empty string so callers need no branch.</en>
    /// </lang>
    /// </remarks>
    public static class PortalEmptyStateRenderer
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>渲染零条记录时的表格内提示行；有数据时返回空字符串。</zh-CN>
        ///   <en>Renders the in-table hint row for a zero-row result; returns an empty string when rows exist.</en>
        /// </lang>
        /// </summary>
        /// <param name="rowCount">
        /// <l>
        ///   <zh-CN>当前列表已渲染的数据行数；大于 0 时不输出任何内容。</zh-CN>
        ///   <en>The number of data rows already rendered; when greater than zero nothing is emitted.</en>
        /// </l>
        /// </param>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>提示文案，由调用方按"无数据"或"筛选无结果"选定；输出前按 HTML 规则编码，空白值退化为空文本。</zh-CN>
        ///   <en>The hint wording chosen by the caller for "no data" or "no matches"; it is HTML-encoded before output and blank values degrade to empty text.</en>
        /// </l>
        /// </param>
        /// <param name="columnCount">
        /// <l>
        ///   <zh-CN>当前表格的列数，用作 <c>colspan</c>；小于 1 时按 1 处理，避免产生非法单元格。</zh-CN>
        ///   <en>The current table's column count used as <c>colspan</c>; values below 1 are treated as 1 so no invalid cell is produced.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>内嵌空态提示的 <c>tr</c> 标记；有数据时为空字符串。</zh-CN>
        ///   <en>The <c>tr</c> markup carrying the empty-state hint, or an empty string when rows exist.</en>
        /// </l>
        /// </returns>
        public static string Render(int rowCount, string message, int columnCount)
        {
            // <lang>
            //   <zh-CN>有数据时不输出空态：调用方无需在外层写条件分支，模板保持单一写法。</zh-CN>
            //   <en>Emit nothing when rows exist: the caller needs no outer conditional and the template keeps one shape.</en>
            // </lang>
            if (rowCount > 0)
            {
                return string.Empty;
            }

            // <lang>
            //   <zh-CN>列数下限保护：colspan 小于 1 会产生非法单元格并可能破坏整张表的解析。</zh-CN>
            //   <en>Floor the column count: a colspan below 1 produces an invalid cell and can break parsing of the whole table.</en>
            // </lang>
            int safeColumnCount = columnCount < 1 ? 1 : columnCount;

            // <lang>
            //   <zh-CN>文案一律编码后输出，避免资源文件内容被当作标记解释。</zh-CN>
            //   <en>Always encode the wording before output so resource content is never interpreted as markup.</en>
            // </lang>
            string encodedMessage = string.IsNullOrWhiteSpace(message)
                ? string.Empty
                : HttpUtility.HtmlEncode(message.Trim());

            return "<tr><td colspan=\"" + safeColumnCount.ToString(CultureInfo.InvariantCulture) +
                "\"><div class=\"portal-empty-state\">" + encodedMessage + "</div></td></tr>";
        }
    }
}
