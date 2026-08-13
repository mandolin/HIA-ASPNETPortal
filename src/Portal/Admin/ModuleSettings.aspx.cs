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
        /// <summary>
        /// <lang>
        ///   <zh-CN>允许保存的最大模块缓存秒数。</zh-CN>
        ///   <en>Maximum module-cache duration, in seconds, that may be saved.</en>
        /// </lang>
        /// </summary>
        private const int MaximumCacheSeconds = 86400;

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前通过请求和门户 Tab 归属校验的模块实例标识。</zh-CN>
        ///   <en>The module-instance identifier verified against the request and Portal Tab ownership.</en>
        /// </lang>
        /// </summary>
        private int moduleId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前通过请求校验的门户 Tab 标识，用于归属检查和安全回跳。</zh-CN>
        ///   <en>The Portal Tab identifier verified from the request for ownership checks and safe return navigation.</en>
        /// </lang>
        /// </summary>
        private int tabId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>已通过权限、参数和 Tab 归属校验的当前模块设置快照。</zh-CN>
        ///   <en>The current module-settings snapshot after permission, parameter, and Tab-ownership validation.</en>
        /// </lang>
        /// </summary>
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
            // <lang>
            //   <zh-CN>每次请求先重做权限与归属门禁，避免回发继续使用未验证的模块状态。</zh-CN>
            //   <en>Reapply authorization and ownership gates on every request so a postback cannot reuse unverified module state.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            // <lang>
            //   <zh-CN>仅首次请求绑定数据库快照，保留回发控件值供保存事件读取。</zh-CN>
            //   <en>Bind the database snapshot only on the initial request, preserving postback control values for the save event.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>保存事件再次解析请求和模块快照，拒绝未授权、跨 Tab 或失效对象的回发。</zh-CN>
            //   <en>Resolve the request and module snapshot again for save, rejecting unauthorized, cross-Tab, or stale postbacks.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>标题与缓存秒数共用一个输入门禁；任一失败都会在持久化前终止。</zh-CN>
            //   <en>Apply one input gate to the title and cache duration; any failure stops before persistence.</en>
            // </lang>
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
                // <lang>
                //   <zh-CN>使用已验证模块快照中的布局字段，仅替换允许编辑的标题、缓存、角色和移动端开关。</zh-CN>
                //   <en>Use layout fields from the verified module snapshot and replace only the editable title, cache, roles, and mobile flag.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>保存异常写入统一诊断并只向页面返回事件编号，避免泄漏存储层细节。</zh-CN>
                //   <en>Record save failures through shared diagnostics and return only an event id to the page, avoiding storage-layer detail leakage.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>权限与两个正整数参数构成统一入口门禁；非法参数不进入门户或数据读取。</zh-CN>
            //   <en>Permission and the two positive-integer parameters form the entry gate; invalid input never reaches Portal or data reads.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.PortalModulesEdit) ||
                !PortalNavigationPolicy.TryReadPositiveInt32(Request.Params["mid"], out moduleId) ||
                !PortalNavigationPolicy.TryReadPositiveInt32(Request.Params["tabid"], out tabId))
            {
                // <lang>
                //   <zh-CN>只有已具备编辑权限的请求才重定向到编辑拒绝页，匿名或无权请求沿用权限组件的处理。</zh-CN>
                //   <en>Redirect to edit-denied only after permission is known; anonymous or unauthorized requests retain the authorization component's handling.</en>
                // </lang>
                if (PortalAuthorization.HasPermission(PortalPermissionKeys.PortalModulesEdit))
                {
                    PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                }

                return false;
            }

            // <lang>
            //   <zh-CN>读取当前门户设置快照，用同一请求上下文完成 Tab 与 ActiveTab 归属核对。</zh-CN>
            //   <en>Read the current Portal settings snapshot and verify Tab ownership against the same request context.</en>
            // </lang>
            PortalSettings portalSettings = PortalContext.GetPortalSettings();
            // <lang>
            //   <zh-CN>仅从桌面 Tab 集合匹配请求的 Tab 标识，避免把任意数字当作有效容器。</zh-CN>
            //   <en>Match the requested Tab id only within desktop tabs so an arbitrary number cannot become a valid container.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>未找到同一 ActiveTab 下的模块实例时拒绝编辑，阻断伪造 mid/tabid 组合。</zh-CN>
            //   <en>Reject editing when the module is not found under the same ActiveTab, blocking forged mid/tabid pairs.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>把已验证快照投影到控件，并从当前门户读取可配置角色；不在绑定阶段执行写入。</zh-CN>
            //   <en>Project the verified snapshot into controls and read configurable roles from the current Portal; binding performs no writes.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>先清空旧回发列表，避免角色选项跨请求累积。</zh-CN>
            //   <en>Clear the old postback list first so role options cannot accumulate across requests.</en>
            // </lang>
            authEditRoles.Items.Clear();
            // <lang>
            //   <zh-CN>All Users 在模块编辑权限中保留旧门户的公开编辑语义。细粒度权限迁移可能维护同名配置载体，但 RolesDb 不会把它作为可配置普通角色返回。</zh-CN>
            //   <en>All Users retains its legacy public-edit meaning for module permissions. Fine-grained permission migration may maintain a same-named configuration carrier, but RolesDb never returns it as a configurable regular role.</en>
            // </lang>
            // <lang>
            //   <zh-CN>按不区分大小写匹配已授权集合，保留 All Users 的特殊公开编辑语义。</zh-CN>
            //   <en>Match the authorized set case-insensitively while preserving the special public-edit meaning of All Users.</en>
            // </lang>
            var allItem = new ListItem(PortalRoleNames.AllUsers, PortalRoleNames.AllUsers)
            {
                Selected = authorizedRoles.Any(role =>
                    string.Equals(role, PortalRoleNames.AllUsers, StringComparison.OrdinalIgnoreCase))
            };
            authEditRoles.Items.Add(allItem);

            // <lang>
            //   <zh-CN>把数据层提供的当前门户角色投影为可选项，只按角色名恢复选中状态。</zh-CN>
            //   <en>Project roles supplied by the data layer into selectable items, restoring selection by role name only.</en>
            // </lang>
            foreach (IRoleItem role in roles)
            {
                // <lang>
                //   <zh-CN>每个角色项使用稳定名称和标识，选中状态来自已验证授权集合。</zh-CN>
                //   <en>Each role item uses its stable name and id, with selection derived from the verified authorized set.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>消息统一 HTML 编码并将空值归一化，防止异常文本进入页面标记。</zh-CN>
            //   <en>HTML-encode every message and normalize null before it reaches page markup.</en>
            // </lang>
            Message.Text = Server.HtmlEncode(message ?? string.Empty);
        }
    }
}
