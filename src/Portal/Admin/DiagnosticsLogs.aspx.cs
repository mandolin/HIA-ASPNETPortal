using System;
using System.Globalization;
using System.Web;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 只读结构化诊断日志查询页面。
    /// Read-only structured diagnostics log query page.
    /// </summary>
    public partial class DiagnosticsLogs : PortalPage<DiagnosticsLogs>
    {
        private const int PageSize = 50;

        /// <summary>
        /// 初始化管理员日志查询页面。
        /// Initializes the administrator log-query page.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.OpsDiagnosticsView))
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                DateTime todayUtc = DateTime.UtcNow.Date;
                StartDateTextBox.Text = todayUtc.AddDays(-6).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                EndDateTextBox.Text = todayUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                BindEntries();
            }
        }

        /// <summary>
        /// 应用新的筛选条件并回到第一页。
        /// Applies new filters and returns to the first page.
        /// </summary>
        protected void SearchButton_Click(object sender, EventArgs e)
        {
            CurrentPage = 0;
            BindEntries();
        }

        /// <summary>
        /// 读取上一页。
        /// Reads the previous page.
        /// </summary>
        protected void PreviousButton_Click(object sender, EventArgs e)
        {
            CurrentPage = Math.Max(0, CurrentPage - 1);
            BindEntries();
        }

        /// <summary>
        /// 读取下一页。
        /// Reads the next page.
        /// </summary>
        protected void NextButton_Click(object sender, EventArgs e)
        {
            CurrentPage++;
            BindEntries();
        }

        /// <summary>
        /// 构建只接受事件编号的详情链接。
        /// Builds a detail link that accepts only an event id.
        /// </summary>
        public string GetDetailUrl(object eventId)
        {
            return ResolveUrl("~/Admin/DiagnosticLogDetail.aspx?id=" + HttpUtility.UrlEncode(Convert.ToString(eventId)));
        }

        private int CurrentPage
        {
            get
            {
                object value = ViewState["DiagnosticsLogs.CurrentPage"];
                return value is int ? (int)value : 0;
            }
            set { ViewState["DiagnosticsLogs.CurrentPage"] = Math.Max(0, value); }
        }

        private void BindEntries()
        {
            DateTime startUtc;
            DateTime endUtc;
            if (!TryReadDateRange(out startUtc, out endUtc))
            {
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

            PortalDiagnosticQueryResult result = PortalDiagnosticQueryService.Query(query);
            EntriesRepeater.DataSource = result.Entries;
            EntriesRepeater.DataBind();

            PreviousButton.Visible = CurrentPage > 0;
            NextButton.Visible = result.HasMore;
            ResultLabel.Text = "Page " + (CurrentPage + 1) + "; entries: " + result.Entries.Count + ".";
            if (result.WasTruncated)
            {
                MessageLabel.Text = "The server scan limit was reached. Narrow the date range or filters.";
            }
            else
            {
                MessageLabel.Text = string.Empty;
            }
        }

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
                MessageLabel.Text = "Enter Start UTC and End UTC using yyyy-MM-dd.";
                return false;
            }

            if (endUtc < startUtc)
            {
                MessageLabel.Text = "End UTC must be on or after Start UTC.";
                return false;
            }

            if ((endUtc - startUtc).TotalDays >= 31)
            {
                MessageLabel.Text = "The date range must not exceed 31 days.";
                return false;
            }

            return true;
        }
    }
}
