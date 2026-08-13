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
    ///   <zh-CN>旧门户 Tab 列表、排序和创建管理控件。</zh-CN>
    ///   <en>Legacy Portal control for Tab listing, ordering, and creation.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>当前以名称 <c>Admin</c> 识别核心后台 Tab，并在此兼容阶段禁止从 UI 删除该 Tab。 未来应以稳定标识替代名称约定。</zh-CN>
    ///   <en>The current compatibility phase identifies the core administration Tab by the <c>Admin</c> name and prevents deleting it from this UI. A future design should replace this naming convention with a stable identifier.</en>
    /// </lang>
    /// </remarks>
    public partial class Tabs : PortalModuleControl<Tabs>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>供列表绑定的当前门户 Tab 设置集合。</zh-CN>
        ///   <en>Current-Portal Tab-settings collection used for list binding.</en>
        /// </lang>
        /// </summary>
        protected readonly List<TabSettings> PortalTabs = new List<TabSettings>();

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选的 Tab 回跳标识。</zh-CN>
        ///   <en>Optional Tab identifier preserved for return navigation.</en>
        /// </lang>
        /// </summary>
        private int tabId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选的 Tab 列表索引回跳参数。</zh-CN>
        ///   <en>Optional Tab-list index preserved for return navigation.</en>
        /// </lang>
        /// </summary>
        private int tabIndex;

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
        ///   <zh-CN>模块实例数据访问依赖。</zh-CN>
        ///   <en>Module-instance data-access dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public new IModulesDb ModulesConfig { private get; set; }

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
        ///   <zh-CN>门户全局设置数据访问依赖。</zh-CN>
        ///   <en>Portal global-settings data-access dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IGlobalsDb PortalConfig { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>授权、读取可选导航参数并在首次请求绑定 Tab 列表。</zh-CN>
        ///   <en>Authorizes, reads optional navigation parameters, and binds the Tab list on the initial request.</en>
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
            //   <zh-CN>统一初始化负责权限、导航参数和当前门户 Tab 快照，避免各事件处理器形成不同门禁。</zh-CN>
            //   <en>Centralized initialization owns permission, navigation parameters, and the current portal Tab snapshot so event handlers share one gate.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            // <lang>
            //   <zh-CN>仅首次请求绑定列表，保留回发时的选中项和控件状态。</zh-CN>
            //   <en>Bind the list only on the initial request, preserving the selected item and control state during postback.</en>
            // </lang>
            if (!Page.IsPostBack)
            {
                tabList.DataBind();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>调整当前选择的普通 Tab 顺序。</zh-CN>
        ///   <en>Adjusts the order of the currently selected non-core Tab.</en>
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
            //   <zh-CN>排序命令只接受受控的 up/down 值，并要求选中项通过列表与快照交叉校验。</zh-CN>
            //   <en>Ordering accepts only the controlled up/down commands and requires the selected item to cross-check against the list snapshot.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            IButtonControl button = sender as IButtonControl;
            TabSettings selectedTab;
            if (button == null || (button.CommandName != "up" && button.CommandName != "down") ||
                !TryGetSelectedTab(out selectedTab))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            // <lang>
            //   <zh-CN>先调整内存顺序，再由 OrderTabs 写回稳定的奇数步长并记录低敏审计。</zh-CN>
            //   <en>Adjust the in-memory order first, then let OrderTabs persist a stable odd-step sequence and record a low-sensitivity audit.</en>
            // </lang>
            selectedTab.TabOrder += button.CommandName == "down" ? 3 : -3;
            try
            {
                OrderTabs();
                PortalOperationAudit.Record(
                    "TabAdministration",
                    "Order",
                    "Tab",
                    selectedTab.TabId.ToString(),
                    "Changed Tab display order.",
                    Context);
                RedirectToPortalHome();
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>排序失败只展示事件编号，不回显异常或 Tab 资料。</zh-CN>
                //   <en>On ordering failure, expose only the event identifier rather than the exception or Tab data.</en>
                // </lang>
                string eventId = PortalDiagnostics.Error(
                    "Admin.Tabs.Order",
                    "Ordering a Tab failed. TabId=" + selectedTab.TabId,
                    exception,
                    Context);
                ShowMessage("Tab 排序失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除当前选择的普通 Tab；删除会连带清理该 Tab 的模块实例。</zh-CN>
        ///   <en>Deletes the currently selected non-core Tab; deletion also cleans up that Tab's module instances.</en>
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
            //   <zh-CN>删除前复用统一初始化和选中项校验，并阻止删除受保护的核心后台 Tab。</zh-CN>
            //   <en>Reuse centralized initialization and selection validation before deletion, and block deletion of the protected administration Tab.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            TabSettings selectedTab;
            if (!TryGetSelectedTab(out selectedTab))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            if (PortalAdministrationPolicy.IsProtectedAdministrationTabName(selectedTab.TabName))
            {
                ShowMessage("核心后台 Tab 不能删除。");
                return;
            }

            try
            {
                // <lang>
                //   <zh-CN>先统计关联模块实例，再执行 Tab 删除、列表重排和低敏审计，保持操作语义可追踪。</zh-CN>
                //   <en>Count related module instances before deleting the Tab, reordering the list, and recording a traceable low-sensitivity audit.</en>
                // </lang>
                int moduleCount = ModulesConfig.GetModulesByTab(selectedTab.TabId).Count();
                TabsConfig.DeleteTab(selectedTab.TabId);
                PortalTabs.Remove(selectedTab);
                OrderTabs();
                PortalOperationAudit.Record(
                    "TabAdministration",
                    "Delete",
                    "Tab",
                    selectedTab.TabId.ToString(),
                    "Deleted Tab and " + moduleCount + " module instance(s).",
                    Context);
                RedirectToPortalHome();
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>删除失败不吞异常，向用户仅返回事件编号。</zh-CN>
                //   <en>Do not swallow deletion failures; return only the event identifier to the user.</en>
                // </lang>
                string eventId = PortalDiagnostics.Error(
                    "Admin.Tabs.Delete",
                    "Deleting a Tab failed. TabId=" + selectedTab.TabId,
                    exception,
                    Context);
                ShowMessage("Tab 删除失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建默认公开的普通 Tab，并转入其布局设置页面。</zh-CN>
        ///   <en>Creates a default public non-core Tab and opens its layout-settings page.</en>
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
        protected void AddTab_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>创建入口只产生默认公开 Tab，并沿用既有布局设置页和安全回跳策略。</zh-CN>
            //   <en>The creation entry produces only a default public Tab and reuses the existing layout page and safe-return policy.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            try
            {
                // <lang>
                //   <zh-CN>新增后立即纳入当前快照、规范排序并记录创建审计，再导航到已验证的布局页。</zh-CN>
                //   <en>After creation, add the Tab to the current snapshot, normalize ordering, record the audit, and navigate to the verified layout page.</en>
                // </lang>
                PortalSettings portalSettings = PortalContext.GetPortalSettings();
                int newTabId = TabsConfig.AddTab(portalSettings.PortalId, "New Tab", 999);
                ITabItem newTab = TabsConfig.GetSingleTab(newTabId);
                PortalTabs.Add(new TabSettings(newTab));
                OrderTabs();
                PortalOperationAudit.Record(
                    "TabAdministration",
                    "Create",
                    "Tab",
                    newTabId.ToString(),
                    "Created a new Tab.",
                    Context);
                PortalNavigationPolicy.RedirectToSafeReturnUrl(
                    Context,
                    ResolveUrl("~/Admin/TabLayout.aspx?tabid=" + newTabId));
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>创建失败只保留事件编号提示，避免把底层异常暴露到页面。</zh-CN>
                //   <en>On creation failure, show only the event identifier and avoid exposing the underlying exception.</en>
                // </lang>
                string eventId = PortalDiagnostics.Error(
                    "Admin.Tabs.Add",
                    "Adding a Tab failed.",
                    exception,
                    Context);
                ShowMessage("Tab 创建失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>进入当前选择 Tab 的布局设置页。</zh-CN>
        ///   <en>Opens the layout-settings page for the currently selected Tab.</en>
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
            //   <zh-CN>编辑入口要求选中项来自已验证快照，并只通过安全导航策略进入布局页。</zh-CN>
            //   <en>The edit entry requires a selection from the verified snapshot and enters the layout page only through the safe-navigation policy.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            TabSettings selectedTab;
            if (!TryGetSelectedTab(out selectedTab))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(
                Context,
                ResolveUrl("~/Admin/TabLayout.aspx?tabid=" + selectedTab.TabId));
        }

        private bool TryInitializeRequest()
        {
            // <lang>
            //   <zh-CN>权限和两个可选导航参数必须全部通过后，才读取门户 Tab 快照。</zh-CN>
            //   <en>Read the portal Tab snapshot only after permission and both optional navigation parameters pass validation.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.PortalTabsEdit) ||
                !TryReadOptionalPositiveParameter("tabid", out tabId) ||
                !TryReadOptionalNonNegativeParameter("tabindex", out tabIndex))
            {
                return false;
            }

            // <lang>
            //   <zh-CN>快照来自当前门户桌面 Tab 集合；后续选中项和排序都基于这一受控集合。</zh-CN>
            //   <en>The snapshot comes from the current portal desktop Tab collection; later selection and ordering operate on this controlled set.</en>
            // </lang>
            PortalTabs.Clear();
            foreach (ITabItem tab in PortalContext.GetPortalSettings().DesktopTabs)
            {
                PortalTabs.Add(new TabSettings(tab));
            }

            // <lang>
            //   <zh-CN>将核心后台 Tab 固定到排序末端，避免普通 Tab 操作改变其兼容位置。</zh-CN>
            //   <en>Keep the core administration Tab at the end so ordinary Tab operations cannot change its compatibility position.</en>
            // </lang>
            EnsureCoreAdministrationTabLast();
            return true;
        }

        private bool TryReadOptionalPositiveParameter(string parameterName, out int value)
        {
            value = 0;
            // <lang>
            //   <zh-CN>缺失参数保持兼容默认值 0；提供的值必须是正整数。</zh-CN>
            //   <en>Keep the compatibility default of 0 when absent; a supplied value must be a positive integer.</en>
            // </lang>
            string rawValue = Request.Params[parameterName];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (PortalNavigationPolicy.TryReadPositiveInt32(rawValue, out value))
            {
                return true;
            }

            // <lang>
            //   <zh-CN>非法正整数参数直接进入拒绝页，不把原始输入带入后续地址。</zh-CN>
            //   <en>Route invalid positive-integer input to the denied page without carrying the raw value into later URLs.</en>
            // </lang>
            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        private bool TryReadOptionalNonNegativeParameter(string parameterName, out int value)
        {
            value = 0;
            // <lang>
            //   <zh-CN>索引参数允许零，但仍必须通过非负整数策略。</zh-CN>
            //   <en>The index permits zero but must still pass the non-negative-integer policy.</en>
            // </lang>
            string rawValue = Request.Params[parameterName];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (PortalNavigationPolicy.TryReadNonNegativeInt32(rawValue, out value))
            {
                return true;
            }

            // <lang>
            //   <zh-CN>非法索引不回退为默认值，直接阻断导航。</zh-CN>
            //   <en>Do not fall back to a default for an invalid index; block navigation directly.</en>
            // </lang>
            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        private bool TryGetSelectedTab(out TabSettings selectedTab)
        {
            selectedTab = null;
            // <lang>
            //   <zh-CN>先检查控件索引、快照范围和选中项存在性，拒绝越界或缺失的回发状态。</zh-CN>
            //   <en>Check the control index, snapshot bounds, and selected item before accepting postback state.</en>
            // </lang>
            if (tabList.SelectedIndex < 0 || tabList.SelectedIndex >= PortalTabs.Count || tabList.SelectedItem == null)
            {
                return false;
            }

            // <lang>
            //   <zh-CN>列表值必须是正整数，并与同索引快照项的真实 TabId 一致。</zh-CN>
            //   <en>The list value must be a positive integer matching the real TabId of the snapshot item at the same index.</en>
            // </lang>
            int selectedTabId;
            if (!PortalNavigationPolicy.TryReadPositiveInt32(tabList.SelectedItem.Value, out selectedTabId))
            {
                return false;
            }

            TabSettings candidate = PortalTabs[tabList.SelectedIndex];
            if (candidate.TabId != selectedTabId)
            {
                return false;
            }

            selectedTab = candidate;
            return true;
        }

        private void OrderTabs()
        {
            // <lang>
            //   <zh-CN>排序前再次固定核心后台 Tab，随后以奇数步长写回全部 Tab 的稳定顺序。</zh-CN>
            //   <en>Fix the core administration Tab again before writing a stable odd-step order for every Tab.</en>
            // </lang>
            EnsureCoreAdministrationTabLast();
            int order = 1;
            foreach (TabSettings tab in PortalTabs)
            {
                // <lang>
                //   <zh-CN>每个写回只更新顺序字段，不在排序流程中改变 Tab 名称、权限或模块内容。</zh-CN>
                //   <en>Each write-back updates only ordering; the sort flow does not change Tab names, permissions, or module content.</en>
                // </lang>
                tab.TabOrder = order;
                order += 2;
                TabsConfig.UpdateTabOrder(tab.TabId, tab.TabOrder);
            }
        }

        private void EnsureCoreAdministrationTabLast()
        {
            // <lang>
            //   <zh-CN>按受保护名称识别核心后台 Tab，并将其排序值提升为最大值后再排序。</zh-CN>
            //   <en>Identify the core administration Tab by its protected name, assign the maximum order, then sort.</en>
            // </lang>
            TabSettings administrationTab = PortalTabs.FirstOrDefault(tab =>
                PortalAdministrationPolicy.IsProtectedAdministrationTabName(tab.TabName));
            if (administrationTab != null)
            {
                administrationTab.TabOrder = int.MaxValue;
            }

            PortalTabs.Sort();
        }

        private void RedirectToPortalHome()
        {
            // <lang>
            //   <zh-CN>操作完成后只通过安全回跳策略返回门户首页。</zh-CN>
            //   <en>After an operation, return to the portal home only through the safe-return policy.</en>
            // </lang>
            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ResolveUrl("~/DesktopDefault.aspx"));
        }

        private void ShowMessage(string message)
        {
            // <lang>
            //   <zh-CN>所有提示先进行 HTML 编码，避免旧控件把诊断或输入内容当作标记输出。</zh-CN>
            //   <en>HTML-encode every message so diagnostics or input cannot be emitted as markup by the legacy control.</en>
            // </lang>
            Message.Text = Server.HtmlEncode(message ?? string.Empty);
        }
    }
}
