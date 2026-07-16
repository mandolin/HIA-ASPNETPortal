using System;
using System.Globalization;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 只读运营审计查询页面。
    /// Read-only operations-audit query page.
    /// </summary>
    public partial class OperationAudits : PortalPage<OperationAudits>
    {
        private const int PageSize = 50;

        /// <summary>
        /// 初始化管理员运营审计查询页面。
        /// Initializes the administrator operations-audit query page.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AuditOperationView))
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
        /// 应用新的审计筛选条件并回到第一页。
        /// Applies new audit filters and returns to the first page.
        /// </summary>
        protected void SearchButton_Click(object sender, EventArgs e)
        {
            CurrentPage = 0;
            BindEntries();
        }

        /// <summary>
        /// 读取上一页审计记录。
        /// Reads the previous audit page.
        /// </summary>
        protected void PreviousButton_Click(object sender, EventArgs e)
        {
            CurrentPage = Math.Max(0, CurrentPage - 1);
            BindEntries();
        }

        /// <summary>
        /// 读取下一页审计记录。
        /// Reads the next audit page.
        /// </summary>
        protected void NextButton_Click(object sender, EventArgs e)
        {
            CurrentPage++;
            BindEntries();
        }

        private int CurrentPage
        {
            get
            {
                object value = ViewState["OperationAudits.CurrentPage"];
                return value is int ? (int)value : 0;
            }
            set { ViewState["OperationAudits.CurrentPage"] = Math.Max(0, value); }
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

            PortalOperationAuditQueryResult result = PortalOperationAudit.Query(query, Context);
            EntriesRepeater.DataSource = result.Entries;
            EntriesRepeater.DataBind();
            PreviousButton.Visible = CurrentPage > 0;
            NextButton.Visible = result.HasMore;

            if (!result.IsAvailable)
            {
                MessageLabel.Text = "The operations audit table is unavailable. Run PortalCfg_OperationAudits.sql for this database.";
                ResultLabel.Text = string.Empty;
                return;
            }

            MessageLabel.Text = string.Empty;
            ResultLabel.Text = "Page " + (CurrentPage + 1) + "; entries: " + result.Entries.Count + ".";
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
