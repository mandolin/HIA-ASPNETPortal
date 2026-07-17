using System;
using System.Globalization;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：员工资料确认业务模块样板。
    ///
    /// English: Business-module sample for employee-profile confirmation.
    /// </summary>
    /// <remarks>
    /// 中文：第一版只允许已登录且拥有 Active 员工绑定的用户查看并确认自己的低敏员工基础资料。
    /// 它不提供资料编辑、附件上传、在线脚本、外部资源加载或 HR 同步。
    ///
    /// English: The first version allows only signed-in users with an active employee binding to view and confirm
    /// their own low-sensitivity employee foundation profile. It provides no profile editing, attachment upload,
    /// online script, external-resource loading, or HR synchronization.
    /// </remarks>
    public partial class EmployeeProfileConfirm : PortalModuleControl<EmployeeProfileConfirm>
    {
        /// <summary>
        /// 中文：用户数据访问服务，用于把当前登录名解析为门户用户标识。
        ///
        /// English: User data service used to resolve the current sign-in name to a Portal user identifier.
        /// </summary>
        [Dependency]
        public IUsersDb UsersDb { private get; set; }

        /// <summary>
        /// 中文：员工资料确认模块数据访问服务。
        ///
        /// English: Employee-profile confirmation module data service.
        /// </summary>
        [Dependency]
        public IEmployeeProfileConfirmationDb EmployeeProfileConfirmationDb { private get; set; }

        /// <summary>
        /// 中文：初始化员工资料确认模块。
        ///
        /// English: Initializes the employee-profile confirmation module.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindProfile();
            }
        }

        /// <summary>
        /// 中文：确认当前绑定员工资料。
        ///
        /// English: Confirms the current bound employee profile.
        /// </summary>
        protected void ConfirmButton_Click(object sender, EventArgs e)
        {
            int userId = GetCurrentUserId();
            EmployeeProfileConfirmationView profile = GetCurrentProfile(userId);
            if (profile == null)
            {
                ShowMessage("当前账号没有可确认的在职员工资料。");
                return;
            }

            EmployeeProfileConfirmationResult result = EmployeeProfileConfirmationDb.ConfirmProfile(
                new EmployeeProfileConfirmationRequest
                {
                    UserId = userId,
                    EmployeeId = profile.EmployeeId,
                    ConfirmedUtc = DateTime.UtcNow,
                    ConfirmedBy = GetCurrentUserName()
                });

            if (!result.Succeeded)
            {
                ShowMessage(result.Message);
                return;
            }

            PortalOperationAudit.Record(
                PortalOperationAuditEvents.BusinessModuleCategory,
                PortalOperationAuditEvents.EmployeeProfileConfirmed,
                PortalOperationAuditEvents.EmployeeProfileConfirmationTargetType,
                result.ConfirmationId.ToString(CultureInfo.InvariantCulture),
                "Employee profile confirmed. EmployeeId=" + profile.EmployeeId.ToString(CultureInfo.InvariantCulture),
                Context);

            BindProfile();
            ShowMessage("资料确认已记录。");
        }

        private void BindProfile()
        {
            int userId = GetCurrentUserId();
            EmployeeProfileConfirmationView profile = GetCurrentProfile(userId);
            if (profile == null)
            {
                ProfilePanel.Visible = false;
                ShowMessage(GetUnavailableMessage(userId));
                return;
            }

            ProfilePanel.Visible = true;
            MessageLabel.Text = string.Empty;
            EmployeeCodeLabel.Text = EncodeDisplay(profile.EmployeeCode);
            DisplayNameLabel.Text = EncodeDisplay(profile.DisplayName);
            PreferredNameLabel.Text = EncodeDisplay(EmptyToNone(profile.PreferredName));
            WorkEmailLabel.Text = EncodeDisplay(EmptyToNone(profile.WorkEmail));
            OrganizationLabel.Text = EncodeDisplay(EmptyToNone(profile.OrganizationDisplayName));
            EmploymentStatusLabel.Text = EncodeDisplay(profile.EmploymentStatus);
            LastConfirmedLabel.Text = EncodeDisplay(FormatUtc(profile.LastConfirmedUtc));
        }

        private EmployeeProfileConfirmationView GetCurrentProfile(int userId)
        {
            if (EmployeeProfileConfirmationDb == null ||
                !EmployeeProfileConfirmationDb.IsSchemaAvailable() ||
                userId <= 0)
            {
                return null;
            }

            return EmployeeProfileConfirmationDb.GetCurrentProfileForUser(userId);
        }

        private int GetCurrentUserId()
        {
            string userName = GetCurrentUserName();
            if (string.IsNullOrWhiteSpace(userName) || UsersDb == null)
            {
                return 0;
            }

            IUserItem user = UsersDb.GetSingleUser(userName);
            return user == null ? 0 : user.UserId;
        }

        private string GetUnavailableMessage(int userId)
        {
            if (!IsCurrentUserAuthenticated())
            {
                return "请先登录后再确认员工资料。";
            }

            if (EmployeeProfileConfirmationDb == null || !EmployeeProfileConfirmationDb.IsSchemaAvailable())
            {
                return "员工资料确认模块尚未完成数据库初始化。";
            }

            return userId <= 0
                ? "当前登录账号无法解析到门户用户。"
                : "当前账号没有可确认的在职员工资料。";
        }

        private bool IsCurrentUserAuthenticated()
        {
            return Context != null &&
                   Context.User != null &&
                   Context.User.Identity != null &&
                   Context.User.Identity.IsAuthenticated;
        }

        private string GetCurrentUserName()
        {
            return IsCurrentUserAuthenticated() ? Context.User.Identity.Name : string.Empty;
        }

        private void ShowMessage(string message)
        {
            MessageLabel.Text = Server.HtmlEncode(message ?? string.Empty);
        }

        private string EncodeDisplay(string value)
        {
            return Server.HtmlEncode(value ?? string.Empty);
        }

        private static string EmptyToNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }

        private static string FormatUtc(DateTime? value)
        {
            return value.HasValue
                ? value.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture)
                : "(not confirmed)";
        }
    }
}
