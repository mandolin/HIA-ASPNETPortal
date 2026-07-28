using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>旧门户模块实例设置页面。</zh-CN>
    ///   <en>Legacy Portal module-instance settings page.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>页面只允许管理员修改属于指定 Tab 的模块实例；缓存秒数保持 <c>0</c> 为不缓存的既有语义。</zh-CN>
    ///   <en>The page allows administrators to modify only a module instance that belongs to the specified Tab; cache timeout <c>0</c> retains its existing no-cache meaning.</en>
    /// </lang>
    /// </remarks>
    public partial class ModuleSettingsPage : PortalPage<ModuleSettingsPage>
    {
        private const int MaximumCacheSeconds = 86400;
        private int moduleId;
        private int tabId;
        private ModuleSettings currentModule;

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前门户角色查询依赖。</zh-CN>
        ///   <en>Current-Portal role-query dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IRolesDb RolesDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>模块实例数据访问依赖。</zh-CN>
        ///   <en>Module-instance data-access dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IModulesDb ModulesDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>授权并解析模块与 Tab 的归属关系，在首次请求绑定设置。</zh-CN>
        ///   <en>Authorizes and resolves module-to-Tab ownership, then binds settings on the initial request.</en>
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

            if (!IsPostBack)
            {
                BindData();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>校验并保存当前模块实例的标题、缓存、编辑角色和移动端显示设置。</zh-CN>
        ///   <en>Validates and saves the current module instance title, cache, edit roles, and mobile-display setting.</en>
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
        protected void ApplyChanges_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            // <lang>
            //   <zh-CN>先在页面层做轻量输入归一化，阻断空标题、换行标题和过大的缓存秒数进入旧存储过程。</zh-CN>
            //   <en>Performs lightweight page-level normalization first so empty titles, multiline titles, and excessive cache values never reach the legacy stored procedure.</en>
            // </lang>
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
                // <lang>
                //   <zh-CN>编辑角色仍沿用旧的分号串存储格式，保存前统一通过角色解析器拼接，避免页面控件自行拼字符串。</zh-CN>
                //   <en>Edit roles still use the legacy semicolon-delimited storage format, so the role parser owns joining instead of the page concatenating strings directly.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>模块设置影响页面运行时行为，成功后写运营审计，再回到当前 Tab 布局页确认结果。</zh-CN>
                //   <en>Module settings affect runtime page behavior, so successful saves write an operation audit before returning to the current Tab layout page.</en>
                // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化请求上下文，并确认当前模块确实属于当前 Tab。</zh-CN>
        ///   <en>Initializes request context and verifies that the current module truly belongs to the current Tab.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>授权、参数和模块归属均合法时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when authorization, parameters, and module ownership are all valid.</en>
        /// </l>
        /// </returns>
        private bool TryInitializeRequest()
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.PortalModulesEdit) ||
                !PortalNavigationPolicy.TryReadPositiveInt32(Request.Params["mid"], out moduleId) ||
                !PortalNavigationPolicy.TryReadPositiveInt32(Request.Params["tabid"], out tabId))
            {
                if (PortalAuthorization.HasPermission(PortalPermissionKeys.PortalModulesEdit))
                {
                    PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                }

                return false;
            }

            PortalSettings portalSettings = PortalContext.GetPortalSettings();
            ITabItem targetTab = portalSettings.DesktopTabs.FirstOrDefault(tab => tab.TabId == tabId);
            // <lang>
            //   <zh-CN>旧门户的 ActiveTab 由请求参数驱动；这里同时检查目标 Tab 和 ActiveTab，避免跨 Tab 构造 mid/tabid 操作其他模块。</zh-CN>
            //   <en>The legacy Portal drives ActiveTab from request parameters; checking both target Tab and ActiveTab prevents crafted mid/tabid pairs from editing another Tab's module.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>把当前模块设置和可选角色绑定到页面控件。</zh-CN>
        ///   <en>Binds the current module settings and selectable roles to page controls.</en>
        /// </lang>
        /// </summary>
        private void BindData()
        {
            moduleTitle.Text = currentModule.ModuleTitle;
            cacheTime.Text = currentModule.CacheTime.ToString();
            showMobile.Checked = currentModule.ShowMobile;
            PopulateRoleList(
                PortalRoleParser.Parse(currentModule.AuthorizedEditRoles),
                RolesDb.GetPortalRoles(PortalContext.GetPortalSettings().PortalId));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>重建编辑角色复选列表，并保持旧系统的 All Users 特殊角色语义。</zh-CN>
        ///   <en>Rebuilds the edit-role checkbox list while preserving the legacy All Users special-role semantics.</en>
        /// </lang>
        /// </summary>
        /// <param name="authorizedRoles">
        /// <l>
        ///   <zh-CN>当前模块已经授权的角色名集合。</zh-CN>
        ///   <en>Role names currently authorized for the module.</en>
        /// </l>
        /// </param>
        /// <param name="roles">
        /// <l>
        ///   <zh-CN>当前门户可配置角色集合。</zh-CN>
        ///   <en>Configurable roles for the current Portal.</en>
        /// </l>
        /// </param>
        private void PopulateRoleList(string[] authorizedRoles, IEnumerable<IRoleItem> roles)
        {
            authEditRoles.Items.Clear();
            // <lang>
            //   <zh-CN>All Users 在模块编辑权限中保留旧门户的公开编辑语义。细粒度权限迁移可能维护同名配置载体，但 RolesDb 不会把它作为可配置普通角色返回。</zh-CN>
            //   <en>All Users retains its legacy public-edit meaning for module permissions. Fine-grained permission migration may maintain a same-named configuration carrier, but RolesDb never returns it as a configurable regular role.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>向页面显示经过 HTML 编码的非敏感提示。</zh-CN>
        ///   <en>Displays an HTML-encoded non-sensitive page message.</en>
        /// </lang>
        /// </summary>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>待显示提示文本。</zh-CN>
        ///   <en>Message text to display.</en>
        /// </l>
        /// </param>
        private void ShowMessage(string message)
        {
            Message.Text = Server.HtmlEncode(message ?? string.Empty);
        }
    }
}
