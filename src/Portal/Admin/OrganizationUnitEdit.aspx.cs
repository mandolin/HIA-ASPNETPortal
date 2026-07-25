using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>组织单元后台最小维护页面。</zh-CN>
    ///   <en>Minimal administration maintenance page for organization units.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P6.3-S4 只允许新增和编辑组织单元，不提供硬删除、导入、导出或批量同步。父级存在性、自引用和循环关系在数据层再次校验。</zh-CN>
    ///   <en>P6.3-S4 allows only creation and editing of organization units. It provides no hard delete, import, export, or batch synchronization. Parent existence, self-parenting, and cycles are revalidated by the data layer.</en>
    /// </lang>
    /// </remarks>
    public partial class OrganizationUnitEdit : PortalPage<OrganizationUnitEdit>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>员工组织后台写入服务。</zh-CN>
        ///   <en>Employee-directory administration write service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IEmployeeDirectoryAdminDb EmployeeDirectoryAdminDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工组织只读目录服务，用于父级下拉框。</zh-CN>
        ///   <en>Read-only employee-directory service used by the parent selector.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IEmployeeDirectoryDb EmployeeDirectoryDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化组织单元维护页。</zh-CN>
        ///   <en>Initializes the organization-unit maintenance page.</en>
        /// </lang>
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>组织维护属于员工目录写权限范围；权限失败时统一 helper 已负责跳转，页面不再继续绑定表单。</zh-CN>
            //   <en>Organization maintenance belongs to employee-directory edit permission; when authorization fails, the shared helper redirects and the form is not bound.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.EmployeeDirectoryEdit))
            {
                return;
            }

            if (!IsPostBack)
            {
                BindForm();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>保存组织单元新增或编辑结果。</zh-CN>
        ///   <en>Saves organization-unit creation or editing changes.</en>
        /// </lang>
        /// </summary>
        protected void SaveButton_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>保存动作再次检查权限，避免管理员在页面加载后被撤权仍能通过旧表单提交。</zh-CN>
            //   <en>The save action checks permission again so an administrator whose permission changed after page load cannot submit an old form.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.EmployeeDirectoryEdit))
            {
                return;
            }

            if (EmployeeDirectoryAdminDb == null || !EmployeeDirectoryAdminDb.IsSchemaAvailable())
            {
                ShowMessage("P6.3 schema is unavailable.");
                return;
            }

            OrganizationUnitSaveRequest request;
            string validationMessage;
            if (!TryCreateSaveRequest(out request, out validationMessage))
            {
                ShowMessage(validationMessage);
                return;
            }

            bool isNew = request.OrganizationUnitId <= 0;
            try
            {
                // <lang>
                //   <zh-CN>数据层负责父级存在性、自引用、循环关系和并发时间戳校验；页面只拼装请求和展示低敏结果。</zh-CN>
                //   <en>The data layer owns parent existence, self-parenting, cycle and concurrency timestamp validation; the page only builds the request and displays low-sensitivity results.</en>
                // </lang>
                EmployeeDirectoryWriteResult result = EmployeeDirectoryAdminDb.SaveOrganizationUnit(request);
                if (!result.Succeeded)
                {
                    ShowMessage(result.Message);
                    return;
                }

                PortalOperationAudit.Record(
                    PortalOperationAuditEvents.EnterpriseDirectoryCategory,
                    isNew
                        ? PortalOperationAuditEvents.OrganizationUnitCreated
                        : PortalOperationAuditEvents.OrganizationUnitUpdated,
                    PortalOperationAuditEvents.OrganizationUnitTargetType,
                    result.EntityId.ToString(CultureInfo.InvariantCulture),
                    isNew ? "Created organization unit metadata." : "Updated organization unit metadata.",
                    Context);
                RedirectToDirectory();
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>异常详情进入结构化诊断日志；浏览器只看到事件编号，便于管理员定位而不暴露数据库细节。</zh-CN>
                //   <en>Exception details go to structured diagnostics; the browser sees only the event id so administrators can locate it without exposing database details.</en>
                // </lang>
                string eventId = PortalDiagnostics.Error(
                    "Admin.OrganizationUnitEdit.Save",
                    "Saving organization unit failed. OrganizationUnitId=" + request.OrganizationUnitId,
                    exception,
                    Context);
                ShowMessage("Organization unit save failed. Event id: " + eventId);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定组织单元编辑表单初始状态。</zh-CN>
        ///   <en>Binds the initial state of the organization-unit edit form.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>新增路径使用安全默认值；编辑路径加载现有组织、父级选择和并发时间戳。</zh-CN>
        ///   <en>The creation path uses safe defaults; the editing path loads the existing organization, parent selection and concurrency timestamp.</en>
        /// </lang>
        /// </remarks>
        private void BindForm()
        {
            if (EmployeeDirectoryAdminDb == null || EmployeeDirectoryDb == null || !EmployeeDirectoryAdminDb.IsSchemaAvailable())
            {
                DisableForm("P6.3 schema is unavailable. Run the employee organization SQL scripts before editing.");
                return;
            }

            int organizationUnitId;
            if (!TryReadOrganizationUnitId(out organizationUnitId))
            {
                return;
            }

            BindParentList(organizationUnitId);
            if (organizationUnitId <= 0)
            {
                // <lang>
                //   <zh-CN>新增组织不自动生成编码或名称，避免页面层创造业务标识；管理员必须明确录入。</zh-CN>
                //   <en>New organizations do not receive generated codes or names at the page layer; administrators must enter business identifiers explicitly.</en>
                // </lang>
                TitleLabel.Text = "New Organization Unit";
                OrganizationUnitIdField.Value = "0";
                OriginalUpdatedUtcField.Value = string.Empty;
                SortOrderTextBox.Text = "0";
                IsActiveCheckBox.Checked = true;
                return;
            }

            // <lang>
            //   <zh-CN>编辑路径把 `UpdatedUtc` 写入隐藏域，保存时用于检测其他管理员是否已修改同一组织。</zh-CN>
            //   <en>The edit path stores `UpdatedUtc` in a hidden field so save can detect whether another administrator changed the same organization.</en>
            // </lang>
            IOrganizationUnitInfo organization = EmployeeDirectoryAdminDb.GetOrganizationUnitById(organizationUnitId);
            if (organization == null)
            {
                DisableForm("Organization unit was not found.");
                return;
            }

            TitleLabel.Text = "Edit Organization Unit: " + Server.HtmlEncode(organization.DisplayName);
            OrganizationUnitIdField.Value = organization.OrganizationUnitId.ToString(CultureInfo.InvariantCulture);
            OriginalUpdatedUtcField.Value = FormatRoundTripUtc(organization.UpdatedUtc);
            OrganizationCodeTextBox.Text = organization.OrganizationCode;
            DisplayNameTextBox.Text = organization.DisplayName;
            SortOrderTextBox.Text = organization.SortOrder.ToString(CultureInfo.InvariantCulture);
            IsActiveCheckBox.Checked = organization.IsActive;
            SelectListValue(
                ParentOrganizationList,
                organization.ParentOrganizationUnitId.HasValue
                    ? organization.ParentOrganizationUnitId.Value.ToString(CultureInfo.InvariantCulture)
                    : string.Empty);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定父级组织下拉框。</zh-CN>
        ///   <en>Binds the parent-organization selector.</en>
        /// </lang>
        /// </summary>
        /// <param name="currentOrganizationUnitId">
        /// <l>
        ///   <zh-CN>当前编辑组织标识；用于从父级候选中排除自身。</zh-CN>
        ///   <en>Current organization identifier used to exclude itself from parent candidates.</en>
        /// </l>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>下拉框包含停用组织，方便修复历史树结构；真正的循环校验仍由数据层完成。</zh-CN>
        ///   <en>The selector includes inactive organizations so historical tree structure can be repaired; real cycle validation still happens in the data layer.</en>
        /// </lang>
        /// </remarks>
        private void BindParentList(int currentOrganizationUnitId)
        {
            ParentOrganizationList.Items.Clear();
            ParentOrganizationList.Items.Add(new ListItem("(root)", string.Empty));

            IList<IOrganizationUnitInfo> organizations = EmployeeDirectoryDb.GetOrganizationUnits(new EmployeeDirectoryQuery
            {
                IncludeInactiveOrganizations = true,
                Take = 500
            }).ToList();

            foreach (IOrganizationUnitInfo organization in organizations)
            {
                if (organization.OrganizationUnitId == currentOrganizationUnitId)
                {
                    continue;
                }

                ParentOrganizationList.Items.Add(new ListItem(
                    organization.DisplayName + " (#" + organization.OrganizationUnitId.ToString(CultureInfo.InvariantCulture) + ")",
                    organization.OrganizationUnitId.ToString(CultureInfo.InvariantCulture)));
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从表单输入创建组织单元保存请求。</zh-CN>
        ///   <en>Creates an organization-unit save request from form input.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>校验通过时输出的保存请求。</zh-CN>
        ///   <en>Save request emitted when validation succeeds.</en>
        /// </l>
        /// </param>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>校验失败时可展示给管理员的低敏提示。</zh-CN>
        ///   <en>Low-sensitivity message displayable to administrators when validation fails.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>输入可转换为保存请求时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when input can be converted into a save request.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>页面层只做格式和基本范围校验；组织编码唯一性、父级规则和并发冲突由数据层处理。</zh-CN>
        ///   <en>The page layer validates only format and basic ranges; organization-code uniqueness, parent rules and concurrency conflicts are handled by the data layer.</en>
        /// </lang>
        /// </remarks>
        private bool TryCreateSaveRequest(out OrganizationUnitSaveRequest request, out string message)
        {
            request = null;
            message = string.Empty;

            int organizationUnitId;
            if (!int.TryParse(OrganizationUnitIdField.Value, NumberStyles.None, CultureInfo.InvariantCulture, out organizationUnitId) ||
                organizationUnitId < 0)
            {
                message = "Organization unit id is invalid.";
                return false;
            }

            int? parentOrganizationUnitId;
            if (!TryReadOptionalListInt32(ParentOrganizationList.SelectedValue, out parentOrganizationUnitId))
            {
                message = "Parent organization id is invalid.";
                return false;
            }

            int sortOrder;
            if (!int.TryParse(SortOrderTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out sortOrder))
            {
                message = "Sort order must be an integer.";
                return false;
            }

            DateTime? originalUpdatedUtc;
            if (!TryReadOriginalUpdatedUtc(organizationUnitId, OriginalUpdatedUtcField.Value, out originalUpdatedUtc))
            {
                message = "The edit timestamp is invalid. Reload before saving again.";
                return false;
            }

            request = new OrganizationUnitSaveRequest
            {
                OrganizationUnitId = organizationUnitId,
                ParentOrganizationUnitId = parentOrganizationUnitId,
                OrganizationCode = OrganizationCodeTextBox.Text,
                DisplayName = DisplayNameTextBox.Text,
                SortOrder = sortOrder,
                IsActive = IsActiveCheckBox.Checked,
                OriginalUpdatedUtc = originalUpdatedUtc,
                ActorName = GetCurrentActor()
            };
            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从请求参数读取组织单元标识。</zh-CN>
        ///   <en>Reads the organization-unit identifier from request parameters.</en>
        /// </lang>
        /// </summary>
        /// <param name="organizationUnitId">
        /// <l>
        ///   <zh-CN>解析出的组织标识；缺失时为 `0`，表示新增。</zh-CN>
        ///   <en>Parsed organization identifier; `0` when absent, meaning creation.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>参数缺失或合法时返回 <c>true</c>；非法时跳转到编辑拒绝页。</zh-CN>
        ///   <en><c>true</c> when the parameter is absent or valid; invalid values redirect to the edit-denied page.</en>
        /// </l>
        /// </returns>
        private bool TryReadOrganizationUnitId(out int organizationUnitId)
        {
            organizationUnitId = 0;
            string rawValue = Request.Params["organizationUnitId"];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (PortalNavigationPolicy.TryReadPositiveInt32(rawValue, out organizationUnitId))
            {
                return true;
            }

            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析可为空的下拉框整数值。</zh-CN>
        ///   <en>Parses an optional integer value from a selector.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>下拉框提交值。</zh-CN>
        ///   <en>Posted selector value.</en>
        /// </l>
        /// </param>
        /// <param name="parsedValue">
        /// <l>
        ///   <zh-CN>解析后的正整数；空值表示根组织。</zh-CN>
        ///   <en>Parsed positive integer; empty input means a root organization.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>值为空或为正整数时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the value is empty or a positive integer.</en>
        /// </l>
        /// </returns>
        private static bool TryReadOptionalListInt32(string value, out int? parsedValue)
        {
            parsedValue = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            int integerValue;
            if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out integerValue) && integerValue > 0)
            {
                parsedValue = integerValue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析编辑路径的原始更新时间戳。</zh-CN>
        ///   <en>Parses the original update timestamp used by the editing path.</en>
        /// </lang>
        /// </summary>
        /// <param name="entityId">
        /// <l>
        ///   <zh-CN>组织标识；新增路径不需要并发时间戳。</zh-CN>
        ///   <en>Organization identifier; creation path does not require a concurrency timestamp.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>隐藏域中的 round-trip 时间戳。</zh-CN>
        ///   <en>Round-trip timestamp from the hidden field.</en>
        /// </l>
        /// </param>
        /// <param name="parsedValue">
        /// <l>
        ///   <zh-CN>解析出的原始更新时间。</zh-CN>
        ///   <en>Parsed original update timestamp.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>新增路径或时间戳合法时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> for creation path or a valid timestamp.</en>
        /// </l>
        /// </returns>
        private static bool TryReadOriginalUpdatedUtc(int entityId, string value, out DateTime? parsedValue)
        {
            parsedValue = null;
            if (entityId <= 0)
            {
                return true;
            }

            DateTime dateTime;
            if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out dateTime))
            {
                parsedValue = dateTime;
                return true;
            }

            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>禁用表单并显示不可编辑原因。</zh-CN>
        ///   <en>Disables the form and displays why editing is unavailable.</en>
        /// </lang>
        /// </summary>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>展示给管理员的低敏提示。</zh-CN>
        ///   <en>Low-sensitivity message displayed to administrators.</en>
        /// </l>
        /// </param>
        private void DisableForm(string message)
        {
            TitleLabel.Text = "Organization Unit";
            SaveButton.Enabled = false;
            OrganizationCodeTextBox.Enabled = false;
            DisplayNameTextBox.Enabled = false;
            ParentOrganizationList.Enabled = false;
            SortOrderTextBox.Enabled = false;
            IsActiveCheckBox.Enabled = false;
            ShowMessage(message);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>显示页面级提示。</zh-CN>
        ///   <en>Displays a page-level message.</en>
        /// </lang>
        /// </summary>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>提示文本；会在写入控件前做 HTML 编码。</zh-CN>
        ///   <en>Message text; HTML-encoded before being written to the control.</en>
        /// </l>
        /// </param>
        private void ShowMessage(string message)
        {
            MessageLabel.Text = Server.HtmlEncode(message ?? string.Empty);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回员工目录页。</zh-CN>
        ///   <en>Returns to the employee directory page.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>使用 `CompleteRequest` 避免 `Response.End` 产生线程中止异常，便于诊断真实保存错误。</zh-CN>
        ///   <en>`CompleteRequest` avoids the thread-abort exception caused by `Response.End`, keeping real save errors easier to diagnose.</en>
        /// </lang>
        /// </remarks>
        private void RedirectToDirectory()
        {
            Response.Redirect("EmployeeDirectory.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取当前审计操作者。</zh-CN>
        ///   <en>Gets the current audit actor.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前登录身份名称；缺失时使用 `admin` 作为旧后台兜底。</zh-CN>
        ///   <en>Current identity name; falls back to `admin` for the legacy admin path when missing.</en>
        /// </l>
        /// </returns>
        private string GetCurrentActor()
        {
            return Context.User == null || Context.User.Identity == null ||
                   string.IsNullOrWhiteSpace(Context.User.Identity.Name)
                ? "admin"
                : Context.User.Identity.Name;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按值选择下拉框项目。</zh-CN>
        ///   <en>Selects a drop-down item by value.</en>
        /// </lang>
        /// </summary>
        /// <param name="list">
        /// <l>
        ///   <zh-CN>目标下拉框。</zh-CN>
        ///   <en>Target drop-down list.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>要选择的值；缺失时不改变当前选择。</zh-CN>
        ///   <en>Value to select; current selection is unchanged when no item matches.</en>
        /// </l>
        /// </param>
        private static void SelectListValue(DropDownList list, string value)
        {
            ListItem item = list.Items.FindByValue(value ?? string.Empty);
            if (item == null)
            {
                return;
            }

            list.ClearSelection();
            item.Selected = true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>格式化并发控制使用的 round-trip UTC 时间。</zh-CN>
        ///   <en>Formats the round-trip UTC timestamp used for concurrency control.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>组织记录当前更新时间。</zh-CN>
        ///   <en>Current update timestamp of the organization record.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>round-trip 格式时间文本。</zh-CN>
        ///   <en>Round-trip formatted timestamp text.</en>
        /// </l>
        /// </returns>
        private static string FormatRoundTripUtc(DateTime value)
        {
            return value.ToString("O", CultureInfo.InvariantCulture);
        }
    }
}
