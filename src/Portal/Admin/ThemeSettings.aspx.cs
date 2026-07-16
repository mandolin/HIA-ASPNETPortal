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
        /// 所有成功的全局主题与 Tab 覆盖操作均写入运营审计，但本页不创建或删除物理主题目录。
        /// This page selects deployed and validated theme packages only; it provides no ZIP upload, online CSS editing,
        /// external URL, or theme-script entry point. Every successful global-theme or Tab-override operation writes an
        /// operations audit, while this page never creates or deletes a physical theme directory.
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
        /// <remarks>
        /// 管理员授权在每次请求中执行。列表只显示 catalog 校验通过的部署主题；主题目录缺失或无效时不会从表单值回退加载。
        /// Administrator authorization is performed on every request. Lists show only catalog-validated deployed themes;
        /// a missing or invalid theme directory is never loaded as a fallback from form input.
        /// </remarks>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.ThemeView))
            {
                return;
            }

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
        /// <remarks>
        /// 成功时写入运行时设置覆盖和运营审计，然后重定向以重新解析本请求的主题。失败不修改主题文件或原有覆盖值。
        /// On success, writes a runtime-setting override and operations audit, then redirects to re-resolve the theme
        /// for the new request. Failure changes neither theme files nor the existing override.
        /// </remarks>
        protected void SaveGlobalThemeButton_Click(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.ThemeEdit))
            {
                return;
            }

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
        /// <remarks>
        /// 仅删除数据库覆盖值；有效主题随后按 appSettings 或 Default 回退，并记录成功的运营审计。
        /// Deletes only the database override; the effective theme then falls back through appSettings or Default and a
        /// successful operation is recorded in operations audit.
        /// </remarks>
        protected void ResetGlobalThemeButton_Click(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.ThemeEdit))
            {
                return;
            }

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
        /// <remarks>
        /// 该动作只更新当前管理页面的显示状态，不写入 Tab 覆盖或全局设置。
        /// This action updates only display state on the current admin page; it writes neither a Tab override nor a global setting.
        /// </remarks>
        protected void TabList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.ThemeView))
            {
                return;
            }

            BindTabStatus();
        }

        /// <summary>
        /// 保存选定 Tab 的主题覆盖值。
        /// Saves the theme override for the selected tab.
        /// </summary>
        /// <param name="sender">事件源。Event source.</param>
        /// <param name="e">事件参数。Event arguments.</param>
        /// <remarks>
        /// 覆盖写入前会再次由存储层验证已部署主题。成功后写入运营审计；本页面不改变主题文件或主题包 manifest。
        /// The store validates the deployed theme again before writing an override. Success records an operations audit;
        /// this page does not change theme files or package manifests.
        /// </remarks>
        protected void SaveTabThemeButton_Click(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.ThemeEdit))
            {
                return;
            }

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
        /// <remarks>
        /// 清除后当前 Tab 回退已解析全局主题。写入失败时保留原覆盖值，并由状态存储记录诊断。
        /// After clearing, the current Tab falls back to the resolved global theme. On a write failure the existing
        /// override remains, and diagnostics are recorded by the state store.
        /// </remarks>
        protected void ClearTabThemeButton_Click(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.ThemeEdit))
            {
                return;
            }

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
