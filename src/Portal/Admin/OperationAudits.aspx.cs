using System;
using System.Globalization;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>只读运营审计查询页面。</zh-CN>
    ///   <en>Read-only operations-audit query page.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>页面只查询已净化的运营审计投影，不写入审计记录、不替代授权，并在审计表不可用时清晰提示隔离数据库初始化要求。</zh-CN>
    ///   <en>The page queries only sanitized operations-audit projections; it does not write audit records or replace authorization, and clearly reports the isolated-database initialization requirement when the audit table is unavailable.</en>
    /// </lang>
    /// </remarks>
    public partial class OperationAudits : PortalPage<OperationAudits>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>每页最多展示的运营审计记录数；这是页面读取上限，不替代审计服务扫描上限。</zh-CN>
        ///   <en>Maximum operations-audit records displayed per page; this is a page-read cap and does not replace the audit-service scan limit.</en>
        /// </lang>
        /// </summary>
        private const int PageSize = 50;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化管理员运营审计查询页面。</zh-CN>
        ///   <en>Initializes the administrator operations-audit query page.</en>
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
            //   <zh-CN>每次生命周期先复核运营审计查看权限，拒绝时不调用审计查询服务。</zh-CN>
            //   <en>Recheck operations-audit-view permission on every lifecycle entry so rejection does not call the audit query service.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AuditOperationView))
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
        ///   <zh-CN>应用新的运营审计筛选条件并回到第一页。</zh-CN>
        ///   <en>Applies new operations-audit filters and returns to the first page.</en>
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
            //   <zh-CN>新筛选从第一页开始；BindEntries 仍负责日期和审计存储可用性边界。</zh-CN>
            //   <en>New filters start at page one; BindEntries remains responsible for date and audit-store availability boundaries.</en>
            // </lang>
            CurrentPage = 0;
            BindEntries();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取上一页运营审计记录。</zh-CN>
        ///   <en>Reads the previous operations-audit page.</en>
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
            //   <zh-CN>页面索引在减法后保持非负，避免伪造回发值产生负页查询。</zh-CN>
            //   <en>Keep the page index non-negative after decrementing so forged postback values cannot produce a negative-page query.</en>
            // </lang>
            CurrentPage = Math.Max(0, CurrentPage - 1);
            BindEntries();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取下一页运营审计记录。</zh-CN>
        ///   <en>Reads the next operations-audit page.</en>
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
            //   <zh-CN>下一页动作不直接信任按钮可见性，实际可用性由审计查询结果 HasMore 决定。</zh-CN>
            //   <en>The next-page action does not trust button visibility; actual availability is determined by the audit-query result HasMore flag.</en>
            // </lang>
            CurrentPage++;
            BindEntries();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>保存当前运营审计查询页索引。</zh-CN>
        ///   <en>Stores the current operations-audit query page index.</en>
        /// </lang>
        /// </summary>
        private int CurrentPage
        {
            get
            {
                object value = ViewState["OperationAudits.CurrentPage"];
                return value is int ? (int)value : 0;
            }
            // <lang>
            //   <zh-CN>ViewState 只承载非负索引，避免回发状态将审计查询游标写成负数。</zh-CN>
            //   <en>ViewState carries only a non-negative index so postback state cannot write a negative audit-query cursor.</en>
            // </lang>
            set { ViewState["OperationAudits.CurrentPage"] = Math.Max(0, value); }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按当前日期和文本筛选条件查询并绑定运营审计记录。</zh-CN>
        ///   <en>Queries and binds operations-audit records using the current date and text filters.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>日期解析失败时清空旧结果；成功后将页码、固定页大小和受控筛选值交给只读审计查询，并把存储不可用作为明确回退。</zh-CN>
        ///   <en>Invalid dates clear stale results; valid dates pass the page, fixed page size, and controlled filters to the read-only audit query, treating unavailable storage as an explicit fallback.</en>
        /// </lang>
        /// </remarks>
        private void BindEntries()
        {
            DateTime startUtc;
            DateTime endUtc;
            if (!TryReadDateRange(out startUtc, out endUtc))
            {
                // <lang>
                //   <zh-CN>输入无效时同时清空列表和分页控件，避免继续展示上一组日期的审计数据。</zh-CN>
                //   <en>Clear both the list and paging controls for invalid input so audit records from the previous date range are not left visible.</en>
                // </lang>
                EntriesRepeater.DataSource = null;
                EntriesRepeater.DataBind();
                PreviousButton.Visible = false;
                NextButton.Visible = false;
                return;
            }

            var query = new PortalOperationAuditQuery
            {
                StartUtc = startUtc,
                EndUtcExclusive = endUtc.AddDays(1),
                Category = CategoryFilter.Text,
                Action = ActionFilter.Text,
                TargetId = TargetIdFilter.Text,
                Page = CurrentPage,
                PageSize = PageSize
            };

            // <lang>
            //   <zh-CN>查询使用当前请求上下文供服务完成审计读取边界；页面本身不写审计或改变授权。</zh-CN>
            //   <en>The query uses the current request context for the service's audit-read boundary; the page itself neither writes audit records nor changes authorization.</en>
            // </lang>
            PortalOperationAuditQueryResult result = PortalOperationAudit.Query(query, Context);
            EntriesRepeater.DataSource = result.Entries;
            EntriesRepeater.DataBind();

            // <lang>
            //   <zh-CN>分页可见性依据当前索引和服务 HasMore 结果计算，先绑定低敏投影再处理存储可用性提示。</zh-CN>
            //   <en>Compute paging visibility from the current index and service HasMore result, binding the low-sensitivity projection before handling availability messaging.</en>
            // </lang>
            PreviousButton.Visible = CurrentPage > 0;
            NextButton.Visible = result.HasMore;

            if (!result.IsAvailable)
            {
                // <lang>
                //   <zh-CN>审计表缺失时保留服务返回的空结果并给出固定迁移提示，不泄露连接或 SQL 细节。</zh-CN>
                //   <en>When the audit table is missing, keep the service-provided empty result and show a fixed migration hint without exposing connection or SQL details.</en>
                // </lang>
                MessageLabel.Text = "The operations audit table is unavailable. Run PortalCfg_OperationAudits.sql for this database.";
                ResultLabel.Text = string.Empty;
                return;
            }

            MessageLabel.Text = string.Empty;
            ResultLabel.Text = "Page " + (CurrentPage + 1) + "; entries: " + result.Entries.Count + ".";
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
                //   <zh-CN>只接受不带本地化歧义的 yyyy-MM-dd 文本，失败时不向审计服务传递部分解析值。</zh-CN>
                //   <en>Accept only unambiguous yyyy-MM-dd text and do not pass partially parsed values to the audit service on failure.</en>
                // </lang>
                MessageLabel.Text = "Enter Start UTC and End UTC using yyyy-MM-dd.";
                return false;
            }

            if (endUtc < startUtc)
            {
                // <lang>
                //   <zh-CN>结束日期不得早于起始日期，保持审计查询区间方向稳定。</zh-CN>
                //   <en>The end date cannot precede the start date, keeping the audit-query interval direction stable.</en>
                // </lang>
                MessageLabel.Text = "End UTC must be on or after Start UTC.";
                return false;
            }

            if ((endUtc - startUtc).TotalDays >= 31)
            {
                // <lang>
                //   <zh-CN>限制小于 31 天的输入窗口，避免运营审计查询放大扫描成本。</zh-CN>
                //   <en>Limit the input window to fewer than 31 days to avoid amplifying the operations-audit scan cost.</en>
                // </lang>
                MessageLabel.Text = "The date range must not exceed 31 days.";
                return false;
            }

            return true;
        }
    }
}
