using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>只读系统健康状态页面。</zh-CN>
    ///   <en>Read-only system health page.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>页面只执行受控健康检查并展示快照，不写入数据库、配置或运行状态；刷新动作仍受同一运维查看权限保护。</zh-CN>
    ///   <en>The page runs only the controlled health check and displays its snapshot; it does not write databases, configuration, or runtime state, and refresh remains protected by the same operations-view permission.</en>
    /// </lang>
    /// </remarks>
    public partial class SystemHealth : PortalPage<SystemHealth>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>加载页面并绑定健康检查结果。</zh-CN>
        ///   <en>Loads and binds health-check results.</en>
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
            //   <zh-CN>每次生命周期复核运维健康查看权限，拒绝时不执行健康检查或绑定。</zh-CN>
            //   <en>Recheck operations-health-view permission on every lifecycle entry so rejection runs no health check or binding.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.OpsHealthView))
            {
                return;
            }

            // <lang>
            //   <zh-CN>仅首次请求读取快照，回发由显式刷新动作决定是否重新检查。</zh-CN>
            //   <en>Read the snapshot only on the initial request; postbacks rerun the check only through the explicit refresh action.</en>
            // </lang>
            if (!Page.IsPostBack)
            {
                BindHealthSnapshot();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>重新执行只读健康检查。</zh-CN>
        ///   <en>Re-runs the read-only health check.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发刷新的 Web Forms 事件源。</zh-CN>
        ///   <en>The Web Forms event source that triggered refresh.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>刷新事件参数。</zh-CN>
        ///   <en>Refresh event arguments.</en>
        /// </l>
        /// </param>
        protected void RefreshButton_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>刷新回发再次复核权限，避免页面初次加载后权限变化仍触发健康检查。</zh-CN>
            //   <en>Recheck permission on refresh postback so a permission change after initial load cannot trigger the health check.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.OpsHealthView))
            {
                return;
            }

            BindHealthSnapshot();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取健康快照并绑定状态、检查项和设置摘要。</zh-CN>
        ///   <en>Reads a health snapshot and binds the status, check, and settings summaries.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>健康检查器负责数据来源、脱敏和失败回退；页面只投影快照字段，不自行解释检查结果或执行修复动作。</zh-CN>
        ///   <en>The health checker owns data sources, redaction, and failure fallback; the page only projects snapshot fields and does not interpret results or perform remediation.</en>
        /// </lang>
        /// </remarks>
        private void BindHealthSnapshot()
        {
            // <lang>
            //   <zh-CN>使用当前请求上下文执行受控检查，保留健康检查器统一的权限、配置和诊断边界。</zh-CN>
            //   <en>Use the current request context for the controlled check, preserving the health checker's shared permission, configuration, and diagnostics boundaries.</en>
            // </lang>
            PortalHealthSnapshot snapshot = PortalHealthChecker.Check(Context);
            OverallStatusLabel.Text = snapshot.OverallStatus.ToString();
            GeneratedUtcLabel.Text = snapshot.GeneratedUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

            // <lang>
            //   <zh-CN>三个控件只绑定快照投影，避免页面重复查询或把设置值当作可编辑配置。</zh-CN>
            //   <en>Bind only the snapshot projections to the three controls so the page neither repeats queries nor treats settings as editable configuration.</en>
            // </lang>
            HealthChecksRepeater.DataSource = snapshot.Checks;
            HealthChecksRepeater.DataBind();

            SettingsRepeater.DataSource = snapshot.Settings;
            SettingsRepeater.DataBind();
        }
    }
}
