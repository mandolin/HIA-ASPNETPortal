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
    /// 中文：员工主数据后台最小维护页面。
    ///
    /// English: Minimal administration maintenance page for employee master data.
    /// </summary>
    /// <remarks>
    /// 中文：P6.3-S4 只维护员工主数据，不启用员工工号登录，不处理账号绑定，也不保存手机号、身份证号等高敏资料。
    ///
    /// English: P6.3-S4 maintains employee master data only. It does not enable employee-code sign-in, process
    /// account binding, or store highly sensitive data such as phone numbers or government identifiers.
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
        /// 中文：员工组织后台写入服务。
        ///
        /// English: Employee-directory administration write service.
        /// </summary>
        [Dependency]
        public IEmployeeDirectoryAdminDb EmployeeDirectoryAdminDb { private get; set; }

        /// <summary>
        /// 中文：员工组织只读目录服务，用于组织下拉框。
        ///
        /// English: Read-only employee-directory service used by the organization selector.
        /// </summary>
        [Dependency]
        public IEmployeeDirectoryDb EmployeeDirectoryDb { private get; set; }

        /// <summary>
        /// 中文：初始化员工维护页。
        ///
        /// English: Initializes the employee maintenance page.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsureAdmin(Context))
            {
                return;
            }

            if (!IsPostBack)
            {
                BindForm();
            }
        }

        /// <summary>
        /// 中文：保存员工新增或编辑结果。
        ///
        /// English: Saves employee creation or editing changes.
        /// </summary>
        protected void SaveButton_Click(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsureAdmin(Context))
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
                string eventId = PortalDiagnostics.Error(
                    "Admin.EmployeeEdit.Save",
                    "Saving employee failed. EmployeeId=" + request.EmployeeId,
                    exception,
                    Context);
                ShowMessage("Employee save failed. Event id: " + eventId);
            }
        }

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
                TitleLabel.Text = "New Employee";
                EmployeeIdField.Value = "0";
                OriginalUpdatedUtcField.Value = string.Empty;
                SourceSystemTextBox.Text = "Portal";
                SelectListValue(EmploymentStatusList, PortalEmployeeStatuses.Active);
                return;
            }

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

        private void BindStatusList()
        {
            EmploymentStatusList.Items.Clear();
            EmploymentStatusList.Items.Add(new ListItem(PortalEmployeeStatuses.Active, PortalEmployeeStatuses.Active));
            EmploymentStatusList.Items.Add(new ListItem(PortalEmployeeStatuses.Pending, PortalEmployeeStatuses.Pending));
            EmploymentStatusList.Items.Add(new ListItem(PortalEmployeeStatuses.Suspended, PortalEmployeeStatuses.Suspended));
            EmploymentStatusList.Items.Add(new ListItem(PortalEmployeeStatuses.Left, PortalEmployeeStatuses.Left));
        }

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

        private void ShowMessage(string message)
        {
            MessageLabel.Text = Server.HtmlEncode(message ?? string.Empty);
        }

        private void RedirectToDirectory()
        {
            Response.Redirect("EmployeeDirectory.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private string GetCurrentActor()
        {
            return Context.User == null || Context.User.Identity == null ||
                   string.IsNullOrWhiteSpace(Context.User.Identity.Name)
                ? "admin"
                : Context.User.Identity.Name;
        }

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

        private static string FormatOptionalUtc(DateTime? value)
        {
            return value.HasValue
                ? value.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                : string.Empty;
        }

        private static string FormatRoundTripUtc(DateTime value)
        {
            return value.ToString("O", CultureInfo.InvariantCulture);
        }
    }
}
