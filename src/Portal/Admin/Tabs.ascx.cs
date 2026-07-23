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

        private int tabId;
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
            if (!TryInitializeRequest())
            {
                return;
            }

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
            if (!TryInitializeRequest())
            {
                return;
            }

            try
            {
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
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.PortalTabsEdit) ||
                !TryReadOptionalPositiveParameter("tabid", out tabId) ||
                !TryReadOptionalNonNegativeParameter("tabindex", out tabIndex))
            {
                return false;
            }

            PortalTabs.Clear();
            foreach (ITabItem tab in PortalContext.GetPortalSettings().DesktopTabs)
            {
                PortalTabs.Add(new TabSettings(tab));
            }

            EnsureCoreAdministrationTabLast();
            return true;
        }

        private bool TryReadOptionalPositiveParameter(string parameterName, out int value)
        {
            value = 0;
            string rawValue = Request.Params[parameterName];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (PortalNavigationPolicy.TryReadPositiveInt32(rawValue, out value))
            {
                return true;
            }

            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        private bool TryReadOptionalNonNegativeParameter(string parameterName, out int value)
        {
            value = 0;
            string rawValue = Request.Params[parameterName];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (PortalNavigationPolicy.TryReadNonNegativeInt32(rawValue, out value))
            {
                return true;
            }

            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        private bool TryGetSelectedTab(out TabSettings selectedTab)
        {
            selectedTab = null;
            if (tabList.SelectedIndex < 0 || tabList.SelectedIndex >= PortalTabs.Count || tabList.SelectedItem == null)
            {
                return false;
            }

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
            EnsureCoreAdministrationTabLast();
            int order = 1;
            foreach (TabSettings tab in PortalTabs)
            {
                tab.TabOrder = order;
                order += 2;
                TabsConfig.UpdateTabOrder(tab.TabId, tab.TabOrder);
            }
        }

        private void EnsureCoreAdministrationTabLast()
        {
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
            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ResolveUrl("~/DesktopDefault.aspx"));
        }

        private void ShowMessage(string message)
        {
            Message.Text = Server.HtmlEncode(message ?? string.Empty);
        }
    }
}
