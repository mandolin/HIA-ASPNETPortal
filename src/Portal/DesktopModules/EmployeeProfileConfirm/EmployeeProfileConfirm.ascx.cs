using System;
using System.Globalization;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>员工资料确认业务模块样板。</zh-CN>
    ///   <en>Business-module sample for employee-profile confirmation.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>第一版只允许已登录且拥有 Active 员工绑定的用户查看并确认自己的低敏员工基础资料。它不提供资料编辑、附件上传、在线脚本、外部资源加载或 HR 同步。</zh-CN>
    ///   <en>The first version allows only signed-in users with an active employee binding to view and confirm their own low-sensitivity employee foundation profile. It provides no profile editing, attachment upload, online script, external-resource loading, or HR synchronization.</en>
    /// </lang>
    /// </remarks>
    public partial class EmployeeProfileConfirm : PortalModuleControl<EmployeeProfileConfirm>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>用户数据访问服务，用于把当前登录名解析为门户用户标识。</zh-CN>
        ///   <en>User data service used to resolve the current sign-in name to a Portal user identifier.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IUsersDb UsersDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工资料确认模块数据访问服务。</zh-CN>
        ///   <en>Employee-profile confirmation module data service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IEmployeeProfileConfirmationDb EmployeeProfileConfirmationDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化员工资料确认模块。</zh-CN>
        ///   <en>Initializes the employee-profile confirmation module.</en>
        /// </lang>
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // <lang>
                //   <zh-CN>资料确认模块只在首次加载时绑定，确认按钮回发完成后由事件方法显式刷新，避免重复覆盖提示信息。</zh-CN>
                //   <en>The profile-confirmation module binds only on initial load; after the confirm postback, the event handler refreshes explicitly so messages are not overwritten.</en>
                // </lang>
                BindProfile();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>确认当前绑定员工资料。</zh-CN>
        ///   <en>Confirms the current bound employee profile.</en>
        /// </lang>
        /// </summary>
        protected void ConfirmButton_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>确认动作重新读取当前绑定资料，避免用户在页面加载后绑定状态变化时仍提交旧员工标识。</zh-CN>
            //   <en>The confirm action reloads the current bound profile so a user cannot submit a stale employee identifier after binding state changes.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>写入确认快照后刷新模块，确保最近确认时间来自数据库返回路径而不是页面临时状态。</zh-CN>
            //   <en>After writing the confirmation snapshot, the module refreshes so the latest confirmation time comes from the database path rather than transient page state.</en>
            // </lang>
            BindProfile();
            ShowMessage("资料确认已记录。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定当前用户可确认的员工资料。</zh-CN>
        ///   <en>Binds the employee profile that the current user can confirm.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>所有展示字段都经过 HTML 编码；空值统一转换为低敏占位文本，避免页面出现空白难以判断。</zh-CN>
        ///   <en>All display fields are HTML-encoded; empty values are converted to low-sensitivity placeholders so blank UI does not obscure the state.</en>
        /// </lang>
        /// </remarks>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前用户的可确认员工资料。</zh-CN>
        ///   <en>Reads the confirmable employee profile for the current user.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>当前门户用户标识。</zh-CN>
        ///   <en>Current Portal user identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>可确认资料视图；未登录、缺表或没有 Active 绑定时返回 <c>null</c>。</zh-CN>
        ///   <en>Confirmable profile view, or <c>null</c> when unauthenticated, schema is missing, or no active binding exists.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>把当前登录身份解析为门户用户标识。</zh-CN>
        ///   <en>Resolves the current sign-in identity to a Portal user identifier.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>门户用户标识；无法解析时返回 `0`。</zh-CN>
        ///   <en>Portal user identifier, or `0` when it cannot be resolved.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>当前沿用旧 `UsersDb.GetSingleUser` 登录名查找；工号登录最终仍会映射回门户用户标识。</zh-CN>
        ///   <en>This currently uses the legacy `UsersDb.GetSingleUser` lookup by sign-in name; employee-code sign-in still maps back to a Portal user identifier.</en>
        /// </lang>
        /// </remarks>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>生成资料不可确认时的低敏提示。</zh-CN>
        ///   <en>Builds the low-sensitivity message used when the profile cannot be confirmed.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>当前解析出的门户用户标识。</zh-CN>
        ///   <en>Currently resolved Portal user identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>适合直接展示给用户的提示文本。</zh-CN>
        ///   <en>Message text suitable for direct user display.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断当前请求是否已有认证用户。</zh-CN>
        ///   <en>Determines whether the current request has an authenticated user.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>存在认证身份时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when an authenticated identity exists.</en>
        /// </l>
        /// </returns>
        private bool IsCurrentUserAuthenticated()
        {
            return Context != null &&
                   Context.User != null &&
                   Context.User.Identity != null &&
                   Context.User.Identity.IsAuthenticated;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前登录名。</zh-CN>
        ///   <en>Reads the current sign-in name.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>认证身份名称；未登录时为空字符串。</zh-CN>
        ///   <en>Authenticated identity name, or an empty string when unauthenticated.</en>
        /// </l>
        /// </returns>
        private string GetCurrentUserName()
        {
            return IsCurrentUserAuthenticated() ? Context.User.Identity.Name : string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>显示模块级提示。</zh-CN>
        ///   <en>Displays a module-level message.</en>
        /// </lang>
        /// </summary>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>提示文本；写入控件前统一 HTML 编码。</zh-CN>
        ///   <en>Message text; HTML-encoded before being written to the control.</en>
        /// </l>
        /// </param>
        private void ShowMessage(string message)
        {
            MessageLabel.Text = Server.HtmlEncode(message ?? string.Empty);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>编码展示字段。</zh-CN>
        ///   <en>Encodes a display field.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>原始展示值。</zh-CN>
        ///   <en>Raw display value.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>HTML 编码后的文本。</zh-CN>
        ///   <en>HTML-encoded text.</en>
        /// </l>
        /// </returns>
        private string EncodeDisplay(string value)
        {
            return Server.HtmlEncode(value ?? string.Empty);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把空展示值转换为占位文本。</zh-CN>
        ///   <en>Converts an empty display value to placeholder text.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>原始文本。</zh-CN>
        ///   <en>Raw text.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>非空文本或 `(none)`。</zh-CN>
        ///   <en>Non-empty text or `(none)`.</en>
        /// </l>
        /// </returns>
        private static string EmptyToNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>格式化最近确认 UTC 时间。</zh-CN>
        ///   <en>Formats the latest confirmation UTC time.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>可为空的确认时间。</zh-CN>
        ///   <en>Optional confirmation timestamp.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>固定 UTC 展示文本；未确认时返回 `(not confirmed)`。</zh-CN>
        ///   <en>Fixed UTC display text, or `(not confirmed)` when no confirmation exists.</en>
        /// </l>
        /// </returns>
        private static string FormatUtc(DateTime? value)
        {
            return value.HasValue
                ? value.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture)
                : "(not confirmed)";
        }
    }
}
