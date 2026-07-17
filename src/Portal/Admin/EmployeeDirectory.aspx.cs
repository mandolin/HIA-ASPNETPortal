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
    /// 中文：员工、组织和账号绑定的后台只读目录页。
    ///
    /// English: Read-only administration directory page for employees, organization units, and Portal-user bindings.
    /// </summary>
    /// <remarks>
    /// 中文：P6.3-S4 保持列表只读，并将新增/编辑动作交给独立后台维护页；导入、导出、绑定或员工工号登录启用仍不在本页处理。
    ///
    /// English: P6.3-S4 keeps the lists read-only and delegates creation/editing to separate administration
    /// maintenance pages; import, export, binding, and employee-code sign-in enablement remain outside this page.
    /// </remarks>
    public partial class EmployeeDirectory : PortalPage<EmployeeDirectory>
    {
        private const int PageSize = 50;

        /// <summary>
        /// 中文：员工组织目录只读数据服务。
        ///
        /// English: Read-only employee and organization directory data service.
        /// </summary>
        [Dependency]
        public IEmployeeDirectoryDb EmployeeDirectoryDb { private get; set; }

        /// <summary>
        /// 中文：初始化后台员工目录页面。
        ///
        /// English: Initializes the administration employee-directory page.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsureAdmin(Context))
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                BindFilterLists();
                BindDirectory();
            }
        }

        /// <summary>
        /// 中文：按当前只读筛选条件重新绑定目录。
        ///
        /// English: Rebinds the directory using the current read-only filters.
        /// </summary>
        protected void SearchButton_Click(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsureAdmin(Context))
            {
                return;
            }

            BindDirectory();
        }

        private void BindFilterLists()
        {
            EmployeeStatusList.Items.Clear();
            EmployeeStatusList.Items.Add(new ListItem("All", string.Empty));
            EmployeeStatusList.Items.Add(new ListItem(PortalEmployeeStatuses.Active, PortalEmployeeStatuses.Active));
            EmployeeStatusList.Items.Add(new ListItem(PortalEmployeeStatuses.Pending, PortalEmployeeStatuses.Pending));
            EmployeeStatusList.Items.Add(new ListItem(PortalEmployeeStatuses.Suspended, PortalEmployeeStatuses.Suspended));
            EmployeeStatusList.Items.Add(new ListItem(PortalEmployeeStatuses.Left, PortalEmployeeStatuses.Left));

            BindingStatusList.Items.Clear();
            BindingStatusList.Items.Add(new ListItem("Active", PortalUserEmployeeBindingStatuses.Active));
            BindingStatusList.Items.Add(new ListItem("All", string.Empty));
            BindingStatusList.Items.Add(new ListItem(PortalUserEmployeeBindingStatuses.Pending, PortalUserEmployeeBindingStatuses.Pending));
            BindingStatusList.Items.Add(new ListItem(PortalUserEmployeeBindingStatuses.Disabled, PortalUserEmployeeBindingStatuses.Disabled));
            BindingStatusList.Items.Add(new ListItem(PortalUserEmployeeBindingStatuses.Ended, PortalUserEmployeeBindingStatuses.Ended));
            BindingStatusList.SelectedValue = PortalUserEmployeeBindingStatuses.Active;
        }

        private void BindDirectory()
        {
            if (EmployeeDirectoryDb == null)
            {
                ShowUnavailable("Employee-directory data service is not registered.");
                return;
            }

            bool schemaAvailable = EmployeeDirectoryDb.IsSchemaAvailable();
            if (!schemaAvailable)
            {
                ShowUnavailable("P6.3 employee-directory schema is unavailable. Run the P6.3 SQL scripts in an isolated database before expecting data.");
                return;
            }

            EmployeeDirectoryQuery commonQuery = CreateCommonQuery();
            IList<IOrganizationUnitInfo> organizations = EmployeeDirectoryDb.GetOrganizationUnits(new EmployeeDirectoryQuery
            {
                Keyword = commonQuery.Keyword,
                IncludeInactiveOrganizations = commonQuery.IncludeInactiveOrganizations,
                Take = PageSize
            }).ToList();

            IDictionary<int, string> organizationNames = organizations.ToDictionary(
                organization => organization.OrganizationUnitId,
                organization => organization.DisplayName);

            IList<IEmployeeInfo> employees = EmployeeDirectoryDb.GetEmployees(new EmployeeDirectoryQuery
            {
                Keyword = commonQuery.Keyword,
                Status = EmployeeStatusList.SelectedValue,
                Take = PageSize
            }).ToList();

            IList<IUserEmployeeBindingInfo> bindings = EmployeeDirectoryDb.GetUserEmployeeBindings(new EmployeeDirectoryQuery
            {
                Keyword = commonQuery.Keyword,
                Status = BindingStatusList.SelectedValue,
                Take = PageSize
            }).ToList();

            OrganizationsRepeater.DataSource = organizations
                .Select(organization => new OrganizationDirectoryRow(organization, GetParentText(organization, organizationNames)))
                .ToList();
            OrganizationsRepeater.DataBind();

            EmployeesRepeater.DataSource = employees.Select(employee => new EmployeeDirectoryRow(employee)).ToList();
            EmployeesRepeater.DataBind();

            BindingsRepeater.DataSource = bindings.Select(binding => new UserEmployeeBindingDirectoryRow(binding)).ToList();
            BindingsRepeater.DataBind();

            MessageLabel.Text = string.Empty;
            SchemaStatusLabel.Text = "P6.3 schema available. This page is read-only.";
            ResultLabel.Text = "Showing up to " + PageSize.ToString(CultureInfo.InvariantCulture) +
                               " rows per section; organizations: " + organizations.Count.ToString(CultureInfo.InvariantCulture) +
                               ", employees: " + employees.Count.ToString(CultureInfo.InvariantCulture) +
                               ", bindings: " + bindings.Count.ToString(CultureInfo.InvariantCulture) + ".";
        }

        private EmployeeDirectoryQuery CreateCommonQuery()
        {
            return new EmployeeDirectoryQuery
            {
                Keyword = KeywordTextBox.Text,
                IncludeInactiveOrganizations = IncludeInactiveOrganizations.Checked,
                Take = PageSize
            };
        }

        private void ShowUnavailable(string message)
        {
            MessageLabel.Text = message ?? string.Empty;
            SchemaStatusLabel.Text = "P6.3 schema unavailable.";
            ResultLabel.Text = string.Empty;
            OrganizationsRepeater.DataSource = Enumerable.Empty<OrganizationDirectoryRow>();
            OrganizationsRepeater.DataBind();
            EmployeesRepeater.DataSource = Enumerable.Empty<EmployeeDirectoryRow>();
            EmployeesRepeater.DataBind();
            BindingsRepeater.DataSource = Enumerable.Empty<UserEmployeeBindingDirectoryRow>();
            BindingsRepeater.DataBind();
        }

        private static string GetParentText(IOrganizationUnitInfo organization, IDictionary<int, string> organizationNames)
        {
            if (!organization.ParentOrganizationUnitId.HasValue)
            {
                return "(root)";
            }

            string parentName;
            if (organizationNames.TryGetValue(organization.ParentOrganizationUnitId.Value, out parentName) &&
                !string.IsNullOrEmpty(parentName))
            {
                return parentName + " (#" + organization.ParentOrganizationUnitId.Value.ToString(CultureInfo.InvariantCulture) + ")";
            }

            return "#" + organization.ParentOrganizationUnitId.Value.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// 中文：员工目录页的组织展示行。
    ///
    /// English: Organization display row for the employee-directory page.
    /// </summary>
    public sealed class OrganizationDirectoryRow
    {
        internal OrganizationDirectoryRow(IOrganizationUnitInfo organization, string parentText)
        {
            OrganizationUnitId = organization.OrganizationUnitId;
            OrganizationCode = organization.OrganizationCode;
            DisplayName = organization.DisplayName;
            ParentText = parentText;
            SortOrder = organization.SortOrder;
            IsActiveText = organization.IsActive ? "Yes" : "No";
            EditUrl = "OrganizationUnitEdit.aspx?organizationUnitId=" +
                      organization.OrganizationUnitId.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>中文：组织单元标识。English: Organization-unit identifier.</summary>
        public int OrganizationUnitId { get; private set; }

        /// <summary>中文：组织编码。English: Organization code.</summary>
        public string OrganizationCode { get; private set; }

        /// <summary>中文：组织显示名。English: Organization display name.</summary>
        public string DisplayName { get; private set; }

        /// <summary>中文：父级组织展示文本。English: Parent organization display text.</summary>
        public string ParentText { get; private set; }

        /// <summary>中文：排序值。English: Sort order.</summary>
        public int SortOrder { get; private set; }

        /// <summary>中文：启用状态展示文本。English: Active-state display text.</summary>
        public string IsActiveText { get; private set; }

        /// <summary>中文：编辑页站内地址。English: Current-application edit-page URL.</summary>
        public string EditUrl { get; private set; }
    }

    /// <summary>
    /// 中文：员工目录页的员工展示行。
    ///
    /// English: Employee display row for the employee-directory page.
    /// </summary>
    public sealed class EmployeeDirectoryRow
    {
        internal EmployeeDirectoryRow(IEmployeeInfo employee)
        {
            EmployeeId = employee.EmployeeId;
            EmployeeCode = employee.EmployeeCode;
            DisplayName = employee.DisplayName;
            PreferredName = employee.PreferredName;
            WorkEmail = employee.WorkEmail;
            OrganizationText = string.IsNullOrEmpty(employee.OrganizationDisplayName)
                ? employee.OrganizationUnitId.HasValue
                    ? "#" + employee.OrganizationUnitId.Value.ToString(CultureInfo.InvariantCulture)
                    : string.Empty
                : employee.OrganizationDisplayName;
            EmploymentStatus = employee.EmploymentStatus;
            SourceSystem = employee.SourceSystem;
            EditUrl = "EmployeeEdit.aspx?employeeId=" + employee.EmployeeId.ToString(CultureInfo.InvariantCulture);
            BindUrl = "UserEmployeeBindingEdit.aspx?employeeId=" + employee.EmployeeId.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>中文：员工标识。English: Employee identifier.</summary>
        public int EmployeeId { get; private set; }

        /// <summary>中文：员工号。English: Employee code.</summary>
        public string EmployeeCode { get; private set; }

        /// <summary>中文：员工显示名。English: Employee display name.</summary>
        public string DisplayName { get; private set; }

        /// <summary>中文：偏好称呼。English: Preferred name.</summary>
        public string PreferredName { get; private set; }

        /// <summary>中文：工作邮箱。English: Work email.</summary>
        public string WorkEmail { get; private set; }

        /// <summary>中文：组织展示文本。English: Organization display text.</summary>
        public string OrganizationText { get; private set; }

        /// <summary>中文：员工状态。English: Employee status.</summary>
        public string EmploymentStatus { get; private set; }

        /// <summary>中文：来源系统。English: Source system.</summary>
        public string SourceSystem { get; private set; }

        /// <summary>中文：编辑页站内地址。English: Current-application edit-page URL.</summary>
        public string EditUrl { get; private set; }

        /// <summary>中文：账号员工绑定维护页站内地址。English: Current-application user-employee binding URL.</summary>
        public string BindUrl { get; private set; }
    }

    /// <summary>
    /// 中文：员工目录页的账号员工绑定展示行。
    ///
    /// English: User-employee binding display row for the employee-directory page.
    /// </summary>
    public sealed class UserEmployeeBindingDirectoryRow
    {
        internal UserEmployeeBindingDirectoryRow(IUserEmployeeBindingInfo binding)
        {
            BindingId = binding.BindingId;
            UserId = binding.UserId;
            UserName = binding.UserName;
            EmployeeCode = binding.EmployeeCode;
            EmployeeDisplayName = binding.EmployeeDisplayName;
            BindingStatus = binding.BindingStatus;
            BoundUtcText = binding.BoundUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
            Reason = binding.Reason;
            EditUrl = "UserEmployeeBindingEdit.aspx?bindingId=" + binding.BindingId.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>中文：绑定标识。English: Binding identifier.</summary>
        public int BindingId { get; private set; }

        /// <summary>中文：门户账号标识。English: Portal user identifier.</summary>
        public int UserId { get; private set; }

        /// <summary>中文：门户用户名。English: Portal user name.</summary>
        public string UserName { get; private set; }

        /// <summary>中文：员工号。English: Employee code.</summary>
        public string EmployeeCode { get; private set; }

        /// <summary>中文：员工显示名。English: Employee display name.</summary>
        public string EmployeeDisplayName { get; private set; }

        /// <summary>中文：绑定状态。English: Binding status.</summary>
        public string BindingStatus { get; private set; }

        /// <summary>中文：绑定时间展示文本。English: Binding time display text.</summary>
        public string BoundUtcText { get; private set; }

        /// <summary>中文：绑定维护页站内地址。English: Current-application binding maintenance URL.</summary>
        public string EditUrl { get; private set; }

        /// <summary>中文：非敏感绑定说明。English: Non-sensitive binding reason.</summary>
        public string Reason { get; private set; }
    }
}
