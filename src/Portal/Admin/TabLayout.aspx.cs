using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>旧门户 Tab 属性和模块布局管理页面。</zh-CN>
    ///   <en>Legacy Portal page for Tab properties and module-layout administration.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>当前页面只使用 <c>Admins</c> 角色，并以请求中的 Tab 标识和当前门户上下文共同确认目标。 既有核心模块定义继续可选；受信任部署包定义在 Disabled 时不可新增实例。</zh-CN>
    ///   <en>The current page uses only the <c>Admins</c> role and confirms its target through both the requested Tab identifier and current Portal context. Existing core module definitions remain selectable; a trusted deployment-package definition cannot create a new instance while Disabled.</en>
    /// </lang>
    /// </remarks>
    public partial class TabLayout : PortalPage<TabLayout>
    {
        private const string LeftPaneName = "LeftPane";
        private const string ContentPaneName = "ContentPane";
        private const string RightPaneName = "RightPane";

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定到内容主栏的模块列表，供 WebForms 标记层直接枚举。</zh-CN>
        ///   <en>Module list bound to the main content pane and enumerated directly by the WebForms markup.</en>
        /// </lang>
        /// </summary>
        protected List<ModuleSettings> contentList;

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定到左侧栏的模块列表，保持旧页面三栏布局编辑语义。</zh-CN>
        ///   <en>Module list bound to the left pane, preserving the legacy page's three-column layout-editing semantics.</en>
        /// </lang>
        /// </summary>
        protected List<ModuleSettings> leftList;

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定到右侧栏的模块列表，和排序/移动操作共同维护 Tab 布局状态。</zh-CN>
        ///   <en>Module list bound to the right pane and maintained together with ordering and move operations.</en>
        /// </lang>
        /// </summary>
        protected List<ModuleSettings> rightList;

        private int tabId;
        private PortalSettings currentPortalSettings;
        private Tab currentTab;

        /// <summary>
        /// <lang>
        ///   <zh-CN>角色数据访问依赖。</zh-CN>
        ///   <en>Role data-access dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IRolesDb RolesDB { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>模块实例数据访问依赖。</zh-CN>
        ///   <en>Module-instance data-access dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IModulesDb ModulesConfig { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>Tab 数据访问依赖。</zh-CN>
        ///   <en>Tab data-access dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public ITabsDb TabsConfig { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>模块定义数据访问依赖。</zh-CN>
        ///   <en>Module-definition data-access dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IModuleDefsDb ModuleDefConfig { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>门户全局设置数据访问依赖。</zh-CN>
        ///   <en>Portal global-settings data-access dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IGlobalsDb PortalConfig { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>授权并验证当前 Tab，在首次请求加载布局数据。</zh-CN>
        ///   <en>Authorizes and validates the current Tab, then loads layout data on the initial request.</en>
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
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                BindData();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>向当前 Tab 的主内容窗格添加一个允许的新模块实例。</zh-CN>
        ///   <en>Adds an allowed new module instance to the current Tab's content pane.</en>
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
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void AddModuleToPane_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            int moduleDefinitionId;
            string title;
            if (moduleType.SelectedItem == null ||
                !PortalNavigationPolicy.TryReadPositiveInt32(moduleType.SelectedItem.Value, out moduleDefinitionId) ||
                FindEligibleModuleDefinition(moduleDefinitionId) == null)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            if (!PortalAdministrationPolicy.TryNormalizeRequiredSingleLineText(moduleTitle.Text, 150, out title))
            {
                ShowMessage("模块名称无效，未创建模块。");
                return;
            }

            try
            {
                int moduleId = ModulesConfig.AddModule(
                    tabId,
                    999,
                    ContentPaneName,
                    title,
                    moduleDefinitionId,
                    0,
                    PortalRoleNames.Administrators,
                    false);
                ReloadCurrentTab();
                OrderModules(GetModules(ContentPaneName));
                PortalOperationAudit.Record(
                    "ModuleAdministration",
                    "Create",
                    "Module",
                    moduleId.ToString(),
                    "Created module instance in the content pane.",
                    Context);
                RedirectToCurrentLayout();
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.TabLayout.AddModule",
                    "Adding a module instance failed. TabId=" + tabId,
                    exception,
                    Context);
                ShowMessage("模块创建失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>在同一窗格内调整所选模块的显示顺序。</zh-CN>
        ///   <en>Adjusts the display order of the selected module inside the same pane.</en>
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
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void UpDown_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            LinkButton button = sender as LinkButton;
            string pane;
            ListBox listBox;
            if (button == null || !TryGetPaneListBox(button.CommandArgument, out pane, out listBox) ||
                (button.CommandName != "up" && button.CommandName != "down"))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            List<ModuleSettings> modules = GetModules(pane);
            ModuleSettings selectedModule;
            if (!TryGetSelectedModule(listBox, modules, out selectedModule))
            {
                RedirectToCurrentLayout();
                return;
            }

            selectedModule.ModuleOrder += button.CommandName == "down" ? 3 : -3;
            try
            {
                OrderModules(modules);
                PortalOperationAudit.Record(
                    "ModuleAdministration",
                    "Order",
                    "Module",
                    selectedModule.ModuleId.ToString(),
                    "Changed module order within a pane.",
                    Context);
                RedirectToCurrentLayout();
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.TabLayout.OrderModule",
                    "Ordering a module failed. TabId=" + tabId + "; ModuleId=" + selectedModule.ModuleId,
                    exception,
                    Context);
                ShowMessage("模块排序失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将所选模块移动到另一个允许的布局窗格。</zh-CN>
        ///   <en>Moves the selected module to another allowed layout pane.</en>
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
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void RightLeft_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            LinkButton button = sender as LinkButton;
            string sourcePane;
            string targetPane;
            ListBox sourceBox;
            ListBox ignoredTargetBox;
            if (button == null ||
                !TryGetPaneListBox(button.Attributes["sourcepane"], out sourcePane, out sourceBox) ||
                !TryGetPaneListBox(button.Attributes["targetpane"], out targetPane, out ignoredTargetBox) ||
                string.Equals(sourcePane, targetPane, StringComparison.Ordinal))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            List<ModuleSettings> sourceModules = GetModules(sourcePane);
            ModuleSettings selectedModule;
            if (!TryGetSelectedModule(sourceBox, sourceModules, out selectedModule))
            {
                RedirectToCurrentLayout();
                return;
            }

            try
            {
                ModulesConfig.UpdateModuleOrder(selectedModule.ModuleId, 998, targetPane);
                ReloadCurrentTab();
                OrderModules(GetModules(sourcePane));
                OrderModules(GetModules(targetPane));
                PortalOperationAudit.Record(
                    "ModuleAdministration",
                    "Move",
                    "Module",
                    selectedModule.ModuleId.ToString(),
                    "Moved module between layout panes.",
                    Context);
                RedirectToCurrentLayout();
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.TabLayout.MoveModule",
                    "Moving a module failed. TabId=" + tabId + "; ModuleId=" + selectedModule.ModuleId,
                    exception,
                    Context);
                ShowMessage("模块移动失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>保存 Tab 设置并返回核心后台 Tab。</zh-CN>
        ///   <en>Saves Tab settings and returns to the core administration Tab.</en>
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
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void Apply_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest() || !SaveTabData())
            {
                return;
            }

            ITabItem administrationTab = currentPortalSettings.DesktopTabs.FirstOrDefault(tab =>
                PortalAdministrationPolicy.IsProtectedAdministrationTabName(tab.TabName));
            if (administrationTab == null)
            {
                PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ResolveUrl("~/DesktopDefault.aspx"));
                return;
            }

            int administrationIndex = currentPortalSettings.DesktopTabs.IndexOf(administrationTab);
            PortalNavigationPolicy.RedirectToSafeReturnUrl(
                Context,
                ResolveUrl("~/DesktopDefault.aspx?tabindex=" + administrationIndex + "&tabid=" + administrationTab.TabId));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理 Tab 名称、访问角色或移动端属性的自动保存事件。</zh-CN>
        ///   <en>Handles auto-save events for Tab name, access roles, or mobile properties.</en>
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
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void TabSettings_Change(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            SaveTabData();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>进入所选模块的实例设置页。</zh-CN>
        ///   <en>Opens the instance-settings page for the selected module.</en>
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
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void EditBtn_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            LinkButton button = sender as LinkButton;
            string pane;
            ListBox listBox;
            ModuleSettings selectedModule;
            if (button == null || !TryGetPaneListBox(button.CommandArgument, out pane, out listBox) ||
                !TryGetSelectedModule(listBox, GetModules(pane), out selectedModule))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(
                Context,
                ResolveUrl("~/Admin/ModuleSettings.aspx?mid=" + selectedModule.ModuleId + "&tabid=" + tabId));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除所选模块实例并重新整理该窗格顺序。</zh-CN>
        ///   <en>Deletes the selected module instance and reorders the affected pane.</en>
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
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void DeleteBtn_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            LinkButton button = sender as LinkButton;
            string pane;
            ListBox listBox;
            ModuleSettings selectedModule;
            if (button == null || !TryGetPaneListBox(button.CommandArgument, out pane, out listBox) ||
                !TryGetSelectedModule(listBox, GetModules(pane), out selectedModule))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            try
            {
                ModulesConfig.DeleteModule(selectedModule.ModuleId);
                ReloadCurrentTab();
                OrderModules(GetModules(pane));
                PortalOperationAudit.Record(
                    "ModuleAdministration",
                    "Delete",
                    "Module",
                    selectedModule.ModuleId.ToString(),
                    "Deleted module instance from a Tab layout.",
                    Context);
                RedirectToCurrentLayout();
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.TabLayout.DeleteModule",
                    "Deleting a module failed. TabId=" + tabId + "; ModuleId=" + selectedModule.ModuleId,
                    exception,
                    Context);
                ShowMessage("模块删除失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        private bool TryInitializeRequest()
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.PortalModulesEdit))
            {
                return false;
            }

            if (!PortalNavigationPolicy.TryReadPositiveInt32(Request.Params["tabid"], out tabId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            currentPortalSettings = PortalContext.GetPortalSettings();
            ITabItem requestedTab = currentPortalSettings.DesktopTabs.FirstOrDefault(tab => tab.TabId == tabId);
            currentTab = requestedTab == null || currentPortalSettings.ActiveTab == null ||
                         currentPortalSettings.ActiveTab.TabId != tabId
                ? null
                : currentPortalSettings.ActiveTab;
            if (currentTab != null)
            {
                return true;
            }

            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        private bool SaveTabData()
        {
            string normalizedTabName;
            string normalizedMobileTabName;
            if (!PortalAdministrationPolicy.TryNormalizeRequiredSingleLineText(tabName.Text, 150, out normalizedTabName) ||
                !PortalAdministrationPolicy.TryNormalizeOptionalSingleLineText(mobileTabName.Text, 150, out normalizedMobileTabName))
            {
                ShowMessage("Tab 名称无效，未保存本次修改。");
                return false;
            }

            if (PortalAdministrationPolicy.IsProtectedAdministrationTabName(currentTab.TabName) &&
                !string.Equals(currentTab.TabName, normalizedTabName, StringComparison.Ordinal))
            {
                ShowMessage("核心后台 Tab 不能改名。");
                return false;
            }

            string authorizedRoles = PortalRoleParser.Join(
                authRoles.Items.Cast<ListItem>()
                    .Where(item => item.Selected)
                    .Select(item => item.Text));
            try
            {
                TabsConfig.UpdateTab(
                    currentPortalSettings.PortalId,
                    tabId,
                    normalizedTabName,
                    currentTab.TabOrder,
                    authorizedRoles,
                    normalizedMobileTabName,
                    showMobile.Checked);
                PortalOperationAudit.Record(
                    "TabAdministration",
                    "Update",
                    "Tab",
                    tabId.ToString(),
                    "Updated Tab settings.",
                    Context);
                return true;
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.TabLayout.SaveTab",
                    "Saving Tab settings failed. TabId=" + tabId,
                    exception,
                    Context);
                ShowMessage("Tab 设置保存失败，系统已记录本次错误。事件编号：" + eventId);
                return false;
            }
        }

        private void BindData()
        {
            tabName.Text = currentTab.TabName;
            mobileTabName.Text = currentTab.MobileTabName;
            showMobile.Checked = currentTab.ShowMobile;

            authRoles.Items.Clear();
            var allUsers = new ListItem(PortalRoleNames.AllUsers, PortalRoleNames.AllUsers)
            {
                Selected = PortalRoleParser.Contains(currentTab.AuthorizedRoles, PortalRoleNames.AllUsers)
            };
            authRoles.Items.Add(allUsers);
            foreach (IRoleItem role in RolesDB.GetPortalRoles(currentPortalSettings.PortalId))
            {
                var item = new ListItem(role.RoleName, role.RoleId.ToString())
                {
                    Selected = PortalRoleParser.Contains(currentTab.AuthorizedRoles, role.RoleName)
                };
                authRoles.Items.Add(item);
            }

            moduleType.DataSource = GetEligibleModuleDefinitions();
            moduleType.DataBind();

            rightList = GetModules(RightPaneName);
            rightPane.DataBind();
            contentList = GetModules(ContentPaneName);
            contentPane.DataBind();
            leftList = GetModules(LeftPaneName);
            leftPane.DataBind();
        }

        private IList<IModuleDefinitionItem> GetEligibleModuleDefinitions()
        {
            IList<PortalModulePackage> trustedPackages = PortalModuleCatalog.GetTrustedPackages();
            var eligibleDefinitions = new List<IModuleDefinitionItem>();
            foreach (IModuleDefinitionItem definition in ModuleDefConfig.GetModuleDefinitions())
            {
                string normalizedSource;
                string errorMessage;
                if (!PortalModulePathValidator.TryNormalizeDesktopSource(
                    definition.DesktopSourceFile,
                    out normalizedSource,
                    out errorMessage))
                {
                    continue;
                }

                PortalModulePackage package = trustedPackages.FirstOrDefault(item =>
                    string.Equals(item.DesktopEntry, normalizedSource, StringComparison.OrdinalIgnoreCase));
                if (package != null)
                {
                    PortalModulePackageStateReadResult state = PortalModulePackageStates.Read(package.PackageId, Context);
                    if (state.IsAvailable && state.State != null && !state.State.IsEnabled)
                    {
                        continue;
                    }
                }

                eligibleDefinitions.Add(definition);
            }

            return eligibleDefinitions;
        }

        private IModuleDefinitionItem FindEligibleModuleDefinition(int moduleDefinitionId)
        {
            return GetEligibleModuleDefinitions().FirstOrDefault(item => item.ModuleDefId == moduleDefinitionId);
        }

        private List<ModuleSettings> GetModules(string pane)
        {
            return currentTab.Modules
                .Where(module => string.Equals(module.PaneName, pane, StringComparison.OrdinalIgnoreCase))
                .OrderBy(module => module.ModuleOrder)
                .ThenBy(module => module.ModuleId)
                .ToList();
        }

        private bool TryGetPaneListBox(string candidate, out string pane, out ListBox listBox)
        {
            pane = NormalizePaneName(candidate);
            switch (pane)
            {
                case LeftPaneName:
                    listBox = leftPane;
                    return true;
                case ContentPaneName:
                    listBox = contentPane;
                    return true;
                case RightPaneName:
                    listBox = rightPane;
                    return true;
                default:
                    listBox = null;
                    return false;
            }
        }

        private bool TryGetSelectedModule(ListBox listBox, IList<ModuleSettings> modules, out ModuleSettings selectedModule)
        {
            selectedModule = null;
            if (listBox == null || modules == null || listBox.SelectedIndex < 0 ||
                listBox.SelectedIndex >= modules.Count || listBox.SelectedItem == null)
            {
                return false;
            }

            int selectedModuleId;
            if (!PortalNavigationPolicy.TryReadPositiveInt32(listBox.SelectedItem.Value, out selectedModuleId))
            {
                return false;
            }

            ModuleSettings candidate = modules[listBox.SelectedIndex];
            if (candidate.ModuleId != selectedModuleId)
            {
                return false;
            }

            selectedModule = candidate;
            return true;
        }

        private void OrderModules(List<ModuleSettings> modules)
        {
            modules.Sort();
            int order = 1;
            foreach (ModuleSettings module in modules)
            {
                module.ModuleOrder = order;
                order += 2;
                ModulesConfig.UpdateModuleOrder(module.ModuleId, module.ModuleOrder, module.PaneName);
            }
        }

        private void ReloadCurrentTab()
        {
            int tabIndex = currentPortalSettings.DesktopTabs.FindIndex(tab => tab.TabId == tabId);
            PortalContext.SetPortalSettings(new PortalSettings(
                tabIndex,
                tabId,
                PortalConfig,
                TabsConfig,
                ModulesConfig,
                ModuleDefConfig));
            currentPortalSettings = PortalContext.GetPortalSettings();
            currentTab = currentPortalSettings.ActiveTab;
        }

        private void RedirectToCurrentLayout()
        {
            PortalNavigationPolicy.RedirectToSafeReturnUrl(
                Context,
                ResolveUrl("~/Admin/TabLayout.aspx?tabid=" + tabId));
        }

        private void ShowMessage(string message)
        {
            Message.Text = Server.HtmlEncode(message ?? string.Empty);
        }

        private static string NormalizePaneName(string pane)
        {
            if (string.Equals(pane, "leftPane", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pane, LeftPaneName, StringComparison.OrdinalIgnoreCase))
            {
                return LeftPaneName;
            }

            if (string.Equals(pane, "contentPane", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pane, ContentPaneName, StringComparison.OrdinalIgnoreCase))
            {
                return ContentPaneName;
            }

            if (string.Equals(pane, "rightPane", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pane, RightPaneName, StringComparison.OrdinalIgnoreCase))
            {
                return RightPaneName;
            }

            return string.Empty;
        }
    }
}
