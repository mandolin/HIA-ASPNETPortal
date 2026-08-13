using System;
using System.Linq;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>维护历史模块定义名称并执行受保护删除检查的 Legacy 页面。</zh-CN>
    ///   <en>Legacy page that maintains historical module-definition names and performs protected deletion checks.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>新模块定义必须在 <c>ModuleCatalog.aspx</c> 从已验证部署包登记。本页不再允许创建定义或修改桌面/移动入口，以避免恢复任意动态加载路径。</zh-CN>
    ///   <en>New module definitions must be registered from a validated deployment package in <c>ModuleCatalog.aspx</c>. This page no longer permits creating a definition or changing desktop/mobile entries, preventing arbitrary dynamic-load paths from returning.</en>
    /// </lang>
    /// </remarks>
    public partial class ModuleDefinitions : PortalPage<ModuleDefinitions>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>当前正在维护的模块定义标识。</zh-CN>
        ///   <en>The module-definition identifier currently being maintained.</en>
        /// </lang>
        /// </summary>
        private int defId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回门户页面时使用的可选 Tab 标识。</zh-CN>
        ///   <en>The optional Tab identifier used when returning to a portal page.</en>
        /// </lang>
        /// </summary>
        private int tabId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回门户页面时使用的可选 Tab 索引。</zh-CN>
        ///   <en>The optional Tab index used when returning to a portal page.</en>
        /// </lang>
        /// </summary>
        private int tabIndex;

        /// <summary>
        /// <lang>
        ///   <zh-CN>已通过权限和导航参数校验的当前模块定义快照。</zh-CN>
        ///   <en>The current module-definition snapshot after permission and navigation-parameter validation.</en>
        /// </lang>
        /// </summary>
        private IModuleDefinitionItem currentDefinition;

        /// <summary>
        /// <lang>
        ///   <zh-CN>旧模块定义数据服务，用于读取、更新名称及受保护删除。</zh-CN>
        ///   <en>Legacy module-definition data service used for reading, display-name updates, and protected deletion.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IModuleDefsDb ModuleDefConfig { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>模块实例数据服务，用于在删除前检查引用数量。</zh-CN>
        ///   <en>Module-instance data service used to check reference count before deletion.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IModulesDb ModulesConfig { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证管理员访问权，读取查询参数，并初始化历史模块定义信息。</zh-CN>
        ///   <en>Requires administrator access, reads query parameters, and initializes legacy module-definition information.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发页面加载的 Web Forms 事件源。</zh-CN>
        ///   <en>The Web Forms event source that triggered page loading.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>页面加载事件参数。</zh-CN>
        ///   <en>The page-load event arguments.</en>
        /// </l>
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>先执行统一权限与请求初始化；失败时立即结束，避免未验证的定义标识进入绑定或事件处理。</zh-CN>
            //   <en>Run the shared authorization and request initialization first; stop immediately on failure so an unverified definition id cannot reach binding or event handlers.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            // <lang>
            //   <zh-CN>仅首次请求绑定快照，保留 Web Forms 回发字段并避免重复读取数据库。</zh-CN>
            //   <en>Bind the snapshot only on the initial request, preserving Web Forms postback fields and avoiding a duplicate database read.</en>
            // </lang>
            if (!Page.IsPostBack)
            {
                BindDefinition();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>授权、验证导航参数，并绑定当前门户中存在的模块定义。</zh-CN>
        ///   <en>Authorizes the request, validates navigation parameters, and binds an existing module definition.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>请求可继续操作已验证定义时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the request may operate on a verified definition.</en>
        /// </l>
        /// </returns>
        private bool TryInitializeRequest()
        {
            // <lang>
            //   <zh-CN>模块定义编辑权限是本页所有读取、修改和删除操作的共同门禁。</zh-CN>
            //   <en>The module-definition edit permission is the shared gate for every read, update, and delete operation on this page.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.ModuleDefinitionEdit))
            {
                return false;
            }

            // <lang>
            //   <zh-CN>Tab 参数只用于受控返回地址；任一参数非法都不允许继续处理定义。</zh-CN>
            //   <en>Tab parameters are used only for a controlled return URL; any invalid value prevents further definition processing.</en>
            // </lang>
            if (!TryReadOptionalPositiveParameter("tabid", out tabId) ||
                !TryReadOptionalNonNegativeParameter("tabindex", out tabIndex))
            {
                return false;
            }

            // <lang>
            //   <zh-CN>从 Request.Params 读取兼容查询参数，以支持既有链接同时覆盖查询串和表单参数。</zh-CN>
            //   <en>Read the compatibility parameter from Request.Params so existing links can use either query-string or form input.</en>
            // </lang>
            string rawDefinitionId = Request.Params["defid"];
            if (string.IsNullOrWhiteSpace(rawDefinitionId))
            {
                // <lang>
                //   <zh-CN>缺少定义标识时回到模块目录，不把空标识解释为新建或默认对象。</zh-CN>
                //   <en>Return to the module catalog when the definition id is missing instead of treating an empty id as a create or default object.</en>
                // </lang>
                PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, ResolveUrl("~/Admin/ModuleCatalog.aspx"));
                return false;
            }

            // <lang>
            //   <zh-CN>正整数解析同时约束编辑对象范围，非法输入统一导向拒绝页。</zh-CN>
            //   <en>Positive-integer parsing constrains the editable object range, and invalid input is routed to the common access-denied page.</en>
            // </lang>
            if (!PortalNavigationPolicy.TryReadPositiveInt32(rawDefinitionId, out defId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            // <lang>
            //   <zh-CN>只从当前定义集合匹配快照，避免直接信任请求标识或跨租户读取不存在对象。</zh-CN>
            //   <en>Match a snapshot only from the current definition set, avoiding trust in the request id or reads of a non-existent object across boundaries.</en>
            // </lang>
            currentDefinition = ModuleDefConfig.GetModuleDefinitions()
                .FirstOrDefault(item => item.ModuleDefId == defId);
            if (currentDefinition != null)
            {
                return true;
            }

            // <lang>
            //   <zh-CN>已解析但不存在的定义同样视为不可编辑，防止后续保存或删除使用陈旧标识。</zh-CN>
            //   <en>A parsed but missing definition is also treated as non-editable, preventing later updates or deletes from using a stale id.</en>
            // </lang>
            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新历史模块定义的显示名称，并保留其已受控的路径。</zh-CN>
        ///   <en>Updates a legacy module-definition display name while preserving its controlled paths.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发更新的按钮事件源。</zh-CN>
        ///   <en>The button event source that triggered the update.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>按钮事件参数。</zh-CN>
        ///   <en>The button event arguments.</en>
        /// </l>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>新建请求会跳转目录页；已有定义只更新名称并写入运营审计，不重新验证或修改 DesktopSrc/MobileSrc。</zh-CN>
        ///   <en>A create request redirects to the catalog; an existing definition updates only its name and records an operations audit, without revalidating or changing DesktopSrc/MobileSrc.</en>
        /// </lang>
        /// </remarks>
        protected void UpdateBtn_Click(Object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>回发事件再次初始化请求，防止仅依赖首次加载时缓存的字段状态。</zh-CN>
            //   <en>Reinitialize the request for the postback event so it does not rely only on state cached during the initial load.</en>
            // </lang>
            if (!TryInitializeRequest() || !Page.IsValid)
            {
                return;
            }

            // <lang>
            //   <zh-CN>名称先经过单行、长度和规范化检查；无效输入不触碰数据服务。</zh-CN>
            //   <en>Validate and normalize the single-line name before touching the data service; invalid input performs no persistence.</en>
            // </lang>
            string friendlyName;
            if (!PortalAdministrationPolicy.TryNormalizeRequiredSingleLineText(FriendlyName.Text, 150, out friendlyName))
            {
                ShowMessage("模块定义名称无效，未保存本次修改。");
                return;
            }

            try
            {
                /*
                 * <lang>
                 *   <zh-CN>保留既有受控路径，避免通过 legacy 表单创建或变更任意动态加载入口。</zh-CN>
                 *   <en>Preserve existing controlled paths so the legacy form cannot create or change arbitrary dynamic-load entries.</en>
                 * </lang>
                 */
                ModuleDefConfig.UpdateModuleDefinition(
                    defId,
                    friendlyName,
                    currentDefinition.DesktopSourceFile,
                    currentDefinition.MobileSourceFile);
                // <lang>
                //   <zh-CN>审计只记录名称更新这一受限动作，路径保持来自已验证快照。</zh-CN>
                //   <en>Audit only the constrained name-update action while paths remain sourced from the verified snapshot.</en>
                // </lang>
                PortalOperationAudit.Record(
                    "ModuleDefinition",
                    "UpdateName",
                    "ModuleDefinition",
                    defId.ToString(),
                    "Updated the legacy module-definition display name.",
                    Context);
                PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, BuildPortalReturnUrl());
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>持久化异常转为带事件编号的低敏提示，详细异常留在诊断日志。</zh-CN>
                //   <en>Convert persistence failures into a low-sensitivity message with an event id while keeping details in diagnostics.</en>
                // </lang>
                string eventId = PortalDiagnostics.Error(
                    "Admin.ModuleDefinitions.Update",
                    "Updating a legacy module definition failed. ModuleDefinitionId=" + defId,
                    exception,
                    Context);
                ShowMessage("模块定义保存失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除未被模块实例引用的历史模块定义。</zh-CN>
        ///   <en>Deletes a legacy module definition that no module instance references.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发删除的按钮事件源。</zh-CN>
        ///   <en>The button event source that triggered deletion.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>按钮事件参数。</zh-CN>
        ///   <en>The button event arguments.</en>
        /// </l>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>引用数量大于零时拒绝删除，要求先禁用、迁移或显式清理实例。删除成功会写入运营审计；不会删除物理部署目录。</zh-CN>
        ///   <en>Deletion is refused when references exist, requiring instances to be disabled, migrated, or explicitly cleaned first. A successful deletion writes an operations audit and never deletes a physical deployment directory.</en>
        /// </lang>
        /// </remarks>
        protected void DeleteBtn_Click(Object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>删除事件同样重新执行授权和对象解析，避免陈旧页面状态绕过门禁。</zh-CN>
            //   <en>The delete event repeats authorization and object resolution so stale page state cannot bypass the gate.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            // <lang>
            //   <zh-CN>删除前统计实例引用，明确阻断仍被使用的定义。</zh-CN>
            //   <en>Count instance references before deletion and explicitly block definitions that are still in use.</en>
            // </lang>
            int instanceCount = ModulesConfig.GetModulesByModuleDefId(defId).Count();
            if (instanceCount > 0)
            {
                /*
                 * <lang>
                 *   <zh-CN>旧删除会级联清除业务模块数据；这里先阻断被引用定义，要求管理员先处理实例级影响。</zh-CN>
                 *   <en>Legacy deletion cascades into business module data, so referenced definitions are blocked here and administrators must handle instance-level impact first.</en>
                 * </lang>
                 */
                ShowMessage("该模块定义仍被 " + instanceCount + " 个模块实例使用。请先禁用、迁移或显式清理这些实例。");
                return;
            }

            try
            {
                ModuleDefConfig.DeleteModuleDefinition(defId);
                // <lang>
                //   <zh-CN>仅在删除调用成功后记录审计，避免把失败操作误报为完成。</zh-CN>
                //   <en>Record the audit only after the delete call succeeds, avoiding a false completion record.</en>
                // </lang>
                PortalOperationAudit.Record(
                    "ModuleDefinition",
                    "Delete",
                    "ModuleDefinition",
                    defId.ToString(),
                    "Deleted an unused legacy module definition.",
                    Context);
                PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, BuildPortalReturnUrl());
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>删除失败沿用统一诊断和低敏反馈边界，不向页面泄漏异常细节。</zh-CN>
                //   <en>Delete failures use the shared diagnostics and low-sensitivity feedback boundary without exposing exception details.</en>
                // </lang>
                string eventId = PortalDiagnostics.Error(
                    "Admin.ModuleDefinitions.Delete",
                    "Deleting a legacy module definition failed. ModuleDefinitionId=" + defId,
                    exception,
                    Context);
                ShowMessage("模块定义删除失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>取消当前编辑并返回指定门户 Tab。</zh-CN>
        ///   <en>Cancels the current edit and returns to the specified portal Tab.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>触发取消的按钮事件源。</zh-CN>
        ///   <en>The button event source that triggered cancellation.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>按钮事件参数。</zh-CN>
        ///   <en>The button event arguments.</en>
        /// </l>
        /// </param>
        protected void CancelBtn_Click(Object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>取消也需要验证当前请求，返回地址只能由已解析的 Tab 参数构造。</zh-CN>
            //   <en>Cancellation still validates the request, and the return URL can only be built from parsed Tab parameters.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, BuildPortalReturnUrl());
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>为历史表单保留的服务器端模块路径校验。</zh-CN>
        ///   <en>Server-side module-path validation retained for the legacy form.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>当前 UI 已将路径字段设为只读，新定义也重定向到目录页；保留此处理器是为了不破坏现有 ASPX 验证契约。</zh-CN>
        ///   <en>Current UI marks path fields read-only and redirects new definitions to the catalog; this handler remains so the existing ASPX validation contract is not broken.</en>
        /// </lang>
        /// </remarks>
        /// <param name="source">
        /// <l>
        ///   <zh-CN>验证器控件。</zh-CN>
        ///   <en>The validator control.</en>
        /// </l>
        /// </param>
        /// <param name="args">
        /// <l>
        ///   <zh-CN>包含待校验路径及验证结果的事件参数。</zh-CN>
        ///   <en>Event arguments containing the path to validate and the validation result.</en>
        /// </l>
        /// </param>
        protected void DesktopSrcPathValidator_ServerValidate(object source, ServerValidateEventArgs args)
        {
            // <lang>
            //   <zh-CN>保留验证器契约并复用统一路径策略；即使当前控件停用，也不恢复任意路径写入。</zh-CN>
            //   <en>Keep the validator contract while reusing the shared path policy; a disabled control must not restore arbitrary path writes.</en>
            // </lang>
            string normalizedSource;
            string errorMessage;
            args.IsValid = PortalModulePathValidator.TryNormalizeDesktopSource(args.Value, out normalizedSource, out errorMessage);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>规范化当前历史表单中的桌面入口，供保留的兼容调用点使用。</zh-CN>
        ///   <en>Normalizes the desktop entry in the current legacy form for retained compatibility call sites.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>已通过路径校验的站内相对入口。</zh-CN>
        ///   <en>Site-relative entry that passed path validation.</en>
        /// </l>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <l>
        ///   <zh-CN>路径不符合受限动态加载边界时抛出。</zh-CN>
        ///   <en>Thrown when the path does not meet the constrained dynamic-loading boundary.</en>
        /// </l>
        /// </exception>
        private string NormalizeDesktopSrc()
        {
            // <lang>
            //   <zh-CN>使用同一规范化策略处理兼容调用点，失败时转换为明确的操作异常。</zh-CN>
            //   <en>Use the same normalization policy for compatibility call sites and convert failure into an explicit operation exception.</en>
            // </lang>
            string normalizedSource;
            string errorMessage;
            if (!PortalModulePathValidator.TryNormalizeDesktopSource(DesktopSrc.Text, out normalizedSource, out errorMessage))
            {
                throw new InvalidOperationException(errorMessage);
            }

            return normalizedSource;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取可选正整数查询参数。</zh-CN>
        ///   <en>Reads an optional positive integer query parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="parameterName">
        /// <l>
        ///   <zh-CN>要读取的查询参数名称。</zh-CN>
        ///   <en>The query-parameter name to read.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>读取成功后的参数值；缺失时为 0。</zh-CN>
        ///   <en>The parsed value when reading succeeds, or 0 when the parameter is missing.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>参数缺失或格式合法时为 <c>true</c>；非法时已重定向到编辑拒绝页并返回 <c>false</c>。</zh-CN>
        ///   <en><c>true</c> when the parameter is missing or valid; invalid input redirects to the edit-denied page and returns <c>false</c>.</en>
        /// </l>
        /// </returns>
        private bool TryReadOptionalPositiveParameter(string parameterName, out int value)
        {
            value = 0;
            // <lang>
            //   <zh-CN>缺失参数保持兼容默认值 0；存在参数则必须通过正整数策略。</zh-CN>
            //   <en>Keep the compatibility default of 0 for a missing parameter; a supplied value must pass the positive-integer policy.</en>
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
            //   <zh-CN>非法导航参数统一拒绝，不让调用方继续使用不可信的返回上下文。</zh-CN>
            //   <en>Reject invalid navigation input consistently so callers cannot continue with an untrusted return context.</en>
            // </lang>
            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取可选非负整数查询参数。</zh-CN>
        ///   <en>Reads an optional non-negative integer query parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="parameterName">
        /// <l>
        ///   <zh-CN>要读取的查询参数名称。</zh-CN>
        ///   <en>The query-parameter name to read.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>读取成功后的参数值；缺失时为 0。</zh-CN>
        ///   <en>The parsed value when reading succeeds, or 0 when the parameter is missing.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>参数缺失或格式合法时为 <c>true</c>；非法时已重定向到编辑拒绝页并返回 <c>false</c>。</zh-CN>
        ///   <en><c>true</c> when the parameter is missing or valid; invalid input redirects to the edit-denied page and returns <c>false</c>.</en>
        /// </l>
        /// </returns>
        private bool TryReadOptionalNonNegativeParameter(string parameterName, out int value)
        {
            value = 0;
            // <lang>
            //   <zh-CN>索引参数允许零，但仍需在进入返回 URL 构造前完成非负整数校验。</zh-CN>
            //   <en>The index permits zero but must still be validated as non-negative before constructing a return URL.</en>
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
            //   <zh-CN>非法索引不降级为默认 Tab，直接进入编辑拒绝路径。</zh-CN>
            //   <en>Do not downgrade an invalid index to a default Tab; route it directly to the edit-denied path.</en>
            // </lang>
            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把当前模块定义快照绑定到历史编辑表单。</zh-CN>
        ///   <en>Binds the current module-definition snapshot to the legacy edit form.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>路径字段只读且验证器停用，是为了让页面只维护显示名；路径可信边界由模块目录与部署包机制维护。</zh-CN>
        ///   <en>Path fields are read-only and the validator is disabled so the page only maintains the display name; the trusted path boundary is maintained by the module catalog and deployment-package mechanism.</en>
        /// </lang>
        /// </remarks>
        private void BindDefinition()
        {
            // <lang>
            //   <zh-CN>从已验证快照投影名称和路径；路径只读，验证器停用以保持“仅改名称”的契约。</zh-CN>
            //   <en>Project the name and paths from the verified snapshot; paths stay read-only and validation is disabled to preserve the name-only contract.</en>
            // </lang>
            FriendlyName.Text = currentDefinition.FriendlyName;
            DesktopSrc.Text = currentDefinition.DesktopSourceFile;
            MobileSrc.Text = currentDefinition.MobileSourceFile;
            DesktopSrc.ReadOnly = true;
            MobileSrc.ReadOnly = true;
            Req2.Enabled = false;
            DesktopSrcPathValidator.Enabled = false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>构造受控的编辑完成返回地址。</zh-CN>
        ///   <en>Builds the controlled return URL after editing.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>指向指定门户 Tab 的站内地址；缺少 Tab 时返回门户首页。</zh-CN>
        ///   <en>An in-site URL to the specified portal Tab, or the portal home page when no Tab is supplied.</en>
        /// </l>
        /// </returns>
        private string BuildPortalReturnUrl()
        {
            // <lang>
            //   <zh-CN>只有正 Tab 标识才构造桌面页返回地址，其他情况回到固定门户首页。</zh-CN>
            //   <en>Build a desktop-page return URL only for a positive Tab id; otherwise return to the fixed portal home page.</en>
            // </lang>
            if (tabId > 0)
            {
                return ResolveUrl("~/DesktopDefault.aspx?tabindex=" + tabIndex + "&tabid=" + tabId);
            }

            return ResolveUrl("~/Default.aspx");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>以 HTML 编码方式显示后台低敏提示。</zh-CN>
        ///   <en>Displays a low-sensitivity administration message with HTML encoding.</en>
        /// </lang>
        /// </summary>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>要显示给管理员的提示文本。</zh-CN>
        ///   <en>The message text to display to the administrator.</en>
        /// </l>
        /// </param>
        private void ShowMessage(string message)
        {
            // <lang>
            //   <zh-CN>所有提示先 HTML 编码并将空值归一化，避免异常文本进入标记输出。</zh-CN>
            //   <en>HTML-encode every message and normalize null to an empty string before it reaches markup output.</en>
            // </lang>
            MessageLabel.Text = Server.HtmlEncode(message ?? string.Empty);
        }
    }
}
