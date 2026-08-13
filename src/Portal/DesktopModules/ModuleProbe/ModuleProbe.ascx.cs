using System;
using System.Globalization;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>受信任部署模块包的只读验证样例。</zh-CN>
    ///   <en>Read-only verification sample for a trusted-deployment module package.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本模块只展示 P3.2 catalog 已验证的运行时元数据，不读取或写入业务数据，也不提供上传、脚本、远程资源或动态编译能力。</zh-CN>
    ///   <en>This module displays only runtime metadata validated by the P3.2 catalog. It reads and writes no business data and provides no upload, script, remote-resource, or dynamic-compilation capability.</en>
    /// </lang>
    /// </remarks>
    public partial class ModuleProbe : PortalModuleControl<ModuleProbe>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化模块验证信息。</zh-CN>
        ///   <en>Initializes the module verification information.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender"><lang><zh-CN>事件源。</zh-CN><en>Event source.</en></lang></param>
        /// <param name="e"><lang><zh-CN>事件参数。</zh-CN><en>Event arguments.</en></lang></param>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                BindProbeInformation();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从受控模块描述和当前门户上下文写入只读展示值。</zh-CN>
        ///   <en>Writes read-only display values from the controlled module descriptor and current portal context.</en>
        /// </lang>
        /// </summary>
        private void BindProbeInformation()
        {
            // <lang>
            //   <zh-CN>保留解析出的模块描述，供包标识、版本、启用状态和来源字段共享同一快照。</zh-CN>
            //   <en>Keeps the resolved module descriptor so package identity, version, enabled state, and source share one snapshot.</en>
            // </lang>
            PortalModuleRuntimeDescriptor descriptor;

            // <lang>
            //   <zh-CN>保存解析失败的低敏原因；它只进入不可用展示，不作为授权或部署信任结论。</zh-CN>
            //   <en>Stores the low-sensitivity resolution reason for unavailable display only; it is not an authorization or deployment-trust conclusion.</en>
            // </lang>
            string reason;

            // <lang>
            //   <zh-CN>仅表示受控目录解析是否成功，不能绕过模块状态或页面权限边界。</zh-CN>
            //   <en>Indicates only whether controlled catalog resolution succeeded and cannot bypass module-state or page-permission boundaries.</en>
            // </lang>
            bool resolved = PortalModuleCatalog.TryResolveModule(ModuleConfiguration, Context, out descriptor, out reason);

            // <lang>
            //   <zh-CN>把受控包元数据投影为低敏短文本；解析失败或包缺失时只展示固定不可用前缀和原因。</zh-CN>
            //   <en>Projects controlled package metadata into bounded low-sensitivity text; missing resolution or package data uses a fixed unavailable prefix and reason.</en>
            // </lang>
            string packageText = resolved && descriptor != null && descriptor.Package != null
                ? descriptor.Package.PackageId + " v" + descriptor.Package.Version +
                  (descriptor.IsEnabled ? " (enabled)" : " (disabled)")
                : "Unavailable: " + reason;

            // <lang>
            //   <zh-CN>输出前统一 HTML 编码包元数据，防止受信描述文本改变页面标记。</zh-CN>
            //   <en>HTML-encodes package metadata before output so trusted-descriptor text cannot change page markup.</en>
            // </lang>
            PackageLabel.Text = Server.HtmlEncode(packageText);

            // <lang>
            //   <zh-CN>只组合稳定模块标识和受控来源；未解析时使用固定占位，不回显原始异常。</zh-CN>
            //   <en>Combines stable module identity with the controlled source and uses a fixed placeholder when unresolved instead of echoing raw errors.</en>
            // </lang>
            ModuleLabel.Text = Server.HtmlEncode(
                "Id=" + ModuleConfiguration.ModuleId.ToString(CultureInfo.InvariantCulture) +
                "; Source=" + (resolved && descriptor != null ? descriptor.DesktopSource : "(unavailable)"));
            // <lang>
            //   <zh-CN>读取当前门户设置快照，仅用于展示所在 Tab，不执行授权或持久化。</zh-CN>
            //   <en>Reads the current portal-settings snapshot only to display the containing Tab; it performs no authorization or persistence.</en>
            // </lang>
            PortalSettings portalSettings = PortalContext.GetPortalSettings(Context);

            // <lang>
            //   <zh-CN>缺少门户或活动 Tab 时使用零值，保持验证页可渲染而不虚构位置。</zh-CN>
            //   <en>Uses zero when the portal or active Tab is unavailable so the proof page renders without inventing a placement.</en>
            // </lang>
            int tabId = portalSettings == null || portalSettings.ActiveTab == null
                ? 0
                : portalSettings.ActiveTab.TabId;
            // <lang>
            //   <zh-CN>输出位置和窗格前统一编码，避免模块配置文本进入页面标记。</zh-CN>
            //   <en>Encodes placement and pane text before output so module-configuration text cannot enter page markup.</en>
            // </lang>
            PlacementLabel.Text = Server.HtmlEncode(
                "Tab=" + tabId.ToString(CultureInfo.InvariantCulture) +
                "; Pane=" + ModuleConfiguration.PaneName);
            // <lang>
            //   <zh-CN>展示当前主题 CSS 作用域；主题解析失败时沿用解析器的既有回退。</zh-CN>
            //   <en>Displays the current theme CSS scope while preserving the resolver's existing fallback on failure.</en>
            // </lang>
            ThemeScopeLabel.Text = Server.HtmlEncode(PortalThemeResolver.GetCurrentCssClass(Context));

            // <lang>
            //   <zh-CN>缓存验收通过该非敏感时间标记判断命中与包状态修订后的失效，不读取业务数据。</zh-CN>
            //   <en>The cache proof uses this non-sensitive timestamp to verify hits and invalidation after a package-state revision.</en>
            // </lang>
            RenderedUtcLabel.Text = Server.HtmlEncode(DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        }
    }
}
