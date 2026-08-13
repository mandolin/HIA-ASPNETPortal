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
    ///   <zh-CN>员工、组织和账号绑定的后台只读目录页。</zh-CN>
    ///   <en>Read-only administration directory page for employees, organization units, and Portal-user bindings.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P6.3-S4 保持列表只读，并将新增/编辑动作交给独立后台维护页；导入、导出、绑定或员工工号登录启用仍不在本页处理。</zh-CN>
    ///   <en>P6.3-S4 keeps the lists read-only and delegates creation/editing to separate administration maintenance pages; import, export, binding, and employee-code sign-in enablement remain outside this page.</en>
    /// </lang>
    /// </remarks>
    public partial class EmployeeDirectory : PortalPage<EmployeeDirectory>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>每个目录分区最多读取的行数；这是只读页面的稳定展示上限，不是数据层分页契约。</zh-CN>
        ///   <en>Maximum rows read per directory section; this is a stable display cap for the read-only page, not a data-layer paging contract.</en>
        /// </lang>
        /// </summary>
        private const int PageSize = 50;

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工组织目录只读数据服务。</zh-CN>
        ///   <en>Read-only employee and organization directory data service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IEmployeeDirectoryDb EmployeeDirectoryDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化后台员工目录页面。</zh-CN>
        ///   <en>Initializes the administration employee-directory page.</en>
        /// </lang>
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsureAnyPermission(
                Context,
                PortalPermissionKeys.EmployeeDirectoryView,
                PortalPermissionKeys.EmployeeDirectoryEdit))
            {
                return;
            }

            // <lang>
            //   <zh-CN>首次请求才创建筛选项并查询目录，避免回发覆盖控件状态或重复读取数据。</zh-CN>
            //   <en>Build filters and query the directory only on the initial request so postbacks do not overwrite control state or repeat reads.</en>
            // </lang>
            if (!Page.IsPostBack)
            {
                BindFilterLists();
                BindDirectory();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按当前只读筛选条件重新绑定目录。</zh-CN>
        ///   <en>Rebinds the directory using the current read-only filters.</en>
        /// </lang>
        /// </summary>
        protected void SearchButton_Click(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsureAnyPermission(
                Context,
                PortalPermissionKeys.EmployeeDirectoryView,
                PortalPermissionKeys.EmployeeDirectoryEdit))
            {
                return;
            }

            // <lang>
            //   <zh-CN>搜索动作重新复核查看/编辑任一权限，防止页面加载后的权限变化继续读取目录数据。</zh-CN>
            //   <en>Recheck either view or edit permission for search so a permission change after page load cannot continue directory reads.</en>
            // </lang>
            BindDirectory();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化员工状态与绑定状态筛选项。</zh-CN>
        ///   <en>Initializes employee-status and binding-status filter options.</en>
        /// </lang>
        /// </summary>
        private void BindFilterLists()
        {
            // <lang>
            //   <zh-CN>重建员工状态筛选项，保留稳定的“全部”空值和批准状态常量，不信任回发标记层自行扩展。</zh-CN>
            //   <en>Rebuild employee-status filters with a stable “all” empty value and approved status constants instead of trusting postback markup to extend them.</en>
            // </lang>
            EmployeeStatusList.Items.Clear();
            EmployeeStatusList.Items.Add(new ListItem("All", string.Empty));
            EmployeeStatusList.Items.Add(new ListItem(PortalEmployeeStatuses.Active, PortalEmployeeStatuses.Active));
            EmployeeStatusList.Items.Add(new ListItem(PortalEmployeeStatuses.Pending, PortalEmployeeStatuses.Pending));
            EmployeeStatusList.Items.Add(new ListItem(PortalEmployeeStatuses.Suspended, PortalEmployeeStatuses.Suspended));
            EmployeeStatusList.Items.Add(new ListItem(PortalEmployeeStatuses.Left, PortalEmployeeStatuses.Left));

            // <lang>
            //   <zh-CN>绑定状态筛选与员工状态使用同一固定选项策略，并默认只显示有效绑定。</zh-CN>
            //   <en>Apply the same fixed-option strategy to binding status and default to active bindings only.</en>
            // </lang>
            BindingStatusList.Items.Clear();
            BindingStatusList.Items.Add(new ListItem("Active", PortalUserEmployeeBindingStatuses.Active));
            BindingStatusList.Items.Add(new ListItem("All", string.Empty));
            BindingStatusList.Items.Add(new ListItem(PortalUserEmployeeBindingStatuses.Pending, PortalUserEmployeeBindingStatuses.Pending));
            BindingStatusList.Items.Add(new ListItem(PortalUserEmployeeBindingStatuses.Disabled, PortalUserEmployeeBindingStatuses.Disabled));
            BindingStatusList.Items.Add(new ListItem(PortalUserEmployeeBindingStatuses.Ended, PortalUserEmployeeBindingStatuses.Ended));
            BindingStatusList.SelectedValue = PortalUserEmployeeBindingStatuses.Active;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取组织、员工和账号绑定数据，并绑定到三个只读列表。</zh-CN>
        ///   <en>Reads organization, employee, and user-binding data and binds them to the three read-only lists.</en>
        /// </lang>
        /// </summary>
        private void BindDirectory()
        {
            // <lang>
            //   <zh-CN>没有只读服务时转入统一不可用状态，清空结果而不是继续访问空依赖。</zh-CN>
            //   <en>When the read-only service is missing, enter the shared unavailable state and clear results instead of using a null dependency.</en>
            // </lang>
            if (EmployeeDirectoryDb == null)
            {
                ShowUnavailable("Employee-directory data service is not registered.");
                return;
            }

            // <lang>
            //   <zh-CN>schema 可用性是只读查询的总门禁；不可用时展示固定提示，不泄露连接或 SQL 细节。</zh-CN>
            //   <en>Schema availability is the top-level gate for read-only queries; when unavailable, show a fixed message without exposing connection or SQL details.</en>
            // </lang>
            bool schemaAvailable = EmployeeDirectoryDb.IsSchemaAvailable();
            if (!schemaAvailable)
            {
                ShowUnavailable("P6.3 employee-directory schema is unavailable. Run the P6.3 SQL scripts in an isolated database before expecting data.");
                return;
            }

            // <lang>
            //   <zh-CN>公共查询对象只承载页面筛选和 50 行上限，分别复制到三类读取请求，避免跨列表共享可变状态。</zh-CN>
            //   <en>The common query carries only page filters and the 50-row cap; each of the three read requests copies those values to avoid shared mutable state.</en>
            // </lang>
            EmployeeDirectoryQuery commonQuery = CreateCommonQuery();
            // <lang>
            //   <zh-CN>组织列表作为父级名称映射来源，保持同一批读取结果内的低敏显示一致。</zh-CN>
            //   <en>The organization list becomes the parent-name lookup source, keeping low-sensitivity display consistent within the same read batch.</en>
            // </lang>
            IList<IOrganizationUnitInfo> organizations = EmployeeDirectoryDb.GetOrganizationUnits(new EmployeeDirectoryQuery
            {
                Keyword = commonQuery.Keyword,
                IncludeInactiveOrganizations = commonQuery.IncludeInactiveOrganizations,
                Take = PageSize
            }).ToList();

            // <lang>
            //   <zh-CN>按稳定组织标识建立名称索引；重复主键由数据层事实暴露为异常，不在页面层静默覆盖。</zh-CN>
            //   <en>Build a name index by stable organization identifier; duplicate keys remain a data-layer fact and are not silently overwritten by the page.</en>
            // </lang>
            IDictionary<int, string> organizationNames = organizations.ToDictionary(
                organization => organization.OrganizationUnitId,
                organization => organization.DisplayName);

            // <lang>
            //   <zh-CN>员工读取只带当前关键字、状态和固定上限；页面不把工作邮箱等字段扩展为额外查询条件。</zh-CN>
            //   <en>Employee reads carry only the current keyword, status and fixed cap; the page does not expand work-email or other fields into extra filters.</en>
            // </lang>
            IList<IEmployeeInfo> employees = EmployeeDirectoryDb.GetEmployees(new EmployeeDirectoryQuery
            {
                Keyword = commonQuery.Keyword,
                Status = EmployeeStatusList.SelectedValue,
                Take = PageSize
            }).ToList();

            // <lang>
            //   <zh-CN>绑定读取使用独立状态筛选，保持账号员工关系的可见性不被员工状态误合并。</zh-CN>
            //   <en>Binding reads use an independent status filter so account-employee relationship visibility is not conflated with employee status.</en>
            // </lang>
            IList<IUserEmployeeBindingInfo> bindings = EmployeeDirectoryDb.GetUserEmployeeBindings(new EmployeeDirectoryQuery
            {
                Keyword = commonQuery.Keyword,
                Status = BindingStatusList.SelectedValue,
                Take = PageSize
            }).ToList();

            // <lang>
            //   <zh-CN>投影层只生成低敏展示行和固定站内编辑地址，不把原始数据对象直接交给标记层。</zh-CN>
            //   <en>The projection creates only low-sensitivity display rows and fixed in-application edit URLs instead of passing raw data objects to markup.</en>
            // </lang>
            OrganizationsRepeater.DataSource = organizations
                .Select(organization => new OrganizationDirectoryRow(organization, GetParentText(organization, organizationNames)))
                .ToList();
            OrganizationsRepeater.DataBind();

            // <lang>
            //   <zh-CN>员工展示行集中处理组织回退、状态和编辑/绑定地址，保持标记层只消费已约束字段。</zh-CN>
            //   <en>Employee display rows centralize organization fallback, status and edit/binding URLs so markup consumes only constrained fields.</en>
            // </lang>
            EmployeesRepeater.DataSource = employees.Select(employee => new EmployeeDirectoryRow(employee)).ToList();
            EmployeesRepeater.DataBind();

            // <lang>
            //   <zh-CN>绑定展示行只输出账号、员工和非敏感原因摘要，不在页面层启用绑定或改变关系状态。</zh-CN>
            //   <en>Binding rows expose only account, employee and non-sensitive reason summaries; the page does not enable binding or change relationship state.</en>
            // </lang>
            BindingsRepeater.DataSource = bindings.Select(binding => new UserEmployeeBindingDirectoryRow(binding)).ToList();
            BindingsRepeater.DataBind();

            // <lang>
            //   <zh-CN>成功绑定后清除旧提示，并以固定文化格式显示每个分区的数量事实。</zh-CN>
            //   <en>After successful binding, clear stale messages and display each section's count using invariant formatting.</en>
            // </lang>
            MessageLabel.Text = string.Empty;
            SchemaStatusLabel.Text = "P6.3 schema available. This page is read-only.";
            ResultLabel.Text = "Showing up to " + PageSize.ToString(CultureInfo.InvariantCulture) +
                               " rows per section; organizations: " + organizations.Count.ToString(CultureInfo.InvariantCulture) +
                               ", employees: " + employees.Count.ToString(CultureInfo.InvariantCulture) +
                               ", bindings: " + bindings.Count.ToString(CultureInfo.InvariantCulture) + ".";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>根据页面输入创建目录查询的公共筛选对象。</zh-CN>
        ///   <en>Creates the shared directory query filter from page inputs.</en>
        /// </lang>
        /// </summary>
        private EmployeeDirectoryQuery CreateCommonQuery()
        {
            // <lang>
            //   <zh-CN>从当前控件读取关键字和停用组织开关，形成只读查询输入；具体字段清理由数据层继续负责。</zh-CN>
            //   <en>Read the keyword and inactive-organization switch from controls to form read-only query input; the data layer remains responsible for field cleanup.</en>
            // </lang>
            return new EmployeeDirectoryQuery
            {
                Keyword = KeywordTextBox.Text,
                IncludeInactiveOrganizations = IncludeInactiveOrganizations.Checked,
                Take = PageSize
            };
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>显示数据服务或 schema 不可用提示，并清空只读结果区。</zh-CN>
        ///   <en>Displays data-service or schema-unavailable messages and clears the read-only result area.</en>
        /// </lang>
        /// </summary>
        private void ShowUnavailable(string message)
        {
            // <lang>
            //   <zh-CN>不可用状态清除三组结果，避免用户看到上一次成功读取的陈旧目录数据。</zh-CN>
            //   <en>The unavailable state clears all three result sets so users cannot see stale data from a previous successful read.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>生成父级组织展示文本，优先显示本页已读取到的组织名称。</zh-CN>
        ///   <en>Builds parent-organization display text, preferring organization names already read by this page.</en>
        /// </lang>
        /// </summary>
        private static string GetParentText(IOrganizationUnitInfo organization, IDictionary<int, string> organizationNames)
        {
            // <lang>
            //   <zh-CN>没有父级时使用固定根节点占位，不将 null 传播到标记层。</zh-CN>
            //   <en>Use a fixed root placeholder when no parent exists instead of propagating null into markup.</en>
            // </lang>
            if (!organization.ParentOrganizationUnitId.HasValue)
            {
                return "(root)";
            }

            // <lang>
            //   <zh-CN>父级名称只从本次已读取的索引中查找，避免为展示文本触发额外数据库读取。</zh-CN>
            //   <en>Look up the parent name only in the index read during this batch, avoiding extra database reads for display text.</en>
            // </lang>
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
    /// <lang>
    ///   <zh-CN>员工目录页的组织展示行。</zh-CN>
    ///   <en>Organization display row for the employee-directory page.</en>
    /// </lang>
    /// </summary>
    public sealed class OrganizationDirectoryRow
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>从组织数据对象创建后台展示行。</zh-CN>
        ///   <en>Creates an administration display row from an organization data object.</en>
        /// </lang>
        /// </summary>
        internal OrganizationDirectoryRow(IOrganizationUnitInfo organization, string parentText)
        {
            // <lang>
            //   <zh-CN>投影复制组织字段并把启用状态、编辑地址转换为只读展示值；不保留可写数据对象。</zh-CN>
            //   <en>Project organization fields and convert active state and edit URL into read-only display values without retaining a writable data object.</en>
            // </lang>
            OrganizationUnitId = organization.OrganizationUnitId;
            OrganizationCode = organization.OrganizationCode;
            DisplayName = organization.DisplayName;
            ParentText = parentText;
            SortOrder = organization.SortOrder;
            IsActiveText = organization.IsActive ? "Yes" : "No";
            // <lang>
            //   <zh-CN>编辑地址固定为当前应用内页，标识使用不变文化格式，不接受外部 URL。</zh-CN>
            //   <en>Keep the edit URL inside the current application and format the identifier invariantly; no external URL is accepted.</en>
            // </lang>
            EditUrl = "OrganizationUnitEdit.aspx?organizationUnitId=" +
                      organization.OrganizationUnitId.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>组织单元标识。</zh-CN>
        ///   <en>Organization-unit identifier.</en>
        /// </lang>
        /// </summary>
        public int OrganizationUnitId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>组织编码。</zh-CN>
        ///   <en>Organization code.</en>
        /// </lang>
        /// </summary>
        public string OrganizationCode { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>组织显示名。</zh-CN>
        ///   <en>Organization display name.</en>
        /// </lang>
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>父级组织展示文本。</zh-CN>
        ///   <en>Parent organization display text.</en>
        /// </lang>
        /// </summary>
        public string ParentText { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>排序值。</zh-CN>
        ///   <en>Sort order.</en>
        /// </lang>
        /// </summary>
        public int SortOrder { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>启用状态展示文本。</zh-CN>
        ///   <en>Active-state display text.</en>
        /// </lang>
        /// </summary>
        public string IsActiveText { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>编辑页站内地址。</zh-CN>
        ///   <en>Current-application edit-page URL.</en>
        /// </lang>
        /// </summary>
        public string EditUrl { get; private set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>员工目录页的员工展示行。</zh-CN>
    ///   <en>Employee display row for the employee-directory page.</en>
    /// </lang>
    /// </summary>
    public sealed class EmployeeDirectoryRow
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>从员工数据对象创建后台展示行。</zh-CN>
        ///   <en>Creates an administration display row from an employee data object.</en>
        /// </lang>
        /// </summary>
        internal EmployeeDirectoryRow(IEmployeeInfo employee)
        {
            // <lang>
            //   <zh-CN>投影只复制目录展示所需的低敏字段；账号安全版本、凭据和绑定写入不在此模型中。</zh-CN>
            //   <en>Project only low-sensitivity fields needed for directory display; credentials, security versions and binding writes are outside this model.</en>
            // </lang>
            EmployeeId = employee.EmployeeId;
            EmployeeCode = employee.EmployeeCode;
            DisplayName = employee.DisplayName;
            PreferredName = employee.PreferredName;
            WorkEmail = employee.WorkEmail;
            // <lang>
            //   <zh-CN>组织显示优先使用已联接名称，缺失时回退到稳定组织标识或空字符串，不臆造名称。</zh-CN>
            //   <en>Prefer the joined organization name, then fall back to the stable organization identifier or empty text without inventing a name.</en>
            // </lang>
            OrganizationText = string.IsNullOrEmpty(employee.OrganizationDisplayName)
                ? employee.OrganizationUnitId.HasValue
                    ? "#" + employee.OrganizationUnitId.Value.ToString(CultureInfo.InvariantCulture)
                    : string.Empty
                : employee.OrganizationDisplayName;
            EmploymentStatus = employee.EmploymentStatus;
            SourceSystem = employee.SourceSystem;
            // <lang>
            //   <zh-CN>两个动作地址都固定为站内维护页，分别表达员工编辑与绑定维护，不在只读页执行动作。</zh-CN>
            //   <en>Both action URLs point to fixed in-application maintenance pages, representing employee editing and binding maintenance without executing actions here.</en>
            // </lang>
            EditUrl = "EmployeeEdit.aspx?employeeId=" + employee.EmployeeId.ToString(CultureInfo.InvariantCulture);
            BindUrl = "UserEmployeeBindingEdit.aspx?employeeId=" + employee.EmployeeId.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工标识。</zh-CN>
        ///   <en>Employee identifier.</en>
        /// </lang>
        /// </summary>
        public int EmployeeId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工号。</zh-CN>
        ///   <en>Employee code.</en>
        /// </lang>
        /// </summary>
        public string EmployeeCode { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工显示名。</zh-CN>
        ///   <en>Employee display name.</en>
        /// </lang>
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>偏好称呼。</zh-CN>
        ///   <en>Preferred name.</en>
        /// </lang>
        /// </summary>
        public string PreferredName { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>工作邮箱。</zh-CN>
        ///   <en>Work email.</en>
        /// </lang>
        /// </summary>
        public string WorkEmail { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>组织展示文本。</zh-CN>
        ///   <en>Organization display text.</en>
        /// </lang>
        /// </summary>
        public string OrganizationText { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工状态。</zh-CN>
        ///   <en>Employee status.</en>
        /// </lang>
        /// </summary>
        public string EmploymentStatus { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>来源系统。</zh-CN>
        ///   <en>Source system.</en>
        /// </lang>
        /// </summary>
        public string SourceSystem { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>编辑页站内地址。</zh-CN>
        ///   <en>Current-application edit-page URL.</en>
        /// </lang>
        /// </summary>
        public string EditUrl { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>账号员工绑定维护页站内地址。</zh-CN>
        ///   <en>Current-application user-employee binding URL.</en>
        /// </lang>
        /// </summary>
        public string BindUrl { get; private set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>员工目录页的账号员工绑定展示行。</zh-CN>
    ///   <en>User-employee binding display row for the employee-directory page.</en>
    /// </lang>
    /// </summary>
    public sealed class UserEmployeeBindingDirectoryRow
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>从账号员工绑定数据对象创建后台展示行。</zh-CN>
        ///   <en>Creates an administration display row from a user-employee binding data object.</en>
        /// </lang>
        /// </summary>
        internal UserEmployeeBindingDirectoryRow(IUserEmployeeBindingInfo binding)
        {
            // <lang>
            //   <zh-CN>投影复制账号员工关系的低敏展示字段，时间统一为 UTC 文本，原因保持数据层提供的非敏感摘要。</zh-CN>
            //   <en>Project low-sensitivity account-employee relationship fields, render time as UTC text, and retain the non-sensitive summary supplied by the data layer.</en>
            // </lang>
            BindingId = binding.BindingId;
            UserId = binding.UserId;
            UserName = binding.UserName;
            EmployeeCode = binding.EmployeeCode;
            EmployeeDisplayName = binding.EmployeeDisplayName;
            BindingStatus = binding.BindingStatus;
            BoundUtcText = binding.BoundUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
            Reason = binding.Reason;
            // <lang>
            //   <zh-CN>绑定维护地址固定在当前应用内，标识采用不变文化格式，目录页仍保持只读。</zh-CN>
            //   <en>Keep the binding-maintenance URL inside the current application and format its identifier invariantly while the directory remains read-only.</en>
            // </lang>
            EditUrl = "UserEmployeeBindingEdit.aspx?bindingId=" + binding.BindingId.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定标识。</zh-CN>
        ///   <en>Binding identifier.</en>
        /// </lang>
        /// </summary>
        public int BindingId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>门户账号标识。</zh-CN>
        ///   <en>Portal user identifier.</en>
        /// </lang>
        /// </summary>
        public int UserId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>门户用户名。</zh-CN>
        ///   <en>Portal user name.</en>
        /// </lang>
        /// </summary>
        public string UserName { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工号。</zh-CN>
        ///   <en>Employee code.</en>
        /// </lang>
        /// </summary>
        public string EmployeeCode { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工显示名。</zh-CN>
        ///   <en>Employee display name.</en>
        /// </lang>
        /// </summary>
        public string EmployeeDisplayName { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定状态。</zh-CN>
        ///   <en>Binding status.</en>
        /// </lang>
        /// </summary>
        public string BindingStatus { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定时间展示文本。</zh-CN>
        ///   <en>Binding time display text.</en>
        /// </lang>
        /// </summary>
        public string BoundUtcText { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定维护页站内地址。</zh-CN>
        ///   <en>Current-application binding maintenance URL.</en>
        /// </lang>
        /// </summary>
        public string EditUrl { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>非敏感绑定说明。</zh-CN>
        ///   <en>Non-sensitive binding reason.</en>
        /// </lang>
        /// </summary>
        public string Reason { get; private set; }
    }
}
