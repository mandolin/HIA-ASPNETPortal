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
    ///   <zh-CN>员工主数据后台最小维护页面。</zh-CN>
    ///   <en>Minimal administration maintenance page for employee master data.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P6.3-S4 只维护员工主数据，不启用员工工号登录，不处理账号绑定，也不保存手机号、身份证号等高敏资料。</zh-CN>
    ///   <en>P6.3-S4 maintains employee master data only. It does not enable employee-code sign-in, process account binding, or store highly sensitive data such as phone numbers or government identifiers.</en>
    /// </lang>
    /// </remarks>
    public partial class EmployeeEdit : PortalPage<EmployeeEdit>
    {
        private static readonly string[] DateTimeFormats = new[]
        {
            "yyyy-MM-dd",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss 'UTC'",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssZ",
            "O"
        };

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
        ///   <zh-CN>员工组织只读目录服务，用于组织下拉框。</zh-CN>
        ///   <en>Read-only employee-directory service used by the organization selector.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IEmployeeDirectoryDb EmployeeDirectoryDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化员工维护页。</zh-CN>
        ///   <en>Initializes the employee maintenance page.</en>
        /// </lang>
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>后台页先做统一权限门禁；权限失败时 helper 已负责跳转，剩余初始化不再继续执行。</zh-CN>
            //   <en>The admin page starts with the shared permission gate; when authorization fails, the helper redirects and the remaining initialization is skipped.</en>
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
        ///   <zh-CN>保存员工新增或编辑结果。</zh-CN>
        ///   <en>Saves employee creation or editing changes.</en>
        /// </lang>
        /// </summary>
        protected void SaveButton_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>保存动作再次复核权限，避免用户在页面加载后被撤权仍能通过旧表单提交。</zh-CN>
            //   <en>The save action checks permission again so a user whose permission changed after page load cannot submit an old form successfully.</en>
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

            EmployeeSaveRequest request;
            string validationMessage;
            if (!TryCreateSaveRequest(out request, out validationMessage))
            {
                ShowMessage(validationMessage);
                return;
            }

            bool isNew = request.EmployeeId <= 0;
            try
            {
                // <lang>
                //   <zh-CN>数据层负责唯一性、并发时间戳和状态规则；页面只根据低敏结果显示提示并写入运营审计。</zh-CN>
                //   <en>The data layer owns uniqueness, concurrency timestamp and status rules; the page only displays low-sensitivity results and records operational audit.</en>
                // </lang>
                EmployeeDirectoryWriteResult result = EmployeeDirectoryAdminDb.SaveEmployee(request);
                if (!result.Succeeded)
                {
                    ShowMessage(result.Message);
                    return;
                }

                PortalOperationAudit.Record(
                    PortalOperationAuditEvents.EnterpriseDirectoryCategory,
                    isNew
                        ? PortalOperationAuditEvents.EmployeeCreated
                        : PortalOperationAuditEvents.EmployeeUpdated,
                    PortalOperationAuditEvents.EmployeeTargetType,
                    result.EntityId.ToString(CultureInfo.InvariantCulture),
                    isNew ? "Created employee master data." : "Updated employee master data.",
                    Context);
                RedirectToDirectory();
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>异常详情进入结构化诊断日志；页面只展示事件编号，避免把数据库或路径细节暴露给浏览器。</zh-CN>
                //   <en>Exception details go to structured diagnostics; the page shows only the event id so database or path details are not exposed to the browser.</en>
                // </lang>
                string eventId = PortalDiagnostics.Error(
                    "Admin.EmployeeEdit.Save",
                    "Saving employee failed. EmployeeId=" + request.EmployeeId,
                    exception,
                    Context);
                ShowMessage("Employee save failed. Event id: " + eventId);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定员工编辑表单的初始状态。</zh-CN>
        ///   <en>Binds the initial state of the employee edit form.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>此方法同时处理新增和编辑两条路径：新增时填入安全默认值；编辑时加载现有员工和并发时间戳。</zh-CN>
        ///   <en>This method handles both creation and editing paths: creation fills safe defaults, while editing loads the existing employee and concurrency timestamp.</en>
        /// </lang>
        /// </remarks>
        private void BindForm()
        {
            if (EmployeeDirectoryAdminDb == null || EmployeeDirectoryDb == null || !EmployeeDirectoryAdminDb.IsSchemaAvailable())
            {
                DisableForm("P6.3 schema is unavailable. Run the employee organization SQL scripts before editing.");
                return;
            }

            int employeeId;
            if (!TryReadEmployeeId(out employeeId))
            {
                return;
            }

            BindOrganizationList();
            BindStatusList();
            if (employeeId <= 0)
            {
                // <lang>
                //   <zh-CN>新增员工时不生成工号或姓名默认值，避免页面层创造业务标识；实际内容由管理员明确输入。</zh-CN>
                //   <en>New employees do not receive generated employee-code or name defaults at the page layer; administrators must enter business identifiers explicitly.</en>
                // </lang>
                TitleLabel.Text = "New Employee";
                EmployeeIdField.Value = "0";
                OriginalUpdatedUtcField.Value = string.Empty;
                SourceSystemTextBox.Text = "Portal";
                SelectListValue(EmploymentStatusList, PortalEmployeeStatuses.Active);
                return;
            }

            // <lang>
            //   <zh-CN>编辑路径把 `UpdatedUtc` 以 round-trip 格式写入隐藏域，保存时用于数据层并发保护。</zh-CN>
            //   <en>The edit path writes `UpdatedUtc` into a hidden field in round-trip format so the data layer can enforce concurrency protection during save.</en>
            // </lang>
            IEmployeeInfo employee = EmployeeDirectoryAdminDb.GetEmployeeById(employeeId);
            if (employee == null)
            {
                DisableForm("Employee was not found.");
                return;
            }

            TitleLabel.Text = "Edit Employee: " + Server.HtmlEncode(employee.DisplayName);
            EmployeeIdField.Value = employee.EmployeeId.ToString(CultureInfo.InvariantCulture);
            OriginalUpdatedUtcField.Value = FormatRoundTripUtc(employee.UpdatedUtc);
            EmployeeCodeTextBox.Text = employee.EmployeeCode;
            DisplayNameTextBox.Text = employee.DisplayName;
            PreferredNameTextBox.Text = employee.PreferredName;
            WorkEmailTextBox.Text = employee.WorkEmail;
            SelectListValue(
                OrganizationUnitList,
                employee.OrganizationUnitId.HasValue
                    ? employee.OrganizationUnitId.Value.ToString(CultureInfo.InvariantCulture)
                    : string.Empty);
            SelectListValue(EmploymentStatusList, employee.EmploymentStatus);
            JoinedUtcTextBox.Text = FormatOptionalUtc(employee.JoinedUtc);
            LeftUtcTextBox.Text = FormatOptionalUtc(employee.LeftUtc);
            SourceSystemTextBox.Text = string.IsNullOrWhiteSpace(employee.SourceSystem) ? "Portal" : employee.SourceSystem;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定组织下拉框。</zh-CN>
        ///   <en>Binds the organization selector.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>后台维护允许选择停用组织，以便修复历史员工记录；列表限制为 500 条，避免旧 Web Forms 控件过重。</zh-CN>
        ///   <en>Administration allows inactive organizations so historical employee records can be repaired; the list is capped at 500 items to keep the legacy Web Forms control light.</en>
        /// </lang>
        /// </remarks>
        private void BindOrganizationList()
        {
            OrganizationUnitList.Items.Clear();
            OrganizationUnitList.Items.Add(new ListItem("(none)", string.Empty));

            IList<IOrganizationUnitInfo> organizations = EmployeeDirectoryDb.GetOrganizationUnits(new EmployeeDirectoryQuery
            {
                IncludeInactiveOrganizations = true,
                Take = 500
            }).ToList();

            foreach (IOrganizationUnitInfo organization in organizations)
            {
                OrganizationUnitList.Items.Add(new ListItem(
                    organization.DisplayName + " (#" + organization.OrganizationUnitId.ToString(CultureInfo.InvariantCulture) + ")",
                    organization.OrganizationUnitId.ToString(CultureInfo.InvariantCulture)));
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定员工状态下拉框。</zh-CN>
        ///   <en>Binds the employee-status selector.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>状态值使用稳定常量，保持数据库、权限判断和审计展示一致。</zh-CN>
        ///   <en>Status values use stable constants so database records, permission decisions and audit display stay aligned.</en>
        /// </lang>
        /// </remarks>
        private void BindStatusList()
        {
            EmploymentStatusList.Items.Clear();
            EmploymentStatusList.Items.Add(new ListItem(PortalEmployeeStatuses.Active, PortalEmployeeStatuses.Active));
            EmploymentStatusList.Items.Add(new ListItem(PortalEmployeeStatuses.Pending, PortalEmployeeStatuses.Pending));
            EmploymentStatusList.Items.Add(new ListItem(PortalEmployeeStatuses.Suspended, PortalEmployeeStatuses.Suspended));
            EmploymentStatusList.Items.Add(new ListItem(PortalEmployeeStatuses.Left, PortalEmployeeStatuses.Left));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从表单输入创建员工保存请求。</zh-CN>
        ///   <en>Creates an employee save request from form input.</en>
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
        ///   <zh-CN>页面层只做格式和基本范围校验；业务唯一性、员工状态组合和并发冲突由数据层统一处理。</zh-CN>
        ///   <en>The page layer validates only format and basic ranges; business uniqueness, employee-status combinations and concurrency conflicts are handled by the data layer.</en>
        /// </lang>
        /// </remarks>
        private bool TryCreateSaveRequest(out EmployeeSaveRequest request, out string message)
        {
            request = null;
            message = string.Empty;

            int employeeId;
            if (!int.TryParse(EmployeeIdField.Value, NumberStyles.None, CultureInfo.InvariantCulture, out employeeId) ||
                employeeId < 0)
            {
                message = "Employee id is invalid.";
                return false;
            }

            int? organizationUnitId;
            if (!TryReadOptionalListInt32(OrganizationUnitList.SelectedValue, out organizationUnitId))
            {
                message = "Organization id is invalid.";
                return false;
            }

            DateTime? joinedUtc;
            if (!TryReadOptionalUtc(JoinedUtcTextBox.Text, out joinedUtc))
            {
                message = "Joined UTC must use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss.";
                return false;
            }

            DateTime? leftUtc;
            if (!TryReadOptionalUtc(LeftUtcTextBox.Text, out leftUtc))
            {
                message = "Left UTC must use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss.";
                return false;
            }

            DateTime? originalUpdatedUtc;
            if (!TryReadOriginalUpdatedUtc(employeeId, OriginalUpdatedUtcField.Value, out originalUpdatedUtc))
            {
                message = "The edit timestamp is invalid. Reload before saving again.";
                return false;
            }

            request = new EmployeeSaveRequest
            {
                EmployeeId = employeeId,
                EmployeeCode = EmployeeCodeTextBox.Text,
                DisplayName = DisplayNameTextBox.Text,
                PreferredName = PreferredNameTextBox.Text,
                WorkEmail = WorkEmailTextBox.Text,
                OrganizationUnitId = organizationUnitId,
                EmploymentStatus = EmploymentStatusList.SelectedValue,
                JoinedUtc = joinedUtc,
                LeftUtc = leftUtc,
                SourceSystem = SourceSystemTextBox.Text,
                OriginalUpdatedUtc = originalUpdatedUtc,
                ActorName = GetCurrentActor()
            };
            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从请求参数读取员工标识。</zh-CN>
        ///   <en>Reads the employee identifier from request parameters.</en>
        /// </lang>
        /// </summary>
        /// <param name="employeeId">
        /// <l>
        ///   <zh-CN>解析出的员工标识；缺失时为 `0`，表示新增。</zh-CN>
        ///   <en>Parsed employee identifier; `0` when absent, meaning creation.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>参数缺失或合法时返回 <c>true</c>；非法时跳转到编辑拒绝页并返回 <c>false</c>。</zh-CN>
        ///   <en><c>true</c> when the parameter is absent or valid; invalid values redirect to edit denied and return <c>false</c>.</en>
        /// </l>
        /// </returns>
        private bool TryReadEmployeeId(out int employeeId)
        {
            employeeId = 0;
            string rawValue = Request.Params["employeeId"];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (PortalNavigationPolicy.TryReadPositiveInt32(rawValue, out employeeId))
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
        ///   <zh-CN>解析后的正整数；空值表示未选择组织。</zh-CN>
        ///   <en>Parsed positive integer; empty input means no organization selected.</en>
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
        ///   <zh-CN>解析可为空的 UTC 日期时间输入。</zh-CN>
        ///   <en>Parses an optional UTC date-time input.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>管理员输入的日期时间文本。</zh-CN>
        ///   <en>Date-time text entered by the administrator.</en>
        /// </l>
        /// </param>
        /// <param name="parsedValue">
        /// <l>
        ///   <zh-CN>解析出的 UTC 时间；空输入时为 <c>null</c>。</zh-CN>
        ///   <en>Parsed UTC value; <c>null</c> when input is empty.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>空值或支持格式的 UTC 时间返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> for empty input or supported UTC formats.</en>
        /// </l>
        /// </returns>
        private static bool TryReadOptionalUtc(string value, out DateTime? parsedValue)
        {
            parsedValue = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            DateTime dateTime;
            if (!DateTime.TryParseExact(
                value.Trim(),
                DateTimeFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out dateTime))
            {
                return false;
            }

            parsedValue = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析编辑路径的原始更新时间戳。</zh-CN>
        ///   <en>Parses the original update timestamp used by the editing path.</en>
        /// </lang>
        /// </summary>
        /// <param name="entityId">
        /// <l>
        ///   <zh-CN>员工标识；新增路径不需要并发时间戳。</zh-CN>
        ///   <en>Employee identifier; creation path does not require a concurrency timestamp.</en>
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
        ///   <en><c>true</c> for creation path or valid timestamp.</en>
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
            TitleLabel.Text = "Employee";
            SaveButton.Enabled = false;
            EmployeeCodeTextBox.Enabled = false;
            DisplayNameTextBox.Enabled = false;
            PreferredNameTextBox.Enabled = false;
            WorkEmailTextBox.Enabled = false;
            OrganizationUnitList.Enabled = false;
            EmploymentStatusList.Enabled = false;
            JoinedUtcTextBox.Enabled = false;
            LeftUtcTextBox.Enabled = false;
            SourceSystemTextBox.Enabled = false;
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
        ///   <zh-CN>使用 `CompleteRequest` 避免 `Response.End` 在线程上抛出中止异常，保持旧 Web Forms 流程更可诊断。</zh-CN>
        ///   <en>`CompleteRequest` avoids the thread-abort exception caused by `Response.End`, keeping the legacy Web Forms flow easier to diagnose.</en>
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
        ///   <zh-CN>格式化可为空的 UTC 时间供页面编辑。</zh-CN>
        ///   <en>Formats an optional UTC value for page editing.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>可为空的 UTC 时间。</zh-CN>
        ///   <en>Optional UTC value.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>`yyyy-MM-dd HH:mm:ss` 格式文本；空值返回空字符串。</zh-CN>
        ///   <en>`yyyy-MM-dd HH:mm:ss` text, or an empty string for null values.</en>
        /// </l>
        /// </returns>
        private static string FormatOptionalUtc(DateTime? value)
        {
            return value.HasValue
                ? value.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                : string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>格式化并发控制使用的 round-trip UTC 时间。</zh-CN>
        ///   <en>Formats the round-trip UTC timestamp used for concurrency control.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>员工记录当前更新时间。</zh-CN>
        ///   <en>Current update timestamp of the employee record.</en>
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
