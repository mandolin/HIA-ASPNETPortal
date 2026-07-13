using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 主题全局选择和 Tab 覆盖管理页面。
    /// Theme global-selection and tab-override management page.
    /// </summary>
    /// <remarks>
    /// 本页面仅选择已部署且已校验的主题包；它不提供 ZIP 上传、在线 CSS 编辑、外部 URL 或主题脚本入口。
    /// This page selects deployed and validated theme packages only; it provides no ZIP upload, online CSS editing,
    /// external URL, or theme-script entry point.
    /// </remarks>
    public partial class ThemeSettings : PortalPage<ThemeSettings>
    {
        /// <summary>
        /// 用于读取门户 Tab 列表的旧数据服务。
        /// Legacy data service used to read the portal tab list.
        /// </summary>
        [Dependency]
        public ITabsDb TabsConfig { private get; set; }

        /// <summary>
        /// 初始化主题选择器和当前覆盖状态。
        /// Initializes theme selectors and current override state.
        /// </summary>
        /// <param name="sender">事件源。Event source.</param>
        /// <param name="e">事件参数。Event arguments.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            PortalAuthorization.RequireAdmin();
            if (!Page.IsPostBack)
            {
                BindThemeLists();
                BindTabs();
                BindStatuses();
            }
        }

        /// <summary>
        /// 保存全局主题的数据库运行级覆盖值。
        /// Saves the database runtime override for the global theme.
        /// </summary>
        /// <param name="sender">事件源。Event source.</param>
        /// <param name="e">事件参数。Event arguments.</param>
        protected void SaveGlobalThemeButton_Click(object sender, EventArgs e)
        {
            PortalAuthorization.RequireAdmin();
            PortalThemePackage package;
            string reason;
            if (!PortalThemeCatalog.TryGetTrustedPackage(GlobalThemeList.SelectedValue, out package, out reason))
            {
                ShowMessage("Select a validated deployed global theme.");
                return;
            }

            PortalSystemSettingWriteResult result = PortalSystemSettingsStore.SaveOverride(
                PortalSettingsRegistry.ThemeName,
                package.Name,
                Context);
            if (result.Succeeded)
            {
                PortalOperationAudit.Record(
                    "Theme",
                    "SetGlobalTheme",
                    "SystemSetting",
                    PortalSettingKeys.ThemeName,
                    "Selected deployed global theme '" + package.Name + "'.",
                    Context);
                RedirectToSelf();
                return;
            }

            ShowMessage(result.Message);
        }

        /// <summary>
        /// 清除全局主题的数据库覆盖值，使其回退部署配置或 Default。
        /// Clears the global theme database override so it falls back to deployment configuration or Default.
        /// </summary>
        /// <param name="sender">事件源。Event source.</param>
        /// <param name="e">事件参数。Event arguments.</param>
        protected void ResetGlobalThemeButton_Click(object sender, EventArgs e)
        {
            PortalAuthorization.RequireAdmin();
            PortalSystemSettingWriteResult result = PortalSystemSettingsStore.DeleteOverride(
                PortalSettingsRegistry.ThemeName,
                Context);
            if (result.Succeeded)
            {
                PortalOperationAudit.Record(
                    "Theme",
                    "ResetGlobalTheme",
                    "SystemSetting",
                    PortalSettingKeys.ThemeName,
                    "Removed the database global-theme override.",
                    Context);
                RedirectToSelf();
                return;
            }

            ShowMessage(result.Message);
        }

        /// <summary>
        /// 切换正在查看的门户 Tab 覆盖状态。
        /// Switches the portal tab whose override state is being viewed.
        /// </summary>
        /// <param name="sender">事件源。Event source.</param>
        /// <param name="e">事件参数。Event arguments.</param>
        protected void TabList_SelectedIndexChanged(object sender, EventArgs e)
        {
            PortalAuthorization.RequireAdmin();
            BindTabStatus();
        }

        /// <summary>
        /// 保存选定 Tab 的主题覆盖值。
        /// Saves the theme override for the selected tab.
        /// </summary>
        /// <param name="sender">事件源。Event source.</param>
        /// <param name="e">事件参数。Event arguments.</param>
        protected void SaveTabThemeButton_Click(object sender, EventArgs e)
        {
            PortalAuthorization.RequireAdmin();
            int tabId;
            if (!TryGetSelectedTabId(out tabId))
            {
                ShowMessage("Select a portal tab before applying an override.");
                return;
            }

            PortalTabThemeOverrideWriteResult result = PortalTabThemeOverrides.Save(
                tabId,
                TabThemeList.SelectedValue,
                Context);
            if (result.Succeeded)
            {
                PortalOperationAudit.Record(
                    "Theme",
                    "SetTabThemeOverride",
                    "Tab",
                    tabId.ToString(CultureInfo.InvariantCulture),
                    "Selected deployed theme '" + TabThemeList.SelectedValue + "' for tab override.",
                    Context);
                RedirectToSelf();
                return;
            }

            ShowMessage(result.Message);
        }

        /// <summary>
        /// 清除选定 Tab 的主题覆盖值。
        /// Clears the theme override for the selected tab.
        /// </summary>
        /// <param name="sender">事件源。Event source.</param>
        /// <param name="e">事件参数。Event arguments.</param>
        protected void ClearTabThemeButton_Click(object sender, EventArgs e)
        {
            PortalAuthorization.RequireAdmin();
            int tabId;
            if (!TryGetSelectedTabId(out tabId))
            {
                ShowMessage("Select a portal tab before clearing an override.");
                return;
            }

            PortalTabThemeOverrideWriteResult result = PortalTabThemeOverrides.Delete(tabId, Context);
            if (result.Succeeded)
            {
                PortalOperationAudit.Record(
                    "Theme",
                    "ClearTabThemeOverride",
                    "Tab",
                    tabId.ToString(CultureInfo.InvariantCulture),
                    "Removed the tab theme override.",
                    Context);
                RedirectToSelf();
                return;
            }

            ShowMessage(result.Message);
        }

        private void BindThemeLists()
        {
            IList<PortalThemePackage> packages = PortalThemeCatalog.GetTrustedPackages();
            BindThemeList(GlobalThemeList, packages);
            BindThemeList(TabThemeList, packages);
        }

        private void BindTabs()
        {
            TabList.Items.Clear();
            foreach (ITabItem tab in TabsConfig.GetTabs())
            {
                string tabName = string.IsNullOrWhiteSpace(tab.TabName) ? "(unnamed)" : tab.TabName.Trim();
                TabList.Items.Add(new ListItem(
                    Server.HtmlEncode(tabName) + " (" + tab.TabId.ToString(CultureInfo.InvariantCulture) + ")",
                    tab.TabId.ToString(CultureInfo.InvariantCulture)));
            }
        }

        private void BindStatuses()
        {
            PortalRuntimeSettingValue globalTheme = PortalRuntimeSettings.GetEffectiveValue(
                PortalSettingsRegistry.ThemeName,
                Context);
            SelectTheme(GlobalThemeList, globalTheme.Value);
            GlobalThemeStatusLabel.Text = Server.HtmlEncode(
                globalTheme.Value + " (" + globalTheme.Source + ")");
            BindTabStatus();
        }

        private void BindTabStatus()
        {
            int tabId;
            if (!TryGetSelectedTabId(out tabId))
            {
                TabThemeStatusLabel.Text = "No portal tab is available.";
                return;
            }

            PortalTabThemeOverrideReadResult result = PortalTabThemeOverrides.Read(tabId, Context);
            if (!result.IsAvailable)
            {
                TabThemeStatusLabel.Text = "The tab-theme migration has not been applied.";
                return;
            }

            if (!result.IsFound)
            {
                TabThemeStatusLabel.Text = "Global theme applies.";
                return;
            }

            SelectTheme(TabThemeList, result.ThemeName);
            TabThemeStatusLabel.Text = Server.HtmlEncode(result.ThemeName);
        }

        private static void BindThemeList(DropDownList list, IEnumerable<PortalThemePackage> packages)
        {
            list.Items.Clear();
            foreach (PortalThemePackage package in packages)
            {
                list.Items.Add(new ListItem(
                    package.DisplayName + " (" + package.Name + ")",
                    package.Name));
            }
        }

        private static void SelectTheme(DropDownList list, string themeName)
        {
            ListItem selected = list.Items.FindByValue(themeName ?? string.Empty);
            if (selected != null)
            {
                list.ClearSelection();
                selected.Selected = true;
            }
        }

        private bool TryGetSelectedTabId(out int tabId)
        {
            return int.TryParse(TabList.SelectedValue, NumberStyles.None, CultureInfo.InvariantCulture, out tabId) &&
                   tabId > 0;
        }

        private void RedirectToSelf()
        {
            Response.Redirect(Request.RawUrl, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void ShowMessage(string message)
        {
            MessageLabel.Text = Server.HtmlEncode(message ?? string.Empty);
        }
    }
}
