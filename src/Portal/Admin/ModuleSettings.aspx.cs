using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：旧门户模块实例设置页面。
    ///
    /// English: Legacy Portal module-instance settings page.
    /// </summary>
    /// <remarks>
    /// 中文：页面只允许管理员修改属于指定 Tab 的模块实例；缓存秒数保持 <c>0</c> 为不缓存的既有语义。
    ///
    /// English: The page allows administrators to modify only a module instance that belongs to the specified Tab;
    /// cache timeout <c>0</c> retains its existing no-cache meaning.
    /// </remarks>
    public partial class ModuleSettingsPage : PortalPage<ModuleSettingsPage>
    {
        private const int MaximumCacheSeconds = 86400;
        private int moduleId;
        private int tabId;
        private ModuleSettings currentModule;

        /// <summary>
        /// 中文：当前门户角色查询依赖。
        ///
        /// English: Current-Portal role-query dependency.
        /// </summary>
        [Dependency]
        public IRolesDb RolesDb { private get; set; }

        /// <summary>
        /// 中文：模块实例数据访问依赖。
        ///
        /// English: Module-instance data-access dependency.
        /// </summary>
        [Dependency]
        public IModulesDb ModulesDb { private get; set; }

        /// <summary>
        /// 中文：授权并解析模块与 Tab 的归属关系，在首次请求绑定设置。
        ///
        /// English: Authorizes and resolves module-to-Tab ownership, then binds settings on the initial request.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            if (!IsPostBack)
            {
                BindData();
            }
        }

        /// <summary>
        /// 中文：校验并保存当前模块实例的标题、缓存、编辑角色和移动端显示设置。
        ///
        /// English: Validates and saves the current module instance title, cache, edit roles, and mobile-display setting.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void ApplyChanges_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            string title;
            int cacheSeconds;
            if (!PortalAdministrationPolicy.TryNormalizeRequiredSingleLineText(moduleTitle.Text, 150, out title) ||
                !int.TryParse(cacheTime.Text, out cacheSeconds) || cacheSeconds < 0 || cacheSeconds > MaximumCacheSeconds)
            {
                ShowMessage("模块名称或缓存秒数无效，未保存本次修改。");
                return;
            }

            try
            {
                string editRoles = PortalRoleParser.Join(
                    authEditRoles.Items.Cast<ListItem>()
                        .Where(item => item.Selected)
                        .Select(item => item.Text));
                ModulesDb.UpdateModule(
                    moduleId,
                    currentModule.ModuleOrder,
                    currentModule.PaneName,
                    title,
                    cacheSeconds,
                    editRoles,
                    showMobile.Checked);
                PortalOperationAudit.Record(
                    "ModuleAdministration",
                    "Update",
                    "Module",
                    moduleId.ToString(),
                    "Updated module instance settings.",
                    Context);
                PortalNavigationPolicy.RedirectToSafeReturnUrl(
                    Context,
                    ResolveUrl("~/Admin/TabLayout.aspx?tabid=" + tabId));
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.ModuleSettings.Apply",
                    "Updating module settings failed. ModuleId=" + moduleId + "; TabId=" + tabId,
                    exception,
                    Context);
                ShowMessage("模块设置保存失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        private bool TryInitializeRequest()
        {
            if (!PortalAuthorization.EnsureAdmin(Context) ||
                !PortalNavigationPolicy.TryReadPositiveInt32(Request.Params["mid"], out moduleId) ||
                !PortalNavigationPolicy.TryReadPositiveInt32(Request.Params["tabid"], out tabId))
            {
                if (PortalAuthorization.IsAdmin())
                {
                    PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                }

                return false;
            }

            PortalSettings portalSettings = PortalContext.GetPortalSettings();
            ITabItem targetTab = portalSettings.DesktopTabs.FirstOrDefault(tab => tab.TabId == tabId);
            currentModule = targetTab == null || portalSettings.ActiveTab == null ||
                            portalSettings.ActiveTab.TabId != tabId
                ? null
                : portalSettings.ActiveTab.Modules.FirstOrDefault(module => module.ModuleId == moduleId);
            if (currentModule != null)
            {
                return true;
            }

            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        private void BindData()
        {
            moduleTitle.Text = currentModule.ModuleTitle;
            cacheTime.Text = currentModule.CacheTime.ToString();
            showMobile.Checked = currentModule.ShowMobile;
            PopulateRoleList(
                PortalRoleParser.Parse(currentModule.AuthorizedEditRoles),
                RolesDb.GetPortalRoles(PortalContext.GetPortalSettings().PortalId));
        }

        private void PopulateRoleList(string[] authorizedRoles, IEnumerable<IRoleItem> roles)
        {
            authEditRoles.Items.Clear();
            var allItem = new ListItem(PortalRoleNames.AllUsers, PortalRoleNames.AllUsers)
            {
                Selected = authorizedRoles.Any(role =>
                    string.Equals(role, PortalRoleNames.AllUsers, StringComparison.OrdinalIgnoreCase))
            };
            authEditRoles.Items.Add(allItem);

            foreach (IRoleItem role in roles)
            {
                var item = new ListItem(role.RoleName, role.RoleId.ToString())
                {
                    Selected = authorizedRoles.Any(authorizedRole =>
                        string.Equals(authorizedRole, role.RoleName, StringComparison.OrdinalIgnoreCase))
                };
                authEditRoles.Items.Add(item);
            }
        }

        private void ShowMessage(string message)
        {
            Message.Text = Server.HtmlEncode(message ?? string.Empty);
        }
    }
}
