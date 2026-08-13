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
            // <lang>
            //   <zh-CN>目录页所有请求都先经过查看权限门禁，拒绝时不读取部署包或数据库。</zh-CN>
            //   <en>Every catalog request passes the view-permission gate before reading packages or databases.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.ModuleCatalogView))
            {
                return;
            }

            // <lang>
            //   <zh-CN>只在首次请求读取目录，回发由命令事件负责动作并避免覆盖控件状态。</zh-CN>
            //   <en>Read the catalog only on the initial request; postbacks let command events own actions without overwriting control state.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>预检只需要查看权限，注册/启停则要求编辑权限；命令名不能绕过这一区分。</zh-CN>
            //   <en>Preflight needs view permission while registration and state changes need edit permission; the command name cannot bypass that distinction.</en>
            // </lang>
            string permissionKey = string.Equals(e.CommandName, "Preflight", StringComparison.OrdinalIgnoreCase)
                ? PortalPermissionKeys.ModuleCatalogView
                : PortalPermissionKeys.ModuleCatalogEdit;
            if (!PortalAuthorization.EnsurePermission(Context, permissionKey))
            {
                return;
            }

            // <lang>
            //   <zh-CN>GridView 参数只作为包标识读取并按不变文化转换，后续仍由受信目录重新解析包。</zh-CN>
            //   <en>Read the GridView argument only as a package identifier using invariant conversion; the trusted catalog resolves the package again below.</en>
            // </lang>
            string packageId = Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture);
            // <lang>
            //   <zh-CN>服务返回的包和失败原因分别承载已部署包快照与低敏回退依据。</zh-CN>
            //   <en>The service outputs carry the deployed-package snapshot and the low-sensitivity fallback reason separately.</en>
            // </lang>
            PortalModulePackage package;
            string reason;
            if (!PortalModuleCatalog.TryGetTrustedPackage(packageId, out package, out reason))
            {
                ShowMessage("The selected module package is no longer deployed or is invalid.");
                BindPackages();
                return;
            }

            // <lang>
            //   <zh-CN>Profile 只约束会改变注册或启停状态的命令；预检允许查看被部署但当前 Profile 阻断的包。</zh-CN>
            //   <en>The Profile constrains commands that change registration or enabled state; preflight may inspect a deployed package blocked by the active Profile.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>命令分派保持原有受控动作集合，未知命令不触发任何写入；统一在动作后刷新只读目录。</zh-CN>
            //   <en>Dispatch only the existing controlled action set; unknown commands perform no write, and the read-only catalog is refreshed after the action.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>先读取旧定义快照，供后续按桌面入口匹配注册状态；列表页不把包目录直接当作数据库定义。</zh-CN>
            //   <en>Read the legacy-definition snapshot first so registration can be matched by desktop entry; the list never treats package files as database definitions.</en>
            // </lang>
            IList<IModuleDefinitionItem> definitions = ModuleDefConfig.GetModuleDefinitions().ToList();
            // <lang>
            //   <zh-CN>解析当前启动 Profile 一次，确保同一批展示行使用一致的允许/阻断判断。</zh-CN>
            //   <en>Resolve the active startup Profile once so every row in this bind uses the same allow/block decision.</en>
            // </lang>
            PortalModuleProfileSnapshot profile = PortalModuleProfileResolver.Resolve(Context);
            // <lang>
            //   <zh-CN>行集合只承载低敏展示投影，真正的包状态和定义读取仍由受控服务完成。</zh-CN>
            //   <en>The row collection carries only a low-sensitivity display projection; controlled services own package-state and definition reads.</en>
            // </lang>
            var rows = new List<ModuleCatalogRow>();

            foreach (PortalModulePackage package in PortalModuleCatalog.GetTrustedPackages())
            {
                // <lang>
                //   <zh-CN>每个包只从受信部署目录进入列表，循环不扫描用户可写路径。</zh-CN>
                //   <en>Each package enters from the trusted deployment catalog; the loop never scans user-writable paths.</en>
                // </lang>
                // <lang>
                //   <zh-CN>按已验证桌面入口匹配旧定义，避免仅凭显示名称误认注册状态。</zh-CN>
                //   <en>Match the legacy definition by validated desktop entry rather than by display name.</en>
                // </lang>
                IModuleDefinitionItem definition = FindDefinition(definitions, package.DesktopEntry);
                // <lang>
                //   <zh-CN>读取状态表的可用性与状态值，缺失时保留显式默认启用语义供展示层说明。</zh-CN>
                //   <en>Read state-table availability and state; when unavailable, preserve the explicit enabled-by-default meaning for display.</en>
                // </lang>
                PortalModulePackageStateReadResult stateResult = PortalModulePackageStates.Read(package.PackageId, Context);
                // <lang>
                //   <zh-CN>状态表不可用、没有记录或记录标记启用时，页面保持兼容的启用投影。</zh-CN>
                //   <en>Project the compatibility enabled state when the table is unavailable, no record exists, or the record says enabled.</en>
                // </lang>
                bool isEnabled = !stateResult.IsAvailable || stateResult.State == null || stateResult.State.IsEnabled;
                // <lang>
                //   <zh-CN>Profile 判断只影响当前门户能力集，不改变部署包本身和数据库状态。</zh-CN>
                //   <en>The Profile decision affects only the current Portal capability set, not the package files or database state.</en>
                // </lang>
                bool isProfileAllowed = profile.IsPackageAllowed(package.PackageId);
                // <lang>
                //   <zh-CN>引用计数只在有匹配定义时查询；未注册包显示零引用而不访问无效定义标识。</zh-CN>
                //   <en>Query reference count only for a matching definition; an unregistered package displays zero without using an invalid definition id.</en>
                // </lang>
                int instanceCount = definition == null
                    ? 0
                    : ModulesConfig.GetModulesByModuleDefId(definition.ModuleDefId).Count();

                // <lang>
                //   <zh-CN>构造器把包、Profile、状态和引用计数压缩为低敏展示行，不把服务对象暴露给标记层。</zh-CN>
                //   <en>Project package, Profile, state, and reference count into a low-sensitivity row instead of exposing service objects to markup.</en>
                // </lang>
                rows.Add(new ModuleCatalogRow(
                    package,
                    isEnabled,
                    isProfileAllowed,
                    profile.ActiveProfile,
                    stateResult,
                    definition,
                    instanceCount));
            }

            // <lang>
            //   <zh-CN>绑定完成后由页面统一决定空目录提示；空结果不会保留上一轮列表。</zh-CN>
            //   <en>After binding, the page owns the empty-catalog message; an empty result never leaves the previous list visible.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>注册前重新读取定义快照，避免页面展示过期时重复创建入口。</zh-CN>
            //   <en>Reload the definition snapshot before registration so a stale page cannot create a duplicate entry.</en>
            // </lang>
            IList<IModuleDefinitionItem> definitions = ModuleDefConfig.GetModuleDefinitions().ToList();
            // <lang>
            //   <zh-CN>按 manifest 桌面入口判断重复注册，名称变化不能产生第二条定义。</zh-CN>
            //   <en>Detect duplicate registration by the manifest desktop entry; a display-name change cannot create a second definition.</en>
            // </lang>
            IModuleDefinitionItem existing = FindDefinition(definitions, package.DesktopEntry);
            if (existing != null)
            {
                ResultLabel.Text = "The package entry is already registered as module definition " +
                                   existing.ModuleDefId.ToString(CultureInfo.InvariantCulture) + ".";
                return;
            }

            // <lang>
            //   <zh-CN>新增定义只使用受信包提供的展示名和入口，移动端入口保持空值兼容。</zh-CN>
            //   <en>Create the definition only from the trusted package display name and entry, retaining the empty mobile entry for compatibility.</en>
            // </lang>
            int definitionId = ModuleDefConfig.AddModuleDefinition(
                package.DisplayName,
                package.DesktopEntry,
                string.Empty);
            // <lang>
            //   <zh-CN>注册成功后记录固定类别和包标识，不把物理路径或连接细节写入审计正文。</zh-CN>
            //   <en>Record fixed audit category and package identity after success without writing physical paths or connection details into the audit text.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>状态存储统一处理 Profile 后的持久化、不可用回退和诊断；页面只传包标识、目标状态和当前上下文。</zh-CN>
            //   <en>The state store owns persistence, unavailable fallback, and diagnostics after Profile checks; the page passes only package identity, target state, and context.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>只有持久化成功才写启停审计并显示成功消息，避免把失败状态伪装成页面成功。</zh-CN>
            //   <en>Write the enable/disable audit and show success only after persistence succeeds, never disguising a failed state as page success.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>预检重新匹配旧定义，不复用可能已过期的 GridView 行状态。</zh-CN>
            //   <en>Preflight rematches the legacy definition instead of trusting possibly stale GridView row state.</en>
            // </lang>
            IModuleDefinitionItem definition = FindDefinition(
                ModuleDefConfig.GetModuleDefinitions().ToList(),
                package.DesktopEntry);
            if (definition == null)
            {
                ResultLabel.Text = "The package has no registered legacy module definition.";
                return;
            }

            // <lang>
            //   <zh-CN>引用计数只用于阻止危险删除提示，预检本身不写入定义、实例或物理目录。</zh-CN>
            //   <en>The reference count only informs the dangerous-delete warning; preflight itself writes neither definitions, instances, nor physical directories.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>使用不区分大小写的已规范化入口匹配，保持部署包 manifest 与旧定义的兼容关系。</zh-CN>
            //   <en>Match normalized entries case-insensitively to preserve compatibility between package manifests and legacy definitions.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>提示只进入编码后的文本控件，清空旧结果以避免误读上一次命令状态。</zh-CN>
            //   <en>Send only encoded text to the message control and clear the old result so a prior command state is not misread.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>以下字段只复制受控包和服务投影，避免标记层持有可变数据访问对象。</zh-CN>
            //   <en>The fields below copy only controlled package and service projections so markup never holds mutable data-access objects.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>Profile 文本与状态文本分别说明能力集阻断和状态表默认值，避免把两种原因合并。</zh-CN>
            //   <en>Keep Profile text and state text separate so capability blocking is not confused with a state-table default.</en>
            // </lang>
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
