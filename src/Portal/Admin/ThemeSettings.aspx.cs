using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>主题全局选择和 Tab 覆盖管理页面。</zh-CN>
    ///   <en>Theme global-selection and tab-override management page.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本页面仅选择已部署且已校验的主题包；它不提供 ZIP 上传、在线 CSS 编辑、外部 URL 或主题脚本入口。成功的全局主题与 Tab 覆盖操作会写入运营审计，但本页不创建或删除物理主题目录。</zh-CN>
    ///   <en>This page selects deployed and validated theme packages only; it provides no ZIP upload, online CSS editing, external URL, or theme-script entry point. Successful global-theme and Tab-override operations write an operations audit, while this page never creates or deletes a physical theme directory.</en>
    /// </lang>
    /// </remarks>
    public partial class ThemeSettings : PortalPage<ThemeSettings>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>用于读取门户 Tab 列表的旧数据服务。</zh-CN>
        ///   <en>Legacy data service used to read the portal tab list.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public ITabsDb TabsConfig { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化主题选择器和当前覆盖状态。</zh-CN>
        ///   <en>Initializes theme selectors and current override state.</en>
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
        /// <remarks>
        /// <lang>
        ///   <zh-CN>每次请求都会执行管理员权限门禁；列表只显示 catalog 校验通过的部署主题，主题目录缺失或无效时不会从表单值回退加载。</zh-CN>
        ///   <en>Administrator authorization is checked on every request; lists show only catalog-validated deployed themes, and a missing or invalid theme directory is never loaded as a fallback from form input.</en>
        /// </lang>
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
        /// <lang>
        ///   <zh-CN>保存全局主题的数据库运行级覆盖值。</zh-CN>
        ///   <en>Saves the database runtime override for the global theme.</en>
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
        /// <remarks>
        /// <lang>
        ///   <zh-CN>成功时写入运行时设置覆盖和运营审计，然后重定向以让新请求重新解析主题；失败不修改主题文件或原有覆盖值。</zh-CN>
        ///   <en>On success, writes a runtime-setting override and operations audit, then redirects so the new request re-resolves the theme; failure changes neither theme files nor the existing override.</en>
        /// </lang>
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
        /// <lang>
        ///   <zh-CN>清除全局主题的数据库覆盖值，使其回退部署配置或 Default。</zh-CN>
        ///   <en>Clears the global theme database override so it falls back to deployment configuration or Default.</en>
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
        /// <remarks>
        /// <lang>
        ///   <zh-CN>仅删除数据库覆盖值；有效主题随后按 appSettings 或 Default 回退，并记录成功的运营审计。</zh-CN>
        ///   <en>Deletes only the database override; the effective theme then falls back through appSettings or Default, and a successful operation is recorded in operations audit.</en>
        /// </lang>
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
        /// <lang>
        ///   <zh-CN>切换正在查看的门户 Tab 覆盖状态。</zh-CN>
        ///   <en>Switches the portal tab whose override state is being viewed.</en>
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
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该动作只更新当前管理页面的显示状态，不写入 Tab 覆盖或全局设置。</zh-CN>
        ///   <en>This action updates only display state on the current admin page; it writes neither a Tab override nor a global setting.</en>
        /// </lang>
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
        /// <lang>
        ///   <zh-CN>保存选定 Tab 的主题覆盖值。</zh-CN>
        ///   <en>Saves the theme override for the selected tab.</en>
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
        /// <remarks>
        /// <lang>
        ///   <zh-CN>覆盖写入前会再次由存储层验证已部署主题；成功后写入运营审计，本页面不改变主题文件或主题包 manifest。</zh-CN>
        ///   <en>The store validates the deployed theme again before writing an override; success records an operations audit, and this page does not change theme files or package manifests.</en>
        /// </lang>
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
        /// <lang>
        ///   <zh-CN>清除选定 Tab 的主题覆盖值。</zh-CN>
        ///   <en>Clears the theme override for the selected tab.</en>
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
        /// <remarks>
        /// <lang>
        ///   <zh-CN>清除后当前 Tab 回退已解析全局主题；写入失败时保留原覆盖值，并由状态存储记录诊断。</zh-CN>
        ///   <en>After clearing, the current Tab falls back to the resolved global theme; on a write failure the existing override remains and diagnostics are recorded by the state store.</en>
        /// </lang>
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

        // <lang>
        //   <zh-CN>绑定全局主题和 Tab 主题下拉列表；数据源只提供已部署且受信的主题包。</zh-CN>
        //   <en>Binds the global-theme and Tab-theme dropdowns; the source contains deployed and trusted theme packages only.</en>
        // </lang>
        private void BindThemeLists()
        {
            IList<PortalThemePackage> packages = PortalThemeCatalog.GetTrustedPackages();
            BindThemeList(GlobalThemeList, packages);
            BindThemeList(TabThemeList, packages);
        }

        // <lang>
        //   <zh-CN>读取旧 Tab 数据服务并构造展示值；Tab 名称先归一化，再进行 HTML 编码，隐藏值仅使用正数 Tab 标识。</zh-CN>
        //   <en>Reads the legacy Tab data service and builds display values; names are normalized then HTML-encoded, while hidden values contain positive Tab identifiers only.</en>
        // </lang>
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

        // <lang>
        //   <zh-CN>读取全局有效主题并初始化全局和当前 Tab 状态；状态文本编码后才写入控件。</zh-CN>
        //   <en>Reads the effective global theme and initializes global/current-Tab status; status text is encoded before being assigned to controls.</en>
        // </lang>
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

        // <lang>
        //   <zh-CN>读取当前 Tab 的覆盖事实，并区分无 Tab、迁移不可用、未命中和已命中回退状态。</zh-CN>
        //   <en>Reads the current Tab override fact and distinguishes no Tab, unavailable migration, not-found, and found fallback states.</en>
        // </lang>
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

        // <lang>
        //   <zh-CN>把受信主题包投影到下拉列表；显示名和规范名称来自 catalog，不接受表单值扩展列表。</zh-CN>
        //   <en>Projects trusted theme packages into a dropdown; display and canonical names come from the catalog, not from form input.</en>
        // </lang>
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

        // <lang>
        //   <zh-CN>按规范主题名称选择现有列表项；不存在的名称不会动态加入列表或改变当前选择。</zh-CN>
        //   <en>Selects an existing list item by canonical theme name; an unknown name is not added dynamically and does not change the current selection.</en>
        // </lang>
        private static void SelectTheme(DropDownList list, string themeName)
        {
            ListItem selected = list.Items.FindByValue(themeName ?? string.Empty);
            if (selected != null)
            {
                list.ClearSelection();
                selected.Selected = true;
            }
        }

        // <lang>
        //   <zh-CN>解析当前 Tab 隐藏值为正整数标识；解析失败或非正数均视为没有可操作 Tab。</zh-CN>
        //   <en>Parses the current Tab hidden value as a positive integer identifier; parse failures and non-positive values mean that no actionable Tab is selected.</en>
        // </lang>
        private bool TryGetSelectedTabId(out int tabId)
        {
            return int.TryParse(TabList.SelectedValue, NumberStyles.None, CultureInfo.InvariantCulture, out tabId) &&
                   tabId > 0;
        }

        // <lang>
        //   <zh-CN>使用当前请求地址执行非终止重定向，并要求 ASP.NET 完成本请求；用于写入成功后的重新解析。</zh-CN>
        //   <en>Performs a non-terminating redirect to the current request URL and asks ASP.NET to complete the request; used to re-resolve after a successful write.</en>
        // </lang>
        private void RedirectToSelf()
        {
            Response.Redirect(Request.RawUrl, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        // <lang>
        //   <zh-CN>把失败或输入提示进行 HTML 编码后写入消息控件；空消息按空字符串处理。</zh-CN>
        //   <en>HTML-encodes a failure or input message before assigning it to the message control; null messages become an empty string.</en>
        // </lang>
        private void ShowMessage(string message)
        {
            MessageLabel.Text = Server.HtmlEncode(message ?? string.Empty);
        }
    }
}
