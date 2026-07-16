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
    /// 中文：旧门户 Tab 属性和模块布局管理页面。
    ///
    /// English: Legacy Portal page for Tab properties and module-layout administration.
    /// </summary>
    /// <remarks>
    /// 中文：当前页面只使用 <c>Admins</c> 角色，并以请求中的 Tab 标识和当前门户上下文共同确认目标。
    /// 既有核心模块定义继续可选；受信任部署包定义在 Disabled 时不可新增实例。
    ///
    /// English: The current page uses only the <c>Admins</c> role and confirms its target through both the requested
    /// Tab identifier and current Portal context. Existing core module definitions remain selectable; a trusted
    /// deployment-package definition cannot create a new instance while Disabled.
    /// </remarks>
    public partial class TabLayout : PortalPage<TabLayout>
    {
        private const string LeftPaneName = "LeftPane";
        private const string ContentPaneName = "ContentPane";
        private const string RightPaneName = "RightPane";

        protected List<ModuleSettings> contentList;
        protected List<ModuleSettings> leftList;
        protected List<ModuleSettings> rightList;

        private int tabId;
        private PortalSettings currentPortalSettings;
        private Tab currentTab;

        /// <summary>中文：角色数据访问依赖。English: Role data-access dependency.</summary>
        [Dependency]
        public IRolesDb RolesDB { private get; set; }

        /// <summary>中文：模块实例数据访问依赖。English: Module-instance data-access dependency.</summary>
        [Dependency]
        public IModulesDb ModulesConfig { private get; set; }

        /// <summary>中文：Tab 数据访问依赖。English: Tab data-access dependency.</summary>
        [Dependency]
        public ITabsDb TabsConfig { private get; set; }

        /// <summary>中文：模块定义数据访问依赖。English: Module-definition data-access dependency.</summary>
        [Dependency]
        public IModuleDefsDb ModuleDefConfig { private get; set; }

        /// <summary>中文：门户全局设置数据访问依赖。English: Portal global-settings data-access dependency.</summary>
        [Dependency]
        public IGlobalsDb PortalConfig { private get; set; }

        /// <summary>
        /// 中文：授权并验证当前 Tab，在首次请求加载布局数据。
        ///
        /// English: Authorizes and validates the current Tab, then loads layout data on the initial request.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
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
        /// 中文：向当前 Tab 的主内容窗格添加一个允许的新模块实例。
        ///
        /// English: Adds an allowed new module instance to the current Tab's content pane.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
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
        /// 中文：在同一窗格内调整所选模块的显示顺序。
        ///
        /// English: Adjusts the display order of the selected module inside the same pane.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：图像按钮事件数据。English: Image-button event data.</param>
        protected void UpDown_Click(object sender, ImageClickEventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            ImageButton button = sender as ImageButton;
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
        /// 中文：将所选模块移动到另一个允许的布局窗格。
        ///
        /// English: Moves the selected module to another allowed layout pane.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：图像按钮事件数据。English: Image-button event data.</param>
        protected void RightLeft_Click(object sender, ImageClickEventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            ImageButton button = sender as ImageButton;
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
        /// 中文：保存 Tab 设置并返回核心后台 Tab。
        ///
        /// English: Saves Tab settings and returns to the core administration Tab.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
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
        /// 中文：处理 Tab 名称、访问角色或移动端属性的自动保存事件。
        ///
        /// English: Handles auto-save events for Tab name, access roles, or mobile properties.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void TabSettings_Change(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            SaveTabData();
        }

        /// <summary>
        /// 中文：进入所选模块的实例设置页。
        ///
        /// English: Opens the instance-settings page for the selected module.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：图像按钮事件数据。English: Image-button event data.</param>
        protected void EditBtn_Click(object sender, ImageClickEventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            ImageButton button = sender as ImageButton;
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
        /// 中文：删除所选模块实例并重新整理该窗格顺序。
        ///
        /// English: Deletes the selected module instance and reorders the affected pane.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：图像按钮事件数据。English: Image-button event data.</param>
        protected void DeleteBtn_Click(object sender, ImageClickEventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            ImageButton button = sender as ImageButton;
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
