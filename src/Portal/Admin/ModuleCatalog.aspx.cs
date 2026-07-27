using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>受信任部署模块包的注册、启停与预检目录页。</zh-CN>
    ///   <en>Catalog page for registering, enabling, disabling, and preflighting trusted-deployment module packages.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P3.2 只消费服务器已部署且通过 manifest 校验的包，不提供上传、解压、DLL、在线编译、外部 URL 或自动脚本能力。物理目录始终由受信任部署流程负责，后台只管理数据库定义和启用状态。</zh-CN>
    ///   <en>P3.2 consumes only server-deployed packages passing manifest validation. It provides no upload, extraction, DLL, online-compilation, external-URL, or automatic-script capability. Physical directories remain the responsibility of a trusted deployment process; this page manages database definitions and enabled state only.</en>
    /// </lang>
    /// </remarks>
    public partial class ModuleCatalog : PortalPage<ModuleCatalog>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>旧模块定义数据服务，仅用于创建或匹配受控入口。</zh-CN>
        ///   <en>Legacy module-definition data service, used only to create or match controlled entries.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IModuleDefsDb ModuleDefConfig { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>旧模块实例数据服务，仅用于预检引用数量。</zh-CN>
        ///   <en>Legacy module-instance data service, used only to preflight reference counts.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IModulesDb ModulesConfig { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化已验证部署包目录。</zh-CN>
        ///   <en>Initializes the validated deployment-package catalog.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>事件参数。</zh-CN>
        ///   <en>Event arguments.</en>
        /// </l>
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.ModuleCatalogView))
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                BindPackages();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理已验证模块包的受限目录操作。</zh-CN>
        ///   <en>Handles restricted catalog actions for a validated module package.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>GridView 命令事件参数。</zh-CN>
        ///   <en>GridView command event arguments.</en>
        /// </l>
        /// </param>
        protected void PackagesGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            string permissionKey = string.Equals(e.CommandName, "Preflight", StringComparison.OrdinalIgnoreCase)
                ? PortalPermissionKeys.ModuleCatalogView
                : PortalPermissionKeys.ModuleCatalogEdit;
            if (!PortalAuthorization.EnsurePermission(Context, permissionKey))
            {
                return;
            }

            string packageId = Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture);
            PortalModulePackage package;
            string reason;
            if (!PortalModuleCatalog.TryGetTrustedPackage(packageId, out package, out reason))
            {
                ShowMessage("The selected module package is no longer deployed or is invalid.");
                BindPackages();
                return;
            }

            PortalModuleProfileSnapshot profile = PortalModuleProfileResolver.Resolve(Context);
            if (!string.Equals(e.CommandName, "Preflight", StringComparison.OrdinalIgnoreCase) &&
                !profile.IsPackageAllowed(package.PackageId))
            {
                ShowMessage(
                    "The selected module package is deployed, but it is not allowed by active module profile '" +
                    profile.ActiveProfile + "'.");
                BindPackages();
                return;
            }

            switch (e.CommandName)
            {
                case "Register":
                    RegisterPackage(package);
                    break;
                case "Enable":
                    SavePackageState(package, true);
                    break;
                case "Disable":
                    SavePackageState(package, false);
                    break;
                case "Preflight":
                    ShowPreflight(package);
                    break;
            }

            BindPackages();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定已验证包及其数据库注册和实例引用摘要。</zh-CN>
        ///   <en>Binds validated packages and their database registration and instance-reference summary.</en>
        /// </lang>
        /// </summary>
        private void BindPackages()
        {
            IList<IModuleDefinitionItem> definitions = ModuleDefConfig.GetModuleDefinitions().ToList();
            PortalModuleProfileSnapshot profile = PortalModuleProfileResolver.Resolve(Context);
            var rows = new List<ModuleCatalogRow>();

            foreach (PortalModulePackage package in PortalModuleCatalog.GetTrustedPackages())
            {
                IModuleDefinitionItem definition = FindDefinition(definitions, package.DesktopEntry);
                PortalModulePackageStateReadResult stateResult = PortalModulePackageStates.Read(package.PackageId, Context);
                bool isEnabled = !stateResult.IsAvailable || stateResult.State == null || stateResult.State.IsEnabled;
                bool isProfileAllowed = profile.IsPackageAllowed(package.PackageId);
                int instanceCount = definition == null
                    ? 0
                    : ModulesConfig.GetModulesByModuleDefId(definition.ModuleDefId).Count();

                rows.Add(new ModuleCatalogRow(
                    package,
                    isEnabled,
                    isProfileAllowed,
                    profile.ActiveProfile,
                    stateResult,
                    definition,
                    instanceCount));
            }

            PackagesGrid.DataSource = rows;
            PackagesGrid.DataBind();
            if (rows.Count == 0)
            {
                ResultLabel.Text = "No validated deployed module package was found.";
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>为已验证包创建仅指向 manifest 入口的旧定义记录。</zh-CN>
        ///   <en>Creates a legacy definition record pointing only to a validated manifest entry.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>此操作写入旧模块定义表并写入运营审计，但不复制、上传、删除或修改包目录。重复入口仅返回既有定义，不会创建第二条定义记录。</zh-CN>
        ///   <en>This operation writes the legacy module-definition table and an operations audit, but never copies, uploads, deletes, or modifies the package directory. A duplicate entry returns the existing definition and does not create a second definition record.</en>
        /// </lang>
        /// </remarks>
        private void RegisterPackage(PortalModulePackage package)
        {
            IList<IModuleDefinitionItem> definitions = ModuleDefConfig.GetModuleDefinitions().ToList();
            IModuleDefinitionItem existing = FindDefinition(definitions, package.DesktopEntry);
            if (existing != null)
            {
                ResultLabel.Text = "The package entry is already registered as module definition " +
                                   existing.ModuleDefId.ToString(CultureInfo.InvariantCulture) + ".";
                return;
            }

            int definitionId = ModuleDefConfig.AddModuleDefinition(
                package.DisplayName,
                package.DesktopEntry,
                string.Empty);
            PortalOperationAudit.Record(
                "ModulePackage",
                "RegisterDefinition",
                "ModulePackage",
                package.PackageId,
                "Registered module definition " + definitionId.ToString(CultureInfo.InvariantCulture) +
                " from validated deployed package.",
                Context);
            ResultLabel.Text = "The validated package was registered as module definition " +
                               definitionId.ToString(CultureInfo.InvariantCulture) + ".";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>保存已验证包的启用状态，并记录高价值运营审计。</zh-CN>
        ///   <en>Saves a validated package enabled state and records a high-value operations audit.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>状态表不可用或写入失败时不改变模块文件，也不伪造成功结果；失败详情由状态存储写入诊断。</zh-CN>
        ///   <en>When the state table is unavailable or a write fails, this method does not alter module files or fabricate a successful result; failure details are recorded in diagnostics by the state store.</en>
        /// </lang>
        /// </remarks>
        private void SavePackageState(PortalModulePackage package, bool isEnabled)
        {
            PortalModulePackageStateWriteResult result = PortalModulePackageStates.Save(
                package.PackageId,
                isEnabled,
                string.Empty,
                Context);
            if (!result.Succeeded)
            {
                ShowMessage(result.Message);
                return;
            }

            PortalOperationAudit.Record(
                "ModulePackage",
                isEnabled ? "Enable" : "Disable",
                "ModulePackage",
                package.PackageId,
                isEnabled ? "Enabled validated deployed module package." : "Disabled validated deployed module package.",
                Context);
            ResultLabel.Text = result.Message;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>展示包定义和实例引用数量，不执行删除、迁移或物理文件操作。</zh-CN>
        ///   <en>Displays definition and instance reference counts without deleting, migrating, or touching physical files.</en>
        /// </lang>
        /// </summary>
        private void ShowPreflight(PortalModulePackage package)
        {
            IModuleDefinitionItem definition = FindDefinition(
                ModuleDefConfig.GetModuleDefinitions().ToList(),
                package.DesktopEntry);
            if (definition == null)
            {
                ResultLabel.Text = "The package has no registered legacy module definition.";
                return;
            }

            int instanceCount = ModulesConfig.GetModulesByModuleDefId(definition.ModuleDefId).Count();
            ResultLabel.Text = "Definition " + definition.ModuleDefId.ToString(CultureInfo.InvariantCulture) +
                               " has " + instanceCount.ToString(CultureInfo.InvariantCulture) +
                               " module instance(s). Disable, migrate, or explicitly clean instances before any removal.";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按已规范化桌面入口匹配旧模块定义。</zh-CN>
        ///   <en>Matches a legacy module definition by normalized desktop entry.</en>
        /// </lang>
        /// </summary>
        private static IModuleDefinitionItem FindDefinition(
            IEnumerable<IModuleDefinitionItem> definitions,
            string desktopEntry)
        {
            return definitions.FirstOrDefault(item =>
                string.Equals(item.DesktopSourceFile, desktopEntry, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>展示不含物理路径或异常详情的管理员安全提示。</zh-CN>
        ///   <en>Displays an administrator-safe message without physical paths or exception details.</en>
        /// </lang>
        /// </summary>
        private void ShowMessage(string message)
        {
            MessageLabel.Text = Server.HtmlEncode(message ?? string.Empty);
            ResultLabel.Text = string.Empty;
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>模块目录 GridView 的只读展示行。</zh-CN>
    ///   <en>Read-only display row for the module-catalog GridView.</en>
    /// </lang>
    /// </summary>
    public sealed class ModuleCatalogRow
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>从已验证包、状态读取结果和旧定义摘要创建目录展示行。</zh-CN>
        ///   <en>Creates a catalog display row from a validated package, state-read result, and legacy definition summary.</en>
        /// </lang>
        /// </summary>
        internal ModuleCatalogRow(
            PortalModulePackage package,
            bool isEnabled,
            bool isProfileAllowed,
            string activeProfile,
            PortalModulePackageStateReadResult stateResult,
            IModuleDefinitionItem definition,
            int instanceCount)
        {
            PackageId = package.PackageId;
            DisplayName = package.DisplayName;
            Version = package.Version;
            DesktopEntry = package.DesktopEntry;
            IsEnabled = isEnabled;
            IsProfileAllowed = isProfileAllowed;
            IsRegistered = definition != null;
            DefinitionText = definition == null
                ? "Not registered"
                : definition.ModuleDefId.ToString(CultureInfo.InvariantCulture);
            InstanceCount = instanceCount;
            ProfileText = isProfileAllowed
                ? "Allowed by " + activeProfile
                : "Blocked by " + activeProfile;
            StateText = !isProfileAllowed
                ? "Profile blocked"
                : !stateResult.IsAvailable
                ? "Enabled (state table unavailable)"
                : stateResult.State == null || !stateResult.State.IsConfigured
                    ? "Enabled (default)"
                    : isEnabled ? "Enabled" : "Disabled";
        }

        /// <summary>
        /// <l>
        ///   <zh-CN>稳定部署包标识。</zh-CN>
        ///   <en>Stable deployment-package identifier.</en>
        /// </l>
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>管理员展示名称。</zh-CN>
        ///   <en>Administrator display name.</en>
        /// </l>
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>部署包版本。</zh-CN>
        ///   <en>Deployment-package version.</en>
        /// </l>
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>已验证桌面入口。</zh-CN>
        ///   <en>Validated desktop entry.</en>
        /// </l>
        /// </summary>
        public string DesktopEntry { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>显示用启用状态。</zh-CN>
        ///   <en>Display enabled state.</en>
        /// </l>
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>是否已有旧定义记录。</zh-CN>
        ///   <en>Whether a legacy definition exists.</en>
        /// </l>
        /// </summary>
        public bool IsRegistered { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>当前启动期 Profile 是否允许该包进入门户能力集。</zh-CN>
        ///   <en>Whether the current startup Profile allows this package into the Portal capability set.</en>
        /// </l>
        /// </summary>
        public bool IsProfileAllowed { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>显示用 Profile 状态。</zh-CN>
        ///   <en>Display Profile state.</en>
        /// </l>
        /// </summary>
        public string ProfileText { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>显示用状态文本。</zh-CN>
        ///   <en>Display state text.</en>
        /// </l>
        /// </summary>
        public string StateText { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>显示用定义摘要。</zh-CN>
        ///   <en>Display definition summary.</en>
        /// </l>
        /// </summary>
        public string DefinitionText { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>引用该定义的模块实例数量。</zh-CN>
        ///   <en>Module-instance count referencing the definition.</en>
        /// </l>
        /// </summary>
        public int InstanceCount { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>是否显示注册按钮。</zh-CN>
        ///   <en>Whether to show the register button.</en>
        /// </l>
        /// </summary>
        public bool CanRegister
        {
            get { return IsProfileAllowed && !IsRegistered; }
        }

        /// <summary>
        /// <l>
        ///   <zh-CN>是否显示启用按钮。</zh-CN>
        ///   <en>Whether to show the enable button.</en>
        /// </l>
        /// </summary>
        public bool CanEnable
        {
            get { return IsProfileAllowed && !IsEnabled; }
        }

        /// <summary>
        /// <l>
        ///   <zh-CN>是否显示禁用按钮。</zh-CN>
        ///   <en>Whether to show the disable button.</en>
        /// </l>
        /// </summary>
        public bool CanDisable
        {
            get { return IsProfileAllowed && IsEnabled; }
        }
    }
}
