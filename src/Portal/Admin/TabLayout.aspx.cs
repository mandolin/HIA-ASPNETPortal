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
    ///   <zh-CN>当前页面只使用 <c>Admins</c> 角色，并以请求中的 Tab 标识和当前门户上下文共同确认目标。既有核心模块定义继续可选；不在当前启动期 Profile 或被包状态禁用的定义不可新增实例。</zh-CN>
    ///   <en>The current page uses only the <c>Admins</c> role and confirms its target through both the requested Tab identifier and current Portal context. Existing core module definitions remain selectable; a definition outside the current startup Profile or disabled package state cannot create a new instance.</en>
    /// </lang>
    /// </remarks>
    public partial class TabLayout : PortalPage<TabLayout>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>左侧布局窗格的规范名称。</zh-CN>
        ///   <en>Canonical name of the left layout pane.</en>
        /// </lang>
        /// </summary>
        private const string LeftPaneName = "LeftPane";

        /// <summary>
        /// <lang>
        ///   <zh-CN>主内容布局窗格的规范名称。</zh-CN>
        ///   <en>Canonical name of the main content layout pane.</en>
        /// </lang>
        /// </summary>
        private const string ContentPaneName = "ContentPane";

        /// <summary>
        /// <lang>
        ///   <zh-CN>右侧布局窗格的规范名称。</zh-CN>
        ///   <en>Canonical name of the right layout pane.</en>
        /// </lang>
        /// </summary>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>通过权限、导航和当前门户上下文校验的 Tab 标识。</zh-CN>
        ///   <en>The Tab identifier verified by permission, navigation, and current-Portal context.</en>
        /// </lang>
        /// </summary>
        private int tabId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前请求使用的门户设置快照。</zh-CN>
        ///   <en>The Portal-settings snapshot used by the current request.</en>
        /// </lang>
        /// </summary>
        private PortalSettings currentPortalSettings;

        /// <summary>
        /// <lang>
        ///   <zh-CN>已通过 ActiveTab 归属校验的当前 Tab 快照。</zh-CN>
        ///   <en>The current Tab snapshot after ActiveTab ownership validation.</en>
        /// </lang>
        /// </summary>
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
            // <lang>
            //   <zh-CN>每次请求先验证模块编辑权限和 Tab 归属，避免未验证上下文进入布局读写。</zh-CN>
            //   <en>Validate module-edit permission and Tab ownership on every request before layout reads or writes use the context.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            // <lang>
            //   <zh-CN>首次请求绑定 Tab 设置、角色和窗格列表；回发保留用户当前选择。</zh-CN>
            //   <en>Bind Tab settings, roles, and pane lists only on the initial request, preserving postback selections.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>新增事件重新解析请求和 Tab 快照，防止陈旧页面状态绕过布局门禁。</zh-CN>
            //   <en>Re-resolve the request and Tab snapshot for creation so stale page state cannot bypass the layout gate.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            // <lang>
            //   <zh-CN>模块定义标识和标题是新增的两个受控输入；定义必须来自当前可用白名单。</zh-CN>
            //   <en>The definition id and title are the two controlled creation inputs; the definition must come from the current eligible allowlist.</en>
            // </lang>
            int moduleDefinitionId;
            string title;
            if (moduleType.SelectedItem == null ||
                !PortalNavigationPolicy.TryReadPositiveInt32(moduleType.SelectedItem.Value, out moduleDefinitionId) ||
                FindEligibleModuleDefinition(moduleDefinitionId) == null)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            // <lang>
            //   <zh-CN>标题先做单行和长度归一化，失败时不调用模块数据服务。</zh-CN>
            //   <en>Normalize the title as a bounded single line before calling the module data service.</en>
            // </lang>
            if (!PortalAdministrationPolicy.TryNormalizeRequiredSingleLineText(moduleTitle.Text, 150, out title))
            {
                ShowMessage("模块名称无效，未创建模块。");
                return;
            }

            try
            {
                // <lang>
                //   <zh-CN>以固定内容窗格、顺序、角色和移动端默认值创建模块，再重载并重新排序当前 Tab。</zh-CN>
                //   <en>Create the module with fixed pane, order, role, and mobile defaults, then reload and reorder the current Tab.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>只有创建、重载和排序完成后才记录成功审计并回到当前布局。</zh-CN>
                //   <en>Record success and return to the current layout only after creation, reload, and ordering complete.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>新增失败写入统一诊断并向页面返回事件编号，不泄漏数据层异常。</zh-CN>
                //   <en>Record creation failures through shared diagnostics and return only an event id without exposing data-layer exceptions.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>排序事件重做权限和上下文检查，避免跨 Tab 操作模块顺序。</zh-CN>
            //   <en>Repeat authorization and context checks for ordering so module order cannot be changed across Tabs.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            // <lang>
            //   <zh-CN>按钮命令只允许 up/down，窗格名称通过规范化 helper 映射到受控控件。</zh-CN>
            //   <en>Allow only up/down commands and map the pane name to a controlled list box through the normalization helper.</en>
            // </lang>
            LinkButton button = sender as LinkButton;
            string pane;
            ListBox listBox;
            if (button == null || !TryGetPaneListBox(button.CommandArgument, out pane, out listBox) ||
                (button.CommandName != "up" && button.CommandName != "down"))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            // <lang>
            //   <zh-CN>从当前窗格按稳定顺序读取模块，并验证列表选中项与数据键一致。</zh-CN>
            //   <en>Read modules from the current pane in stable order and verify that the selected list item matches its data key.</en>
            // </lang>
            List<ModuleSettings> modules = GetModules(pane);
            ModuleSettings selectedModule;
            // <lang>
            //   <zh-CN>没有有效选中模块时只回到当前布局，不写入任何排序变化。</zh-CN>
            //   <en>Return to the current layout without writing when no valid module is selected.</en>
            // </lang>
            if (!TryGetSelectedModule(listBox, modules, out selectedModule))
            {
                RedirectToCurrentLayout();
                return;
            }

            // <lang>
            //   <zh-CN>用有间隔的临时顺序表达方向，随后由 OrderModules 统一归一化。</zh-CN>
            //   <en>Use a spaced temporary order to express direction, then normalize all orders through OrderModules.</en>
            // </lang>
            selectedModule.ModuleOrder += button.CommandName == "down" ? 3 : -3;
            try
            {
                // <lang>
                //   <zh-CN>排序 helper 负责持久化窗格内完整顺序；成功后记录单个模块审计并安全回跳。</zh-CN>
                //   <en>The ordering helper persists the complete pane order; on success audit the module and navigate back safely.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>排序失败沿用统一诊断和低敏提示。</zh-CN>
                //   <en>Ordering failures use shared diagnostics and low-sensitivity feedback.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>移动事件重新验证当前 Tab，并要求来源和目标窗格均为不同的受控名称。</zh-CN>
            //   <en>Revalidate the current Tab and require distinct controlled source and target pane names for a move.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            // <lang>
            //   <zh-CN>按钮属性只用于选择受控窗格控件；目标控件仅验证存在，不直接信任请求文本。</zh-CN>
            //   <en>Button attributes select controlled pane controls; the target control is validated for existence rather than trusted as request text.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>在来源窗格中校验选中模块，空选中仅回跳而不写入移动。</zh-CN>
            //   <en>Validate the selected module in the source pane; an empty selection only navigates back without a move.</en>
            // </lang>
            List<ModuleSettings> sourceModules = GetModules(sourcePane);
            ModuleSettings selectedModule;
            if (!TryGetSelectedModule(sourceBox, sourceModules, out selectedModule))
            {
                RedirectToCurrentLayout();
                return;
            }

            try
            {
                // <lang>
                //   <zh-CN>先更新目标窗格，再重载并分别整理来源和目标顺序，最后审计并回跳。</zh-CN>
                //   <en>Update the target pane first, reload and reorder both panes, then audit and navigate back.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>移动失败写入统一诊断并保持页面低敏反馈。</zh-CN>
                //   <en>Record move failures through shared diagnostics and keep page feedback low sensitivity.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>应用按钮先验证并保存 Tab 设置，再定位受保护后台 Tab 作为回跳目标。</zh-CN>
            //   <en>The apply button validates and saves Tab settings before locating the protected administration Tab for return navigation.</en>
            // </lang>
            if (!TryInitializeRequest() || !SaveTabData())
            {
                return;
            }

            // <lang>
            //   <zh-CN>后台 Tab 由受保护名称策略识别，不从请求参数直接指定。</zh-CN>
            //   <en>Identify the administration Tab through the protected-name policy rather than taking it from request parameters.</en>
            // </lang>
            ITabItem administrationTab = currentPortalSettings.DesktopTabs.FirstOrDefault(tab =>
                PortalAdministrationPolicy.IsProtectedAdministrationTabName(tab.TabName));
            if (administrationTab == null)
            {
                PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ResolveUrl("~/DesktopDefault.aspx"));
                return;
            }

            // <lang>
            //   <zh-CN>用当前桌面 Tab 集合计算稳定索引并构造站内回跳地址。</zh-CN>
            //   <en>Compute the stable index from the current desktop-tab set and build an in-site return URL.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>自动保存事件仍需完整请求门禁，保存结果由 SaveTabData 统一处理。</zh-CN>
            //   <en>Auto-save events still require the full request gate, with SaveTabData owning the save result.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>编辑事件只把已验证模块标识带入实例设置页，并保留当前 Tab 上下文。</zh-CN>
            //   <en>The edit event forwards only the verified module id to the instance-settings page with the current Tab context.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            // <lang>
            //   <zh-CN>按钮窗格和列表选中项必须同时有效，否则拒绝构造编辑地址。</zh-CN>
            //   <en>Both the button pane and selected list item must be valid before constructing the edit URL.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>删除事件重新验证权限、Tab、窗格和选中模块，避免删除陈旧或跨 Tab 对象。</zh-CN>
            //   <en>Revalidate permission, Tab, pane, and selected module before deletion to block stale or cross-Tab objects.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            // <lang>
            //   <zh-CN>删除前使用当前窗格读模型确认目标，失败时不触碰数据服务。</zh-CN>
            //   <en>Confirm the target from the current pane read model before touching the data service.</en>
            // </lang>
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
                // <lang>
                //   <zh-CN>删除成功后重载当前 Tab、整理剩余顺序、写入审计并安全回跳。</zh-CN>
                //   <en>After successful deletion, reload the Tab, reorder remaining modules, audit the action, and navigate back safely.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>删除失败写入统一诊断并只显示事件编号。</zh-CN>
                //   <en>Record delete failures through shared diagnostics and display only the event id.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>模块编辑权限是布局读取、创建、排序、移动、编辑和删除的共同门禁。</zh-CN>
            //   <en>The module-edit permission gates layout reads, creation, ordering, moving, editing, and deletion.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.PortalModulesEdit))
            {
                return false;
            }

            // <lang>
            //   <zh-CN>Tab 标识必须是正整数；非法请求统一进入编辑拒绝页。</zh-CN>
            //   <en>The Tab id must be a positive integer; invalid requests use the common edit-denied path.</en>
            // </lang>
            if (!PortalNavigationPolicy.TryReadPositiveInt32(Request.Params["tabid"], out tabId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            // <lang>
            //   <zh-CN>门户设置、请求 Tab 和 ActiveTab 必须来自同一上下文，阻断跨 Tab 构造的操作。</zh-CN>
            //   <en>Portal settings, requested Tab, and ActiveTab must share one context to block cross-Tab crafted operations.</en>
            // </lang>
            currentPortalSettings = PortalContext.GetPortalSettings();
            // <lang>
            //   <zh-CN>仅从当前桌面 Tab 集合匹配请求标识，不把任意数字视为有效 Tab。</zh-CN>
            //   <en>Match the request id only within the current desktop-tab set instead of treating any number as a valid Tab.</en>
            // </lang>
            ITabItem requestedTab = currentPortalSettings.DesktopTabs.FirstOrDefault(tab => tab.TabId == tabId);
            currentTab = requestedTab == null || currentPortalSettings.ActiveTab == null ||
                         currentPortalSettings.ActiveTab.TabId != tabId
                ? null
                : currentPortalSettings.ActiveTab;
            if (currentTab != null)
            {
                return true;
            }

            // <lang>
            //   <zh-CN>未找到当前 ActiveTab 时拒绝后续布局操作，避免使用陈旧门户快照。</zh-CN>
            //   <en>Reject layout operations when the current ActiveTab is missing, avoiding stale Portal snapshots.</en>
            // </lang>
            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        private bool SaveTabData()
        {
            // <lang>
            //   <zh-CN>保存 helper 只处理已通过 TryInitializeRequest 的当前 Tab。</zh-CN>
            //   <en>The save helper operates only on the current Tab already validated by TryInitializeRequest.</en>
            // </lang>
            // <lang>
            //   <zh-CN>名称输入分别约束必填 Tab 名称和可选移动端名称。</zh-CN>
            //   <en>Normalize the required Tab name and optional mobile name with their respective input contracts.</en>
            // </lang>
            string normalizedTabName;
            string normalizedMobileTabName;
            if (!PortalAdministrationPolicy.TryNormalizeRequiredSingleLineText(tabName.Text, 150, out normalizedTabName) ||
                !PortalAdministrationPolicy.TryNormalizeOptionalSingleLineText(mobileTabName.Text, 150, out normalizedMobileTabName))
            {
                ShowMessage("Tab 名称无效，未保存本次修改。");
                return false;
            }

            // <lang>
            //   <zh-CN>受保护后台 Tab 不允许被重命名，避免破坏管理入口识别策略。</zh-CN>
            //   <en>Protected administration Tabs cannot be renamed, preserving the administration-entry recognition policy.</en>
            // </lang>
            if (PortalAdministrationPolicy.IsProtectedAdministrationTabName(currentTab.TabName) &&
                !string.Equals(currentTab.TabName, normalizedTabName, StringComparison.Ordinal))
            {
                ShowMessage("核心后台 Tab 不能改名。");
                return false;
            }

            // <lang>
            //   <zh-CN>角色列表由控件选中项生成旧格式串，角色解析器负责分隔符和去重语义。</zh-CN>
            //   <en>Build the legacy role string from selected controls while the role parser owns delimiters and deduplication semantics.</en>
            // </lang>
            string authorizedRoles = PortalRoleParser.Join(
                authRoles.Items.Cast<ListItem>()
                    .Where(item => item.Selected)
                    .Select(item => item.Text));
            try
            {
                // <lang>
                //   <zh-CN>只更新当前门户和当前 Tab 的允许字段，成功后记录 Tab 管理审计。</zh-CN>
                //   <en>Update only allowed fields for the current Portal and Tab, then record the Tab-administration audit on success.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>保存异常写入诊断并通过事件编号返回低敏错误。</zh-CN>
                //   <en>Record save failures in diagnostics and return a low-sensitivity error with an event id.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>从已验证 Tab 快照投影名称和移动属性，随后重建角色与模块选择。</zh-CN>
            //   <en>Project names and mobile settings from the verified Tab snapshot, then rebuild role and module choices.</en>
            // </lang>
            tabName.Text = currentTab.TabName;
            mobileTabName.Text = currentTab.MobileTabName;
            showMobile.Checked = currentTab.ShowMobile;

            // <lang>
            //   <zh-CN>清理旧角色选项，保留 All Users 的兼容公开语义。</zh-CN>
            //   <en>Clear old role options while preserving the compatibility public meaning of All Users.</en>
            // </lang>
            authRoles.Items.Clear();
            var allUsers = new ListItem(PortalRoleNames.AllUsers, PortalRoleNames.AllUsers)
            {
                Selected = PortalRoleParser.Contains(currentTab.AuthorizedRoles, PortalRoleNames.AllUsers)
            };
            authRoles.Items.Add(allUsers);
            // <lang>
            //   <zh-CN>把当前门户角色投影为可选项，并按已授权角色名恢复选中状态。</zh-CN>
            //   <en>Project current-Portal roles as options and restore selection by authorized role name.</en>
            // </lang>
            foreach (IRoleItem role in RolesDB.GetPortalRoles(currentPortalSettings.PortalId))
            {
                // <lang>
                //   <zh-CN>每个角色项保存稳定角色标识，选中状态来自当前 Tab 授权串。</zh-CN>
                //   <en>Each role item keeps a stable role id, with selection derived from the current Tab authorization string.</en>
                // </lang>
                var item = new ListItem(role.RoleName, role.RoleId.ToString())
                {
                    Selected = PortalRoleParser.Contains(currentTab.AuthorizedRoles, role.RoleName)
                };
                authRoles.Items.Add(item);
            }

            // <lang>
            //   <zh-CN>模块下拉只绑定当前 Profile、包状态和路径策略均允许的定义。</zh-CN>
            //   <en>Bind only definitions allowed by the current Profile, package state, and path policy.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>重新构造可新增定义列表，失败或禁用项保持不可选。</zh-CN>
            //   <en>Build the list of definitions eligible for new instances, keeping failures and disabled items unselectable.</en>
            // </lang>
            var eligibleDefinitions = new List<IModuleDefinitionItem>();
            foreach (IModuleDefinitionItem definition in ModuleDefConfig.GetModuleDefinitions())
            {
                // <lang>
                //   <zh-CN>路径解析器同时提供运行时描述和失败原因；本页只消费描述的启用状态。</zh-CN>
                //   <en>The path resolver supplies a runtime descriptor and failure reason; this page consumes only the enabled state.</en>
                // </lang>
                PortalModuleRuntimeDescriptor descriptor;
                string reason;
                if (!PortalModuleCatalog.TryResolveDesktopSource(
                    definition.DesktopSourceFile,
                    Context,
                    out descriptor,
                    out reason))
                {
                    continue;
                }

                if (!descriptor.IsEnabled)
                {
                    continue;
                }

                // <lang>
                //   <zh-CN>仅把通过路径解析且启用的定义加入下拉数据源。</zh-CN>
                //   <en>Add only definitions that pass path resolution and are enabled to the dropdown source.</en>
                // </lang>
                eligibleDefinitions.Add(definition);
            }

            return eligibleDefinitions;
        }

        private IModuleDefinitionItem FindEligibleModuleDefinition(int moduleDefinitionId)
        {
            // <lang>
            //   <zh-CN>在同一可用白名单中匹配请求定义，避免直接信任下拉值。</zh-CN>
            //   <en>Match the requested definition within the same eligible allowlist instead of trusting the dropdown value.</en>
            // </lang>
            return GetEligibleModuleDefinitions().FirstOrDefault(item => item.ModuleDefId == moduleDefinitionId);
        }

        private List<ModuleSettings> GetModules(string pane)
        {
            // <lang>
            //   <zh-CN>按不区分大小写的规范窗格筛选，并以顺序、标识提供稳定列表。</zh-CN>
            //   <en>Filter by pane case-insensitively and provide a stable list ordered by display order and id.</en>
            // </lang>
            return currentTab.Modules
                .Where(module => string.Equals(module.PaneName, pane, StringComparison.OrdinalIgnoreCase))
                .OrderBy(module => module.ModuleOrder)
                .ThenBy(module => module.ModuleId)
                .ToList();
        }

        private bool TryGetPaneListBox(string candidate, out string pane, out ListBox listBox)
        {
            // <lang>
            //   <zh-CN>先规范化外部窗格名称，再映射到固定的三个服务器控件。</zh-CN>
            //   <en>Normalize the external pane name first, then map it to one of three fixed server controls.</en>
            // </lang>
            pane = NormalizePaneName(candidate);
            switch (pane)
            {
                case LeftPaneName:
                    // <lang>
                    //   <zh-CN>左窗格名称只映射到左侧列表控件。</zh-CN>
                    //   <en>Map the left-pane name only to the left list control.</en>
                    // </lang>
                    listBox = leftPane;
                    return true;
                case ContentPaneName:
                    // <lang>
                    //   <zh-CN>内容窗格名称只映射到主内容列表控件。</zh-CN>
                    //   <en>Map the content-pane name only to the main content list control.</en>
                    // </lang>
                    listBox = contentPane;
                    return true;
                case RightPaneName:
                    // <lang>
                    //   <zh-CN>右窗格名称只映射到右侧列表控件。</zh-CN>
                    //   <en>Map the right-pane name only to the right list control.</en>
                    // </lang>
                    listBox = rightPane;
                    return true;
                default:
                    // <lang>
                    //   <zh-CN>未知窗格返回空控件，调用方必须拒绝继续操作。</zh-CN>
                    //   <en>Return no control for an unknown pane so callers must reject the operation.</en>
                    // </lang>
                    listBox = null;
                    return false;
            }
        }

        private bool TryGetSelectedModule(ListBox listBox, IList<ModuleSettings> modules, out ModuleSettings selectedModule)
        {
            // <lang>
            //   <zh-CN>把控件索引、读模型边界和数据键同时作为选择门禁，避免篡改索引访问其他模块。</zh-CN>
            //   <en>Gate selection on control index, read-model bounds, and data key together to prevent tampered indexes from reaching another module.</en>
            // </lang>
            selectedModule = null;
            if (listBox == null || modules == null || listBox.SelectedIndex < 0 ||
                listBox.SelectedIndex >= modules.Count || listBox.SelectedItem == null)
            {
                return false;
            }

            // <lang>
            //   <zh-CN>列表值必须是正整数，作为请求侧模块标识的解析结果。</zh-CN>
            //   <en>The list value must parse as a positive integer before it can represent the request-side module id.</en>
            // </lang>
            int selectedModuleId;
            if (!PortalNavigationPolicy.TryReadPositiveInt32(listBox.SelectedItem.Value, out selectedModuleId))
            {
                return false;
            }

            // <lang>
            //   <zh-CN>用同一索引读取当前窗格候选，并确认标识与控件值一致。</zh-CN>
            //   <en>Read the candidate at the same pane index and verify its id matches the control value.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>按模块可比较顺序排序后，以 1、3、5 的稳定间隔逐项持久化。</zh-CN>
            //   <en>Sort by the module comparison order, then persist a stable 1,3,5 sequence for each item.</en>
            // </lang>
            modules.Sort();
            // <lang>
            //   <zh-CN>局部顺序计数器只在当前窗格生命周期内递增，不复用数据库原值。</zh-CN>
            //   <en>The local order counter increments only within the current pane and does not reuse database order values.</en>
            // </lang>
            int order = 1;
            foreach (ModuleSettings module in modules)
            {
                // <lang>
                //   <zh-CN>逐项更新内存顺序并写回对应窗格，保持移动/排序后的布局一致。</zh-CN>
                //   <en>Update the in-memory order and persist the pane for each item to keep moved and sorted layout state consistent.</en>
                // </lang>
                module.ModuleOrder = order;
                order += 2;
                ModulesConfig.UpdateModuleOrder(module.ModuleId, module.ModuleOrder, module.PaneName);
            }
        }

        private void ReloadCurrentTab()
        {
            // <lang>
            //   <zh-CN>用当前 Tab 在桌面集合中的索引重建门户设置，刷新模块操作后的读模型。</zh-CN>
            //   <en>Rebuild Portal settings from the current Tab index in the desktop set to refresh the read model after module changes.</en>
            // </lang>
            int tabIndex = currentPortalSettings.DesktopTabs.FindIndex(tab => tab.TabId == tabId);
            // <lang>
            //   <zh-CN>重建快照继续复用既有数据依赖，不创建新的运行时配置来源。</zh-CN>
            //   <en>Reuse existing data dependencies when rebuilding the snapshot instead of introducing a new runtime configuration source.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>回跳地址固定为当前页面和已验证 Tab 标识，交由安全导航策略执行。</zh-CN>
            //   <en>Keep the return URL fixed to this page and the verified Tab id, then delegate execution to the safe navigation policy.</en>
            // </lang>
            PortalNavigationPolicy.RedirectToSafeReturnUrl(
                Context,
                ResolveUrl("~/Admin/TabLayout.aspx?tabid=" + tabId));
        }

        private void ShowMessage(string message)
        {
            // <lang>
            //   <zh-CN>所有低敏提示先 HTML 编码并将空值归一化。</zh-CN>
            //   <en>HTML-encode every low-sensitivity message and normalize null to an empty string.</en>
            // </lang>
            Message.Text = Server.HtmlEncode(message ?? string.Empty);
        }

        private static string NormalizePaneName(string pane)
        {
            // <lang>
            //   <zh-CN>把标记层的大小写兼容名称收敛为三个固定窗格常量，未知值返回空串。</zh-CN>
            //   <en>Collapse case-compatible markup names to three fixed pane constants and return empty for unknown values.</en>
            // </lang>
            if (string.Equals(pane, "leftPane", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pane, LeftPaneName, StringComparison.OrdinalIgnoreCase))
            {
                // <lang>
                //   <zh-CN>左侧别名统一返回左窗格规范名称。</zh-CN>
                //   <en>Normalize the left-pane aliases to the canonical left name.</en>
                // </lang>
                return LeftPaneName;
            }

            if (string.Equals(pane, "contentPane", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pane, ContentPaneName, StringComparison.OrdinalIgnoreCase))
            {
                // <lang>
                //   <zh-CN>内容别名统一返回主内容窗格规范名称。</zh-CN>
                //   <en>Normalize the content aliases to the canonical content name.</en>
                // </lang>
                return ContentPaneName;
            }

            if (string.Equals(pane, "rightPane", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pane, RightPaneName, StringComparison.OrdinalIgnoreCase))
            {
                // <lang>
                //   <zh-CN>右侧别名统一返回右窗格规范名称。</zh-CN>
                //   <en>Normalize the right-pane aliases to the canonical right name.</en>
                // </lang>
                return RightPaneName;
            }

            // <lang>
            //   <zh-CN>未知或空值不映射到任何窗格。</zh-CN>
            //   <en>Unknown or empty values map to no pane.</en>
            // </lang>
            return string.Empty;
        }
    }
}
