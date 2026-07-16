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
    /// 受信任部署模块包的注册、启停与预检目录页。
    /// Catalog page for registering, enabling, disabling, and preflighting trusted-deployment module packages.
    /// </summary>
    /// <remarks>
    /// P3.2 只消费服务器已部署且通过 manifest 校验的包，不提供上传、解压、DLL、在线编译、外部 URL
    /// 或自动脚本能力。物理目录始终由受信任部署流程负责，后台只管理数据库定义和启用状态。
    /// P3.2 consumes only server-deployed packages passing manifest validation. It provides no upload, extraction,
    /// DLL, online-compilation, external-URL, or automatic-script capability. Physical directories remain the
    /// responsibility of a trusted deployment process; this page manages database definitions and enabled state only.
    /// </remarks>
    public partial class ModuleCatalog : PortalPage<ModuleCatalog>
    {
        /// <summary>
        /// 旧模块定义数据服务，仅用于创建或匹配受控入口。
        /// Legacy module-definition data service, used only to create or match controlled entries.
        /// </summary>
        [Dependency]
        public IModuleDefsDb ModuleDefConfig { private get; set; }

        /// <summary>
        /// 旧模块实例数据服务，仅用于预检引用数量。
        /// Legacy module-instance data service, used only to preflight reference counts.
        /// </summary>
        [Dependency]
        public IModulesDb ModulesConfig { private get; set; }

        /// <summary>
        /// 初始化已验证部署包目录。
        /// Initializes the validated deployment-package catalog.
        /// </summary>
        /// <param name="sender">事件源。Event source.</param>
        /// <param name="e">事件参数。Event arguments.</param>
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
        /// 处理已验证模块包的受限目录操作。
        /// Handles restricted catalog actions for a validated module package.
        /// </summary>
        /// <param name="sender">事件源。Event source.</param>
        /// <param name="e">GridView 命令事件参数。GridView command event arguments.</param>
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
        /// 绑定已验证包及其数据库注册和实例引用摘要。
        /// Binds validated packages and their database registration and instance-reference summary.
        /// </summary>
        private void BindPackages()
        {
            IList<IModuleDefinitionItem> definitions = ModuleDefConfig.GetModuleDefinitions().ToList();
            var rows = new List<ModuleCatalogRow>();

            foreach (PortalModulePackage package in PortalModuleCatalog.GetTrustedPackages())
            {
                IModuleDefinitionItem definition = FindDefinition(definitions, package.DesktopEntry);
                PortalModulePackageStateReadResult stateResult = PortalModulePackageStates.Read(package.PackageId, Context);
                bool isEnabled = !stateResult.IsAvailable || stateResult.State == null || stateResult.State.IsEnabled;
                int instanceCount = definition == null
                    ? 0
                    : ModulesConfig.GetModulesByModuleDefId(definition.ModuleDefId).Count();

                rows.Add(new ModuleCatalogRow(
                    package,
                    isEnabled,
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
        /// 为已验证包创建仅指向 manifest 入口的旧定义记录。
        /// Creates a legacy definition record pointing only to a validated manifest entry.
        /// </summary>
        /// <remarks>
        /// 此操作写入旧模块定义表并写入运营审计，但不复制、上传、删除或修改包目录。重复入口仅返回既有定义，
        /// 不会创建第二条定义记录。
        /// This operation writes the legacy module-definition table and an operations audit, but never copies, uploads,
        /// deletes, or modifies the package directory. A duplicate entry returns the existing definition and does not
        /// create a second definition record.
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
        /// 保存已验证包的启用状态，并记录高价值运营审计。
        /// Saves a validated package enabled state and records a high-value operations audit.
        /// </summary>
        /// <remarks>
        /// 状态表不可用或写入失败时不改变模块文件，也不伪造成功结果；失败详情由状态存储写入诊断。
        /// When the state table is unavailable or a write fails, this method does not alter module files or fabricate a
        /// successful result; failure details are recorded in diagnostics by the state store.
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
        /// 展示包定义和实例引用数量，不执行删除、迁移或物理文件操作。
        /// Displays definition and instance reference counts without deleting, migrating, or touching physical files.
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
        /// 按已规范化桌面入口匹配旧模块定义。
        /// Matches a legacy module definition by normalized desktop entry.
        /// </summary>
        private static IModuleDefinitionItem FindDefinition(
            IEnumerable<IModuleDefinitionItem> definitions,
            string desktopEntry)
        {
            return definitions.FirstOrDefault(item =>
                string.Equals(item.DesktopSourceFile, desktopEntry, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 展示不含物理路径或异常详情的管理员安全提示。
        /// Displays an administrator-safe message without physical paths or exception details.
        /// </summary>
        private void ShowMessage(string message)
        {
            MessageLabel.Text = Server.HtmlEncode(message ?? string.Empty);
            ResultLabel.Text = string.Empty;
        }
    }

    /// <summary>
    /// 模块目录 GridView 的只读展示行。
    /// Read-only display row for the module-catalog GridView.
    /// </summary>
    public sealed class ModuleCatalogRow
    {
        internal ModuleCatalogRow(
            PortalModulePackage package,
            bool isEnabled,
            PortalModulePackageStateReadResult stateResult,
            IModuleDefinitionItem definition,
            int instanceCount)
        {
            PackageId = package.PackageId;
            DisplayName = package.DisplayName;
            Version = package.Version;
            DesktopEntry = package.DesktopEntry;
            IsEnabled = isEnabled;
            IsRegistered = definition != null;
            DefinitionText = definition == null
                ? "Not registered"
                : definition.ModuleDefId.ToString(CultureInfo.InvariantCulture);
            InstanceCount = instanceCount;
            StateText = !stateResult.IsAvailable
                ? "Enabled (state table unavailable)"
                : stateResult.State == null || !stateResult.State.IsConfigured
                    ? "Enabled (default)"
                    : isEnabled ? "Enabled" : "Disabled";
        }

        /// <summary>稳定部署包标识。Stable deployment-package identifier.</summary>
        public string PackageId { get; private set; }

        /// <summary>管理员展示名称。Administrator display name.</summary>
        public string DisplayName { get; private set; }

        /// <summary>部署包版本。Deployment-package version.</summary>
        public string Version { get; private set; }

        /// <summary>已验证桌面入口。Validated desktop entry.</summary>
        public string DesktopEntry { get; private set; }

        /// <summary>显示用启用状态。Display enabled state.</summary>
        public bool IsEnabled { get; private set; }

        /// <summary>是否已有旧定义记录。Whether a legacy definition exists.</summary>
        public bool IsRegistered { get; private set; }

        /// <summary>显示用状态文本。Display state text.</summary>
        public string StateText { get; private set; }

        /// <summary>显示用定义摘要。Display definition summary.</summary>
        public string DefinitionText { get; private set; }

        /// <summary>引用该定义的模块实例数量。Module-instance count referencing the definition.</summary>
        public int InstanceCount { get; private set; }
    }
}
