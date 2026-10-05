using System;
using System.Globalization;
using System.Web;
using Resources;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>只读结构化诊断日志查询页面。</zh-CN>
    ///   <en>Read-only structured diagnostics log query page.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>页面只读取已经由诊断查询服务投影的低敏事件，不创建日志、不执行任意文件读取，并通过固定页大小和日期范围限制查询成本。</zh-CN>
    ///   <en>The page reads only low-sensitivity events projected by the diagnostics query service; it does not create logs or read arbitrary files, and bounds query cost with a fixed page size and date range.</en>
    /// </lang>
    /// </remarks>
    public partial class DiagnosticsLogs : PortalPage<DiagnosticsLogs>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>每页最多展示的诊断事件数；这是页面读取上限，不替代服务端扫描上限。</zh-CN>
        ///   <en>Maximum diagnostics events displayed per page; this is a page-read cap and does not replace the service-side scan limit.</en>
        /// </lang>
        /// </summary>
        private const int PageSize = 50;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化管理员诊断日志查询页面。</zh-CN>
        ///   <en>Initializes the administrator diagnostics-log query page.</en>
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
        ///   <zh-CN>页面加载事件参数。</zh-CN>
        ///   <en>Page-load event arguments.</en>
        /// </l>
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>每次生命周期先复核诊断查看权限，拒绝时不读取查询服务或修改结果控件。</zh-CN>
            //   <en>Recheck diagnostics-view permission on every lifecycle entry so rejection performs no query-service read or result-control mutation.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.OpsDiagnosticsView))
            {
                return;
            }

            // <lang>
            //   <zh-CN>仅首次请求填充最近七个 UTC 日期并查询，回发保留筛选器当前值。</zh-CN>
            //   <en>Populate the recent seven-day UTC range and query only on the initial request, preserving filter values on postback.</en>
            // </lang>
            if (!Page.IsPostBack)
            {
                DateTime todayUtc = DateTime.UtcNow.Date;
                StartDateTextBox.Text = todayUtc.AddDays(-6).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                EndDateTextBox.Text = todayUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                BindEntries();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>应用新的诊断筛选条件并回到第一页。</zh-CN>
        ///   <en>Applies new diagnostics filters and returns to the first page.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发搜索的 Web Forms 事件源。</zh-CN>
        ///   <en>The Web Forms event source that triggered the search.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>搜索事件参数。</zh-CN>
        ///   <en>Search event arguments.</en>
        /// </l>
        /// </param>
        protected void SearchButton_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>新筛选从第一页开始；BindEntries 仍负责日期和查询服务边界校验。</zh-CN>
            //   <en>New filters start at page one; BindEntries remains responsible for date and query-service boundary validation.</en>
            // </lang>
            CurrentPage = 0;
            BindEntries();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取上一页诊断事件。</zh-CN>
        ///   <en>Reads the previous diagnostics-events page.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发分页的 Web Forms 事件源。</zh-CN>
        ///   <en>The Web Forms event source that triggered paging.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>分页事件参数。</zh-CN>
        ///   <en>Paging event arguments.</en>
        /// </l>
        /// </param>
        protected void PreviousButton_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>页面索引在减法后仍保持非负，避免伪造回发值产生负页查询。</zh-CN>
            //   <en>Keep the page index non-negative after decrementing so forged postback values cannot produce a negative-page query.</en>
            // </lang>
            CurrentPage = Math.Max(0, CurrentPage - 1);
            BindEntries();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取下一页诊断事件。</zh-CN>
        ///   <en>Reads the next diagnostics-events page.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发分页的 Web Forms 事件源。</zh-CN>
        ///   <en>The Web Forms event source that triggered paging.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>分页事件参数。</zh-CN>
        ///   <en>Paging event arguments.</en>
        /// </l>
        /// </param>
        protected void NextButton_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>下一页动作不直接信任按钮可见性，实际结果是否存在由服务返回的 HasMore 决定。</zh-CN>
            //   <en>The next-page action does not trust button visibility; actual availability is determined by the service result HasMore flag.</en>
            // </lang>
            CurrentPage++;
            BindEntries();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>构建只携带事件编号的诊断详情链接。</zh-CN>
        ///   <en>Builds a diagnostics-detail link carrying only the event identifier.</en>
        /// </lang>
        /// </summary>
        /// <param name="eventId">
        /// <l>
        ///   <zh-CN>结构化诊断事件编号；空值按 URL 编码规则处理。</zh-CN>
        ///   <en>Structured diagnostics event identifier; null is handled by URL-encoding rules.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>固定站内详情页 URL。</zh-CN>
        ///   <en>A fixed local detail-page URL.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>仅使用固定页面路径和 URL 编码后的 id，不把日志字段或任意路径拼入导航地址。</zh-CN>
        ///   <en>Uses only a fixed page path and an URL-encoded id; log fields and arbitrary paths are never composed into the navigation URL.</en>
        /// </lang>
        /// </remarks>
        public string GetDetailUrl(object eventId)
        {
            return ResolveUrl("~/Admin/DiagnosticLogDetail.aspx?id=" + HttpUtility.UrlEncode(Convert.ToString(eventId)));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>保存当前诊断查询页索引。</zh-CN>
        ///   <en>Stores the current diagnostics-query page index.</en>
        /// </lang>
        /// </summary>
        private int CurrentPage
        {
            get
            {
                object value = ViewState["DiagnosticsLogs.CurrentPage"];
                return value is int ? (int)value : 0;
            }
            // <lang>
            //   <zh-CN>ViewState 只承载非负索引，避免回发状态将查询游标写成负数。</zh-CN>
            //   <en>ViewState carries only a non-negative index so postback state cannot write a negative query cursor.</en>
            // </lang>
            set { ViewState["DiagnosticsLogs.CurrentPage"] = Math.Max(0, value); }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按当前日期和文本筛选条件查询并绑定诊断事件。</zh-CN>
        ///   <en>Queries and binds diagnostics events using the current date and text filters.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>日期解析失败时清空旧结果；成功后将页码、固定页大小和筛选值交给只读查询服务，并只显示服务返回的截断提示。</zh-CN>
        ///   <en>Invalid dates clear stale results; valid dates pass the page, fixed page size, and filter values to the read-only query service and display only its truncation notice.</en>
        /// </lang>
        /// </remarks>
        private void BindEntries()
        {
            DateTime startUtc;
            DateTime endUtc;
            if (!TryReadDateRange(out startUtc, out endUtc))
            {
                // <lang>
                //   <zh-CN>输入无效时同时清空列表和分页控件，避免继续展示上一组日期的诊断数据。</zh-CN>
                //   <en>Clear both the list and paging controls for invalid input so diagnostics from the previous date range are not left visible.</en>
                // </lang>
                // <lang>
                //   <zh-CN>显式抑制空态：此处是**输入无效**而非"没有日志"，渲染"暂无…"会把错误提示混成空结果。</zh-CN>
                //   <en>Suppress the empty state explicitly: this is **invalid input** rather than "no logs", so rendering
                //   "nothing here" would blend an error message into an empty result.</en>
                // </lang>
                entriesEmptyStateRowCount = null;
                EntriesRepeater.DataSource = null;
                EntriesRepeater.DataBind();
                PreviousButton.Visible = false;
                NextButton.Visible = false;
                return;
            }

            var query = new PortalDiagnosticQuery
            {
                StartUtc = startUtc,
                EndUtcExclusive = endUtc.AddDays(1),
                Level = LevelFilter.SelectedValue,
                Category = CategoryFilter.Text,
                EventId = EventIdFilter.Text,
                Page = CurrentPage,
                PageSize = PageSize
            };

            // <lang>
            //   <zh-CN>查询对象只携带受控筛选和页边界；服务负责数据可用性、扫描上限和低敏投影。</zh-CN>
            //   <en>The query carries only controlled filters and page bounds; the service owns availability, scan limits, and low-sensitivity projection.</en>
            // </lang>
            PortalDiagnosticQueryResult result = PortalDiagnosticQueryService.Query(query);

            // <lang>
            //   <zh-CN>绑定前记录行数：标记层在表尾据此渲染空态行；<c>null</c> 表示抑制（输入无效路径）。</zh-CN>
            //   <en>Record the row count before binding: the markup renders the empty-state row in the footer from it; <c>null</c> suppresses it on the invalid-input path.</en>
            // </lang>
            entriesEmptyStateRowCount = result.Entries.Count;
            EntriesRepeater.DataSource = result.Entries;
            EntriesRepeater.DataBind();

            // <lang>
            //   <zh-CN>分页可见性依据当前索引和服务 HasMore 结果计算，截断提示不改变已绑定的低敏事件集合。</zh-CN>
            //   <en>Paging visibility follows the current index and service HasMore result; a truncation notice does not alter the bound low-sensitivity event set.</en>
            // </lang>
            PreviousButton.Visible = CurrentPage > 0;
            NextButton.Visible = result.HasMore;
            // <lang>
            //   <zh-CN>用户可见消息统一取自资源并用编号占位符 ＋ CurrentCulture 格式化数字；跨页完全相同的消息共用一个 Admin_Common_* 键，避免同文案出现多份翻译来源。</zh-CN>
            //   <en>User-visible messages come from resources with numbered placeholders, formatted with CurrentCulture; messages that are identical across pages share one Admin_Common_* key so the same text never has two translation sources.</en>
            // </lang>
            ResultLabel.Text = string.Format(CultureInfo.CurrentCulture, lang.Admin_Common_PageInfo, CurrentPage + 1, result.Entries.Count);
            if (result.WasTruncated)
            {
                MessageLabel.Text = lang.Admin_DiagnosticsLogs_ScanLimitReached;
            }
            else
            {
                MessageLabel.Text = string.Empty;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析并校验 UTC 日期范围。</zh-CN>
        ///   <en>Parses and validates the UTC date range.</en>
        /// </lang>
        /// </summary>
        /// <param name="startUtc">
        /// <l>
        ///   <zh-CN>解析出的起始 UTC 日期。</zh-CN>
        ///   <en>The parsed start UTC date.</en>
        /// </l>
        /// </param>
        /// <param name="endUtc">
        /// <l>
        ///   <zh-CN>解析出的结束 UTC 日期。</zh-CN>
        ///   <en>The parsed end UTC date.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>输入符合固定格式、顺序和 31 天上限时返回 true。</zh-CN>
        ///   <en>True when the input satisfies the fixed format, ordering, and 31-day limit.</en>
        /// </l>
        /// </returns>
        private bool TryReadDateRange(out DateTime startUtc, out DateTime endUtc)
        {
            startUtc = DateTime.MinValue;
            endUtc = DateTime.MinValue;
            if (!DateTime.TryParseExact(
                    StartDateTextBox.Text,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out startUtc) ||
                !DateTime.TryParseExact(
                    EndDateTextBox.Text,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out endUtc))
            {
                // <lang>
                //   <zh-CN>只接受不带本地化歧义的 yyyy-MM-dd 文本，失败时不向查询服务传递部分解析值。</zh-CN>
                //   <en>Accept only unambiguous yyyy-MM-dd text and do not pass partially parsed values to the query service on failure.</en>
                // </lang>
                MessageLabel.Text = lang.Admin_Common_DateRangeFormat;
                return false;
            }

            if (endUtc < startUtc)
            {
                // <lang>
                //   <zh-CN>结束日期不得早于起始日期，保持服务端区间方向稳定。</zh-CN>
                //   <en>The end date cannot precede the start date, keeping the service interval direction stable.</en>
                // </lang>
                MessageLabel.Text = lang.Admin_Common_EndBeforeStart;
                return false;
            }

            if ((endUtc - startUtc).TotalDays >= 31)
            {
                // <lang>
                //   <zh-CN>限制小于 31 天的输入窗口，避免后台查询放大扫描成本。</zh-CN>
                //   <en>Limit the input window to fewer than 31 days to avoid amplifying the administrative scan cost.</en>
                // </lang>
                MessageLabel.Text = lang.Admin_Common_DateRangeMax31Days;
                return false;
            }

            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>条目列表的空态行数：有数据为正数、零条为 <c>0</c>、输入无效为 <c>null</c>（抑制空态）。</zh-CN>
        ///   <en>Empty-state row count of the entry list: positive when rows exist, <c>0</c> when the result is empty, and <c>null</c> for invalid input, which suppresses the empty state.</en>
        /// </lang>
        /// </summary>
        protected int? EntriesEmptyStateRowCount
        {
            get { return entriesEmptyStateRowCount; }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>空态文案：只有日期范围（查询必需边界）时用"无日志"；级别/类别/事件号任一非空时用"筛选无结果"。</zh-CN>
        ///   <en>Empty-state wording: "no logs" when only the date range (a required query bound) is set, and "no matching logs" when any of level, category, or event id is non-empty.</en>
        /// </lang>
        /// </summary>
        protected string EntriesEmptyStateText
        {
            get
            {
                return string.IsNullOrEmpty(LevelFilter.SelectedValue) && string.IsNullOrEmpty(CategoryFilter.Text) && string.IsNullOrEmpty(EventIdFilter.Text)
                    ? lang.Admin_DiagnosticsLogs_EmptyNoEntries
                    : lang.Admin_DiagnosticsLogs_EmptyNoMatches;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>条目列表的空态行数；由 <see cref="BindEntries"/> 在绑定前写入，输入无效时置为 <c>null</c>。</zh-CN>
        ///   <en>Empty-state row count of the entry list; written by <see cref="BindEntries"/> before binding and set to <c>null</c> for invalid input.</en>
        /// </lang>
        /// </summary>
        private int? entriesEmptyStateRowCount;
    }
}
