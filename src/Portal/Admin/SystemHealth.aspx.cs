using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 只读系统健康状态页面。
    /// Read-only system health page.
    /// </summary>
    public partial class SystemHealth : PortalPage<SystemHealth>
    {
        /// <summary>
        /// 加载页面并绑定健康检查结果。
        /// Loads and binds health check results.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            PortalAuthorization.RequireAdmin();

            if (!Page.IsPostBack)
            {
                BindHealthSnapshot();
            }
        }

        /// <summary>
        /// 重新执行只读健康检查。
        /// Re-runs the read-only health check.
        /// </summary>
        protected void RefreshButton_Click(object sender, EventArgs e)
        {
            PortalAuthorization.RequireAdmin();
            BindHealthSnapshot();
        }

        private void BindHealthSnapshot()
        {
            PortalHealthSnapshot snapshot = PortalHealthChecker.Check(Context);
            OverallStatusLabel.Text = snapshot.OverallStatus.ToString();
            GeneratedUtcLabel.Text = snapshot.GeneratedUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

            HealthChecksRepeater.DataSource = snapshot.Checks;
            HealthChecksRepeater.DataBind();

            SettingsRepeater.DataSource = snapshot.Settings;
            SettingsRepeater.DataBind();
        }
    }
}
