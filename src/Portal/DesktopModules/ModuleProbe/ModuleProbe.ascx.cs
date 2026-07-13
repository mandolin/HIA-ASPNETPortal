using System;
using System.Globalization;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 受信任部署模块包的只读验证样例。
    /// Read-only verification sample for a trusted-deployment module package.
    /// </summary>
    /// <remarks>
    /// 本模块只展示 P3.2 catalog 已验证的运行时元数据，不读取或写入业务数据，
    /// 也不提供上传、脚本、远程资源或动态编译能力。
    /// This module displays only runtime metadata validated by the P3.2 catalog. It reads and writes no business
    /// data and provides no upload, script, remote-resource, or dynamic-compilation capability.
    /// </remarks>
    public partial class ModuleProbe : PortalModuleControl<ModuleProbe>
    {
        /// <summary>
        /// 初始化模块验证信息。
        /// Initializes the module verification information.
        /// </summary>
        /// <param name="sender">事件源。Event source.</param>
        /// <param name="e">事件参数。Event arguments.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                BindProbeInformation();
            }
        }

        /// <summary>
        /// 从受控模块描述和当前门户上下文写入只读展示值。
        /// Writes read-only display values from the controlled module descriptor and current portal context.
        /// </summary>
        private void BindProbeInformation()
        {
            PortalModuleRuntimeDescriptor descriptor;
            string reason;
            bool resolved = PortalModuleCatalog.TryResolveModule(ModuleConfiguration, Context, out descriptor, out reason);
            string packageText = resolved && descriptor != null && descriptor.Package != null
                ? descriptor.Package.PackageId + " v" + descriptor.Package.Version +
                  (descriptor.IsEnabled ? " (enabled)" : " (disabled)")
                : "Unavailable: " + reason;

            PackageLabel.Text = Server.HtmlEncode(packageText);
            ModuleLabel.Text = Server.HtmlEncode(
                "Id=" + ModuleConfiguration.ModuleId.ToString(CultureInfo.InvariantCulture) +
                "; Source=" + (resolved && descriptor != null ? descriptor.DesktopSource : "(unavailable)"));
            PortalSettings portalSettings = PortalContext.GetPortalSettings(Context);
            int tabId = portalSettings == null || portalSettings.ActiveTab == null
                ? 0
                : portalSettings.ActiveTab.TabId;
            PlacementLabel.Text = Server.HtmlEncode(
                "Tab=" + tabId.ToString(CultureInfo.InvariantCulture) +
                "; Pane=" + ModuleConfiguration.PaneName);
            ThemeScopeLabel.Text = Server.HtmlEncode(PortalThemeResolver.GetCurrentCssClass(Context));

            // 缓存验收通过该非敏感时间标记判断命中与包状态修订后的失效，不读取业务数据。
            // The cache proof uses this non-sensitive timestamp to verify hits and invalidation after a package-state revision.
            RenderedUtcLabel.Text = Server.HtmlEncode(DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        }
    }
}
