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
    /// 中文：组织单元后台最小维护页面。
    ///
    /// English: Minimal administration maintenance page for organization units.
    /// </summary>
    /// <remarks>
    /// 中文：P6.3-S4 只允许新增和编辑组织单元，不提供硬删除、导入、导出或批量同步。父级存在性、
    /// 自引用和循环关系在数据层再次校验。
    ///
    /// English: P6.3-S4 allows only creation and editing of organization units. It provides no hard delete, import,
    /// export, or batch synchronization. Parent existence, self-parenting, and cycles are revalidated by the data layer.
    /// </remarks>
    public partial class OrganizationUnitEdit : PortalPage<OrganizationUnitEdit>
    {
        /// <summary>
        /// 中文：员工组织后台写入服务。
        ///
        /// English: Employee-directory administration write service.
        /// </summary>
        [Dependency]
        public IEmployeeDirectoryAdminDb EmployeeDirectoryAdminDb { private get; set; }

        /// <summary>
        /// 中文：员工组织只读目录服务，用于父级下拉框。
        ///
        /// English: Read-only employee-directory service used by the parent selector.
        /// </summary>
        [Dependency]
        public IEmployeeDirectoryDb EmployeeDirectoryDb { private get; set; }

        /// <summary>
        /// 中文：初始化组织单元维护页。
        ///
        /// English: Initializes the organization-unit maintenance page.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
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
        /// 中文：保存组织单元新增或编辑结果。
        ///
        /// English: Saves organization-unit creation or editing changes.
        /// </summary>
        protected void SaveButton_Click(object sender, EventArgs e)
        {
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
                string eventId = PortalDiagnostics.Error(
                    "Admin.OrganizationUnitEdit.Save",
                    "Saving organization unit failed. OrganizationUnitId=" + request.OrganizationUnitId,
                    exception,
                    Context);
                ShowMessage("Organization unit save failed. Event id: " + eventId);
            }
        }

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
                TitleLabel.Text = "New Organization Unit";
                OrganizationUnitIdField.Value = "0";
                OriginalUpdatedUtcField.Value = string.Empty;
                SortOrderTextBox.Text = "0";
                IsActiveCheckBox.Checked = true;
                return;
            }

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
            TitleLabel.Text = "Organization Unit";
            SaveButton.Enabled = false;
            OrganizationCodeTextBox.Enabled = false;
            DisplayNameTextBox.Enabled = false;
            ParentOrganizationList.Enabled = false;
            SortOrderTextBox.Enabled = false;
            IsActiveCheckBox.Enabled = false;
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

        private static string FormatRoundTripUtc(DateTime value)
        {
            return value.ToString("O", CultureInfo.InvariantCulture);
        }
    }
}
