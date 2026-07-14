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
    /// 中文：旧门户 Tab 列表、排序和创建管理控件。
    ///
    /// English: Legacy Portal control for Tab listing, ordering, and creation.
    /// </summary>
    /// <remarks>
    /// 中文：当前以名称 <c>Admin</c> 识别核心后台 Tab，并在此兼容阶段禁止从 UI 删除该 Tab。
    /// 未来应以稳定标识替代名称约定。
    ///
    /// English: The current compatibility phase identifies the core administration Tab by the <c>Admin</c> name and
    /// prevents deleting it from this UI. A future design should replace this naming convention with a stable identifier.
    /// </remarks>
    public partial class Tabs : PortalModuleControl<Tabs>
    {
        /// <summary>
        /// 中文：供列表绑定的当前门户 Tab 设置集合。
        ///
        /// English: Current-Portal Tab-settings collection used for list binding.
        /// </summary>
        protected readonly List<TabSettings> PortalTabs = new List<TabSettings>();

        private int tabId;
        private int tabIndex;

        /// <summary>中文：模块定义数据访问依赖。English: Module-definition data-access dependency.</summary>
        [Dependency]
        public IModuleDefsDb ModuleDefConfig { private get; set; }

        /// <summary>中文：模块实例数据访问依赖。English: Module-instance data-access dependency.</summary>
        [Dependency]
        public new IModulesDb ModulesConfig { private get; set; }

        /// <summary>中文：Tab 数据访问依赖。English: Tab data-access dependency.</summary>
        [Dependency]
        public ITabsDb TabsConfig { private get; set; }

        /// <summary>中文：门户全局设置数据访问依赖。English: Portal global-settings data-access dependency.</summary>
        [Dependency]
        public IGlobalsDb PortalConfig { private get; set; }

        /// <summary>
        /// 中文：授权、读取可选导航参数并在首次请求绑定 Tab 列表。
        ///
        /// English: Authorizes, reads optional navigation parameters, and binds the Tab list on the initial request.
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
                tabList.DataBind();
            }
        }

        /// <summary>
        /// 中文：调整当前选择的普通 Tab 顺序。
        ///
        /// English: Adjusts the order of the currently selected non-core Tab.
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
        /// 中文：删除当前选择的普通 Tab；删除会连带清理该 Tab 的模块实例。
        ///
        /// English: Deletes the currently selected non-core Tab; deletion also cleans up that Tab's module instances.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：图像按钮事件数据。English: Image-button event data.</param>
        protected void DeleteBtn_Click(object sender, ImageClickEventArgs e)
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
        /// 中文：创建默认公开的普通 Tab，并转入其布局设置页面。
        ///
        /// English: Creates a default public non-core Tab and opens its layout-settings page.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
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
        /// 中文：进入当前选择 Tab 的布局设置页。
        ///
        /// English: Opens the layout-settings page for the currently selected Tab.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：图像按钮事件数据。English: Image-button event data.</param>
        protected void EditBtn_Click(object sender, ImageClickEventArgs e)
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
            if (!PortalAuthorization.EnsureAdmin(Context) ||
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
