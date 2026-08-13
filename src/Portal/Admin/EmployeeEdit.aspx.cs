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
        /// <summary>
        /// <lang>
        ///   <zh-CN>页面层接受的日期时间输入格式；解析后统一转换为 UTC，不改变数据层时间戳契约。</zh-CN>
        ///   <en>Date-time formats accepted by the page; parsed values are normalized to UTC without changing the data-layer timestamp contract.</en>
        /// </lang>
        /// </summary>
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

            // <lang>
            //   <zh-CN>只在首次请求绑定表单，避免回发时覆盖管理员已输入但尚未保存的值。</zh-CN>
            //   <en>Bind the form only on the initial request so postbacks do not overwrite administrator input that has not been saved.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>保存前要求写入服务和 schema 同时可用；不可用时只显示固定提示，不触发半完成写入。</zh-CN>
            //   <en>Require both the write service and its schema before saving; when unavailable, show a fixed message without attempting a partial write.</en>
            // </lang>
            if (EmployeeDirectoryAdminDb == null || !EmployeeDirectoryAdminDb.IsSchemaAvailable())
            {
                ShowMessage("P6.3 schema is unavailable.");
                return;
            }

            // <lang>
            //   <zh-CN>请求对象只在所有页面级输入校验通过后生成；失败消息保持低敏且不回显原始正文。</zh-CN>
            //   <en>Create the request only after all page-level input checks pass; validation messages stay low-sensitivity and do not echo raw content.</en>
            // </lang>
            EmployeeSaveRequest request;
            string validationMessage;
            if (!TryCreateSaveRequest(out request, out validationMessage))
            {
                ShowMessage(validationMessage);
                return;
            }

            // <lang>
            //   <zh-CN>用请求标识区分新增和更新，供审计事件选择；不在页面层自行生成数据库标识。</zh-CN>
            //   <en>Use the request identifier to distinguish creation from update for audit selection; the page does not generate database identifiers.</en>
            // </lang>
            bool isNew = request.EmployeeId <= 0;
            try
            {
                // <lang>
                //   <zh-CN>数据层负责唯一性、并发时间戳和状态规则；页面只根据低敏结果显示提示并写入运营审计。</zh-CN>
                //   <en>The data layer owns uniqueness, concurrency timestamp and status rules; the page only displays low-sensitivity results and records operational audit.</en>
                // </lang>
                // <lang>
                //   <zh-CN>保存结果承载数据层的低敏成功/失败事实和实体标识，页面不推断唯一性或并发语义。</zh-CN>
                //   <en>The write result carries the data layer's low-sensitivity success/failure fact and entity identifier; the page does not infer uniqueness or concurrency semantics.</en>
                // </lang>
                EmployeeDirectoryWriteResult result = EmployeeDirectoryAdminDb.SaveEmployee(request);
                if (!result.Succeeded)
                {
                    // <lang>
                    //   <zh-CN>失败时保留数据层提示并停留在当前页，避免审计成功事件或错误回跳。</zh-CN>
                    //   <en>On failure, retain the data-layer message on the current page and avoid recording a success audit or redirecting.</en>
                    // </lang>
                    ShowMessage(result.Message);
                    return;
                }

                // <lang>
                //   <zh-CN>只有持久化成功后才写入创建/更新审计，并使用稳定实体类型和不变文化标识。</zh-CN>
                //   <en>Record the create/update audit only after persistence succeeds, using a stable entity type and invariant-culture identifier.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>事件编号是浏览器可见的低敏关联值，不承载异常正文或连接细节。</zh-CN>
                //   <en>The event id is a low-sensitivity correlation value visible to the browser and carries no exception body or connection detail.</en>
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

            // <lang>
            //   <zh-CN>员工标识来自站内请求参数；helper 统一处理缺失、新增、非法值和拒绝回跳。</zh-CN>
            //   <en>The employee identifier comes from an internal request parameter; the helper handles missing, creation, invalid values and denial redirect consistently.</en>
            // </lang>
            int employeeId;
            if (!TryReadEmployeeId(out employeeId))
            {
                return;
            }

            // <lang>
            //   <zh-CN>先绑定候选组织和稳定状态，再根据新增/编辑分支填充当前实体，保持选择器与表单值顺序稳定。</zh-CN>
            //   <en>Bind organization candidates and stable statuses before filling the current entity, keeping selector and form-value ordering stable for both paths.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>读取单个员工只用于低敏表单回填；不存在时禁用表单，不根据缺失记录创造新实体。</zh-CN>
            //   <en>Read the single employee only for low-sensitivity form hydration; when absent, disable the form instead of creating an entity from a missing record.</en>
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
            // <lang>
            //   <zh-CN>清空旧选项并保留空值哨兵，明确“无组织归属”与任意组织标识的区别。</zh-CN>
            //   <en>Clear old options and retain an empty sentinel so “no organization” is distinct from any organization identifier.</en>
            // </lang>
            OrganizationUnitList.Items.Clear();
            OrganizationUnitList.Items.Add(new ListItem("(none)", string.Empty));

            // <lang>
            //   <zh-CN>查询包含停用组织但限制最多 500 条；这是修复历史记录的候选集，不是权限或循环校验。</zh-CN>
            //   <en>Query inactive organizations with a 500-item cap; this is a historical-repair candidate set, not an authorization or cycle check.</en>
            // </lang>
            IList<IOrganizationUnitInfo> organizations = EmployeeDirectoryDb.GetOrganizationUnits(new EmployeeDirectoryQuery
            {
                IncludeInactiveOrganizations = true,
                Take = 500
            }).ToList();

            foreach (IOrganizationUnitInfo organization in organizations)
            {
                // <lang>
                //   <zh-CN>显示名称供管理员识别，值使用不变文化格式的稳定主键，避免本地化文本参与提交。</zh-CN>
                //   <en>Use the display name for administrator recognition and the stable key formatted invariantly as the submitted value, keeping localized text out of the identifier.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>重建状态选项而非依赖页面标记层，确保回发/模板变化不会引入未批准的状态值。</zh-CN>
            //   <en>Rebuild the status options instead of trusting markup, preventing postbacks or template changes from introducing unapproved status values.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>先清空输出，保证任何失败路径都不会把旧请求或旧提示泄漏给调用者。</zh-CN>
            //   <en>Clear outputs first so every failure path cannot leak a request or message from a previous invocation.</en>
            // </lang>
            request = null;
            message = string.Empty;

            // <lang>
            //   <zh-CN>员工标识只接受非负不变整数；0 保留给新增，正数表示已有实体。</zh-CN>
            //   <en>Accept only a non-negative invariant integer for the employee identifier; zero denotes creation and a positive value an existing entity.</en>
            // </lang>
            int employeeId;
            if (!int.TryParse(EmployeeIdField.Value, NumberStyles.None, CultureInfo.InvariantCulture, out employeeId) ||
                employeeId < 0)
            {
                message = "Employee id is invalid.";
                return false;
            }

            // <lang>
            //   <zh-CN>组织下拉框的空值映射为 null，正整数才进入保存请求，避免伪造负数或非数字归属。</zh-CN>
            //   <en>Map an empty organization selector to null and allow only positive integers into the request, rejecting forged negative or non-numeric ownership.</en>
            // </lang>
            int? organizationUnitId;
            if (!TryReadOptionalListInt32(OrganizationUnitList.SelectedValue, out organizationUnitId))
            {
                message = "Organization id is invalid.";
                return false;
            }

            // <lang>
            //   <zh-CN>入职和离职时间按统一 helper 解析为 UTC，页面不把本地时间假设写入请求。</zh-CN>
            //   <en>Parse joined and left times through the shared UTC helper so the page does not write local-time assumptions into the request.</en>
            // </lang>
            DateTime? joinedUtc;
            if (!TryReadOptionalUtc(JoinedUtcTextBox.Text, out joinedUtc))
            {
                message = "Joined UTC must use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss.";
                return false;
            }

            // <lang>
            //   <zh-CN>离职时间与入职时间使用同一格式边界，业务上的先后关系由数据层继续负责。</zh-CN>
            //   <en>Use the same format boundary for the leaving time; the data layer remains responsible for business ordering rules.</en>
            // </lang>
            DateTime? leftUtc;
            if (!TryReadOptionalUtc(LeftUtcTextBox.Text, out leftUtc))
            {
                message = "Left UTC must use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss.";
                return false;
            }

            // <lang>
            //   <zh-CN>编辑时间戳来自隐藏域，仅用于并发保护；新增路径允许为空。</zh-CN>
            //   <en>The edit timestamp comes from a hidden field only for concurrency protection; creation allows it to be empty.</en>
            // </lang>
            DateTime? originalUpdatedUtc;
            if (!TryReadOriginalUpdatedUtc(employeeId, OriginalUpdatedUtcField.Value, out originalUpdatedUtc))
            {
                message = "The edit timestamp is invalid. Reload before saving again.";
                return false;
            }

            // <lang>
            //   <zh-CN>最后组装完整请求，保留页面字段原值交给数据层做业务清理、唯一性和状态组合校验。</zh-CN>
            //   <en>Assemble the complete request last, leaving field cleanup, uniqueness and status-combination validation to the data layer.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>缺失参数代表新增，因此先把输出初始化为 0；该默认值不会访问数据库。</zh-CN>
            //   <en>A missing parameter means creation, so initialize the output to zero; this default does not access the database.</en>
            // </lang>
            employeeId = 0;
            // <lang>
            //   <zh-CN>只从当前请求参数读取实体标识，不使用未验证的控件文本替代路由输入。</zh-CN>
            //   <en>Read the entity identifier only from the current request parameter, rather than substituting unvalidated control text for route input.</en>
            // </lang>
            string rawValue = Request.Params["employeeId"];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (PortalNavigationPolicy.TryReadPositiveInt32(rawValue, out employeeId))
            {
                return true;
            }

            // <lang>
            //   <zh-CN>非法标识统一进入编辑拒绝路径，不向调用方透露解析细节或继续绑定实体。</zh-CN>
            //   <en>Route invalid identifiers to the shared edit-denied path without exposing parsing details or continuing entity binding.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>输出先设为空，区分“没有选择”与“提交了非法值”。</zh-CN>
            //   <en>Initialize the output to null so “no selection” remains distinct from “an invalid value was submitted.”</en>
            // </lang>
            parsedValue = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            // <lang>
            //   <zh-CN>使用不变文化解析正整数，避免本地化数字格式进入主键字段。</zh-CN>
            //   <en>Parse a positive integer with invariant culture so localized number formats cannot enter a key field.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>空文本代表可选时间未设置，输出保持 null。</zh-CN>
            //   <en>Empty text means the optional time is unset, so the output remains null.</en>
            // </lang>
            parsedValue = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            // <lang>
            //   <zh-CN>局部时间变量只承载格式化结果，后续明确标记为 UTC 再放入请求。</zh-CN>
            //   <en>The local variable carries only the parsed format result; it is explicitly marked UTC before entering the request.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>统一时间种类，避免下游把解析结果误当作本地时间。</zh-CN>
            //   <en>Normalize the kind so downstream code cannot mistake the parsed value for local time.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>并发时间戳输出默认为空，新增路径不会因缺少隐藏域而失败。</zh-CN>
            //   <en>Default the concurrency timestamp to null so creation does not fail because the hidden field is absent.</en>
            // </lang>
            parsedValue = null;
            if (entityId <= 0)
            {
                // <lang>
                //   <zh-CN>新增没有旧版本可比较，直接接受空时间戳；更新路径仍必须提供合法值。</zh-CN>
                //   <en>Creation has no prior version to compare, so accept a null timestamp; update paths still require a valid value.</zh-CN>
                // </lang>
                return true;
            }

            // <lang>
            //   <zh-CN>按 round-trip 语义解析隐藏域，保留 UTC/Kind 信息供数据层并发比较。</zh-CN>
            //   <en>Parse the hidden field with round-trip semantics, preserving UTC/Kind information for data-layer concurrency comparison.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>禁用所有会改变员工主数据的控件；只保留低敏提示，不清空已加载的只读上下文。</zh-CN>
            //   <en>Disable every control that could change employee master data while retaining the loaded read-only context and a low-sensitivity message.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>提示写入前统一 HTML 编码，避免服务层或异常摘要形成标记注入。</zh-CN>
            //   <en>HTML-encode every message before rendering so service text or exception summaries cannot become markup injection.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>保存成功后只回到固定目录路径，不把请求参数或用户输入拼入回跳地址。</zh-CN>
            //   <en>After a successful save, return only to the fixed directory path without incorporating request parameters or user input into the redirect.</en>
            // </lang>
            Response.Redirect("EmployeeDirectory.aspx", false);
            // <lang>
            //   <zh-CN>完成当前请求而不触发 Response.End 的线程中止异常。</zh-CN>
            //   <en>Complete the request without triggering the thread-abort exception associated with Response.End.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>审计操作者只取当前身份名称；旧后台缺失身份时沿用固定 admin 兼容值，不把用户输入当作操作者。</zh-CN>
            //   <en>Take the audit actor only from the current identity; preserve the fixed admin compatibility value when the legacy page lacks one, never using user input as the actor.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>按稳定值查找选项，避免把显示文本或文化格式用于选择匹配。</zh-CN>
            //   <en>Find the option by its stable value so display text or culture-specific formatting is not used for selection.</en>
            // </lang>
            ListItem item = list.Items.FindByValue(value ?? string.Empty);
            if (item == null)
            {
                // <lang>
                //   <zh-CN>当前候选集没有该值时保持控件现状，不凭空添加可能越权的选项。</zh-CN>
                //   <en>Keep the control unchanged when the candidate set lacks the value instead of adding an untrusted option.</en>
                // </lang>
                return;
            }

            // <lang>
            //   <zh-CN>先清除旧选择，再标记匹配项，确保单选控件不会保留多个状态。</zh-CN>
            //   <en>Clear the previous selection before marking the match so the single-select control cannot retain multiple states.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>编辑表单只显示固定 UTC 文本格式；空值保持空字符串以匹配可选输入契约。</zh-CN>
            //   <en>Render the edit form with one fixed UTC text format; keep null as an empty string to match the optional-input contract.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>使用 round-trip 格式保留并发比较所需的时间精度和种类信息。</zh-CN>
            //   <en>Use round-trip formatting to preserve the precision and kind information required for concurrency comparison.</en>
            // </lang>
            return value.ToString("O", CultureInfo.InvariantCulture);
        }
    }
}
