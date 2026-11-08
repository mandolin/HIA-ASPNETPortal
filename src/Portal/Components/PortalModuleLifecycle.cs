namespace Portal.Components
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>独立模块生命周期状态：部署后可安装、可运行、可卸载三态的派生事实。</zh-CN>
    ///   <en>Independent module lifecycle states: a derived fact across installed, runnable, and uninstallable.</en>
    /// </lang>
    /// </summary>
    public enum PortalModuleLifecycleState
    {
        /// <summary>不在可信目录（未部署或已卸载）。Not in the trusted catalog (not deployed or already uninstalled).</summary>
        Uninstalled = 0,
        /// <summary>已部署但当前不可运行（Profile 不允许或显式禁用）。Deployed but not currently runnable (profile disallows or disabled).</summary>
        Installed = 1,
        /// <summary>已部署、Profile 允许且已启用，可运行。Deployed, profile-allowed, and enabled; runnable.</summary>
        Active = 2
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>从可信目录、Profile 与状态表三事实派生模块生命周期状态，不写入新持久化列。</zh-CN>
    ///   <en>Derives the module lifecycle state from the trusted catalog, profile, and state table; does not write a new persisted column.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该派生复用 P48 装配基座（可信目录 + Profile gate + 状态表 IsEnabled），不重建 manifest 基础；卸载（W-anp-P285）清除文件与状态行后，包即退出可信目录而归为 Uninstalled。</zh-CN>
    ///   <en>The derivation reuses the P48 assembly base (trusted catalog + profile gate + state-table IsEnabled) and does not rebuild the manifest foundation; after uninstall (W-anp-P285) removes files and the state row, a package leaves the trusted catalog and becomes Uninstalled.</en>
    /// </lang>
    /// </remarks>
    public static class PortalModuleLifecycle
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>推导模块包生命周期状态。</zh-CN>
        ///   <en>Derives the lifecycle state of a module package.</en>
        /// </lang>
        /// </summary>
        /// <param name="packageId">
        /// <lang>
        ///   <zh-CN>模块包标识。</zh-CN>
        ///   <en>Module package identifier.</en>
        /// </lang>
        /// </param>
        /// <param name="context">
        /// <lang>
        ///   <zh-CN>可选 HTTP 上下文，仅传给 Profile/状态解析边界。</zh-CN>
        ///   <en>Optional HTTP context passed only to profile/state-resolution boundaries.</en>
        /// </lang>
        /// </param>
        /// <param name="state">
        /// <lang>
        ///   <zh-CN>推导得出的生命周期状态。</zh-CN>
        ///   <en>Derived lifecycle state.</en>
        /// </lang>
        /// </param>
        /// <param name="reason">
        /// <lang>
        ///   <zh-CN>受控非敏感原因；空字符串表示推导成功且无阻塞。</zh-CN>
        ///   <en>Controlled non-sensitive reason; empty string means derivation succeeded without blocking.</en>
        /// </lang>
        /// </param>
        /// <returns>
        /// <lang>
        ///   <zh-CN>解析成功（含推导为 Uninstalled）为 true；标识空白等前置不成立为 false。</zh-CN>
        ///   <en>true when derivation succeeds (including Uninstalled); false when prerequisites such as a blank identifier are not met.</en>
        /// </lang>
        /// </returns>
        public static bool TryGetLifecycleState(
            string packageId,
            System.Web.HttpContext context,
            out PortalModuleLifecycleState state,
            out string reason)
        {
            state = PortalModuleLifecycleState.Uninstalled;
            reason = string.Empty;

            if (string.IsNullOrWhiteSpace(packageId))
            {
                reason = "Module package identifier is unavailable.";
                return false;
            }

            string id = packageId.Trim();
            PortalModulePackage package;
            if (!PortalModuleCatalog.TryGetTrustedPackage(id, out package, out reason))
            {
                // <lang>
                //   <zh-CN>不在可信目录：未部署或已卸载，归为 Uninstalled。</zh-CN>
                //   <en>Not in the trusted catalog: not deployed or already uninstalled, classified as Uninstalled.</en>
                // </lang>
                state = PortalModuleLifecycleState.Uninstalled;
                reason = string.Empty;
                return true;
            }

            PortalModuleProfileSnapshot profile = PortalModuleProfileResolver.Resolve(context);
            if (!profile.IsPackageAllowed(package))
            {
                // <lang>
                //   <zh-CN>已部署但当前 Profile 不允许运行：归为 Installed（不可运行）。</zh-CN>
                //   <en>Deployed but the current profile disallows running: classified as Installed (not runnable).</en>
                // </lang>
                state = PortalModuleLifecycleState.Installed;
                return true;
            }

            PortalModulePackageStateReadResult stateResult = PortalModulePackageStates.Read(id, context);
            bool enabled = stateResult.IsAvailable
                && stateResult.State != null
                && stateResult.State.IsEnabled;
            state = enabled ? PortalModuleLifecycleState.Active : PortalModuleLifecycleState.Installed;
            return true;
        }
    }
}
