using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>基于 P6.3 表的门户账号与员工绑定后台写入实现。</zh-CN>
    ///   <en>Administration write implementation for Portal-user to employee bindings backed by P6.3 tables.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本实现只维护绑定表本身。运营审计、安全版本递增和登录票据失效由调用方在成功后完成，以便页面层能够把用户上下文和审计摘要控制在安全范围内。</zh-CN>
    ///   <en>This implementation maintains only the binding table. Operations audit, security-version increments, and ticket invalidation are completed by callers after success so page code can control user context and safe audit summaries.</en>
    /// </lang>
    /// </remarks>
    public class UserEmployeeBindingAdminDb : IUserEmployeeBindingAdminDb
    {
        private const string EmployeeTableName = "PortalBiz_Employees";
        private const string BindingTableName = "PortalBiz_UserEmployeeBindings";
        private readonly PortalBizDbContext _context;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化门户账号与员工绑定写入实现。</zh-CN>
        ///   <en>Initializes the Portal-user to employee binding write implementation.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>企业业务基础数据上下文。</zh-CN>
        ///   <en>Enterprise business foundation data context.</en>
        /// </l>
        /// </param>
        public UserEmployeeBindingAdminDb(PortalBizDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查 P6.3 绑定所需表是否可用。</zh-CN>
        ///   <en>Checks whether the P6.3 binding tables are available.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>全部所需表存在时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when all required tables exist.</en>
        /// </l>
        /// </returns>
        public bool IsSchemaAvailable()
        {
            return HasTable(EmployeeTableName) &&
                   HasTable(BindingTableName) &&
                   HasTable("Portal_Users");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按标识读取绑定记录。</zh-CN>
        ///   <en>Reads a binding row by id.</en>
        /// </lang>
        /// </summary>
        /// <param name="bindingId">
        /// <l>
        ///   <zh-CN>绑定记录标识。</zh-CN>
        ///   <en>Binding row identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>绑定记录；不存在或表不可用时返回 <c>null</c>。</zh-CN>
        ///   <en>Binding row, or <c>null</c> when it does not exist or schema is unavailable.</en>
        /// </l>
        /// </returns>
        public IUserEmployeeBindingInfo GetBindingById(int bindingId)
        {
            if (bindingId <= 0 || !IsSchemaAvailable())
            {
                return null;
            }

            var row = _context.Database.SqlQuery<UserEmployeeBindingProjection>(
                GetBindingSql("[Binding].[BindingId] = @BindingId"),
                IntParameter("@BindingId", bindingId)).SingleOrDefault();
            return row == null ? null : CreateBindingInfo(row);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>建立一个当前有效绑定。</zh-CN>
        ///   <en>Creates one current active binding.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>账号、员工、状态、时间和操作者信息组成的保存请求。</zh-CN>
        ///   <en>Save request containing user, employee, status, timestamp and actor information.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>写入结果；失败消息应保持低敏，可直接展示给管理员。</zh-CN>
        ///   <en>Write result; failure messages should remain low-sensitivity and may be shown to administrators.</en>
        /// </l>
        /// </returns>
        public EmployeeDirectoryWriteResult BindUserToEmployee(UserEmployeeBindingSaveRequest request)
        {
            if (!IsSchemaAvailable())
            {
                return EmployeeDirectoryWriteResult.Failed("P6.3 binding schema is unavailable.");
            }

            UserEmployeeBindingSaveRequest normalized = NormalizeSaveRequest(request);
            string validationMessage;
            if (!TryValidateSaveRequest(normalized, out validationMessage))
            {
                return EmployeeDirectoryWriteResult.Failed(validationMessage);
            }

            if (!UserExists(normalized.UserId))
            {
                return EmployeeDirectoryWriteResult.Missing("The Portal user does not exist.");
            }

            EmployeeBindingProjection employee = GetEmployeeByCode(normalized.EmployeeCode);
            if (employee == null)
            {
                return EmployeeDirectoryWriteResult.Missing("The employee does not exist.");
            }

            if (!IsEmployeeStatusAllowedForActiveBinding(employee.EmploymentStatus))
            {
                return EmployeeDirectoryWriteResult.Failed("Only Active or Pending employees may receive an active binding.");
            }

            if (HasActiveBindingForUser(normalized.UserId))
            {
                return EmployeeDirectoryWriteResult.Failed("The Portal user already has an active employee binding.");
            }

            if (HasActiveBindingForEmployee(employee.EmployeeId))
            {
                return EmployeeDirectoryWriteResult.Failed("The employee already has an active Portal-user binding.");
            }

            int bindingId = _context.Database.SqlQuery<int>(
                @"
INSERT INTO [dbo].[PortalBiz_UserEmployeeBindings]
    ([UserId], [EmployeeId], [BindingStatus], [BoundBy], [Reason], [UpdatedBy])
VALUES
    (@UserId, @EmployeeId, N'Active', @ActorName, @Reason, @ActorName);

SELECT CAST(SCOPE_IDENTITY() AS INT);",
                IntParameter("@UserId", normalized.UserId),
                IntParameter("@EmployeeId", employee.EmployeeId),
                TextParameter("@ActorName", normalized.ActorName, 100),
                TextParameter("@Reason", normalized.Reason, 200)).Single();

            return EmployeeDirectoryWriteResult.Success(bindingId, "Employee binding saved.");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>结束一个当前有效绑定。</zh-CN>
        ///   <en>Ends one current active binding.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>解绑请求，包含绑定标识、结束时间和操作者信息。</zh-CN>
        ///   <en>Unbind request containing binding id, end timestamp and actor information.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>写入结果；未找到当前有效绑定时返回失败。</zh-CN>
        ///   <en>Write result; returns failure when no current active binding is found.</en>
        /// </l>
        /// </returns>
        public EmployeeDirectoryWriteResult EndBinding(UserEmployeeBindingEndRequest request)
        {
            if (!IsSchemaAvailable())
            {
                return EmployeeDirectoryWriteResult.Failed("P6.3 binding schema is unavailable.");
            }

            UserEmployeeBindingEndRequest normalized = NormalizeEndRequest(request);
            if (normalized.BindingId <= 0)
            {
                return EmployeeDirectoryWriteResult.Failed("Binding id is required.");
            }

            IUserEmployeeBindingInfo binding = GetBindingById(normalized.BindingId);
            if (binding == null)
            {
                return EmployeeDirectoryWriteResult.Missing("The binding no longer exists.");
            }

            if (!string.Equals(binding.BindingStatus, PortalUserEmployeeBindingStatuses.Active, StringComparison.Ordinal))
            {
                return EmployeeDirectoryWriteResult.ConcurrencyConflict("The binding is no longer active. Reload before saving again.");
            }

            int affectedRows = _context.Database.ExecuteSqlCommand(
                @"
UPDATE [dbo].[PortalBiz_UserEmployeeBindings]
SET [BindingStatus] = N'Ended',
    [EndedUtc] = SYSUTCDATETIME(),
    [EndedBy] = @ActorName,
    [Reason] = @Reason,
    [UpdatedUtc] = SYSUTCDATETIME(),
    [UpdatedBy] = @ActorName
WHERE [BindingId] = @BindingId
  AND [BindingStatus] = N'Active';",
                IntParameter("@BindingId", normalized.BindingId),
                TextParameter("@ActorName", normalized.ActorName, 100),
                TextParameter("@Reason", normalized.Reason, 200));

            if (affectedRows == 1)
            {
                return EmployeeDirectoryWriteResult.Success(normalized.BindingId, "Employee binding ended.");
            }

            return BindingExists(normalized.BindingId)
                ? EmployeeDirectoryWriteResult.ConcurrencyConflict("The binding was changed by another request. Reload before saving again.")
                : EmployeeDirectoryWriteResult.Missing("The binding no longer exists.");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证新增绑定请求的基础字段。</zh-CN>
        ///   <en>Validates the basic fields of a new binding request.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>已归一化的保存请求。</zh-CN>
        ///   <en>Normalized save request.</en>
        /// </l>
        /// </param>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>校验失败时可展示的错误消息。</zh-CN>
        ///   <en>Displayable error message when validation fails.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>请求满足基础字段要求时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the request satisfies basic field requirements.</en>
        /// </l>
        /// </returns>
        private bool TryValidateSaveRequest(UserEmployeeBindingSaveRequest request, out string message)
        {
            if (request.UserId <= 0)
            {
                message = "Portal user id is required.";
                return false;
            }

            if (string.IsNullOrEmpty(request.EmployeeCode))
            {
                message = "Employee code is required.";
                return false;
            }

            if (request.EmployeeCode.Length > 64)
            {
                message = "Employee code cannot exceed 64 characters.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>确认门户用户是否存在。</zh-CN>
        ///   <en>Checks whether a Portal user exists.</en>
        /// </lang>
        /// </summary>
        private bool UserExists(int userId)
        {
            return ScalarInt(
                "SELECT COUNT(*) FROM [dbo].[Portal_Users] WHERE [UserID] = @UserId",
                IntParameter("@UserId", userId)) > 0;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>确认绑定记录是否仍存在。</zh-CN>
        ///   <en>Checks whether a binding record still exists.</en>
        /// </lang>
        /// </summary>
        private bool BindingExists(int bindingId)
        {
            return ScalarInt(
                "SELECT COUNT(*) FROM [dbo].[PortalBiz_UserEmployeeBindings] WHERE [BindingId] = @BindingId",
                IntParameter("@BindingId", bindingId)) > 0;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查用户是否已有有效员工绑定。</zh-CN>
        ///   <en>Checks whether the user already has an active employee binding.</en>
        /// </lang>
        /// </summary>
        private bool HasActiveBindingForUser(int userId)
        {
            return ScalarInt(
                @"
SELECT COUNT(*)
FROM [dbo].[PortalBiz_UserEmployeeBindings]
WHERE [UserId] = @UserId
  AND [BindingStatus] = N'Active';",
                IntParameter("@UserId", userId)) > 0;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查员工是否已有有效门户账号绑定。</zh-CN>
        ///   <en>Checks whether the employee already has an active Portal-user binding.</en>
        /// </lang>
        /// </summary>
        private bool HasActiveBindingForEmployee(int employeeId)
        {
            return ScalarInt(
                @"
SELECT COUNT(*)
FROM [dbo].[PortalBiz_UserEmployeeBindings]
WHERE [EmployeeId] = @EmployeeId
  AND [BindingStatus] = N'Active';",
                IntParameter("@EmployeeId", employeeId)) > 0;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按员工号读取可绑定员工的轻量投影。</zh-CN>
        ///   <en>Reads a lightweight bindable employee projection by employee code.</en>
        /// </lang>
        /// </summary>
        private EmployeeBindingProjection GetEmployeeByCode(string employeeCode)
        {
            return _context.Database.SqlQuery<EmployeeBindingProjection>(
                @"
SELECT TOP (1)
    [EmployeeId],
    [EmployeeCode],
    [DisplayName],
    [EmploymentStatus]
FROM [dbo].[PortalBiz_Employees]
WHERE [EmployeeCode] = @EmployeeCode;",
                TextParameter("@EmployeeCode", employeeCode, 64)).SingleOrDefault();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查指定表是否存在并可访问。</zh-CN>
        ///   <en>Checks whether the specified table exists and is accessible.</en>
        /// </lang>
        /// </summary>
        private bool HasTable(string tableName)
        {
            try
            {
                string sql = string.Format(
                    "SELECT CASE WHEN OBJECT_ID(N'[dbo].[{0}]', N'U') IS NULL THEN 0 ELSE 1 END",
                    tableName);
                return _context.Database.SqlQuery<int>(sql).Single() == 1;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>执行返回单个整数的 SQL 查询。</zh-CN>
        ///   <en>Executes a SQL query that returns a single integer.</en>
        /// </lang>
        /// </summary>
        private int ScalarInt(string sql, params object[] parameters)
        {
            return _context.Database.SqlQuery<int>(sql, parameters).Single();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>生成按指定谓词读取绑定详情的 SQL。</zh-CN>
        ///   <en>Builds SQL for reading binding details by the specified predicate.</en>
        /// </lang>
        /// </summary>
        private static string GetBindingSql(string predicate)
        {
            return @"
SELECT TOP (1)
    [Binding].[BindingId],
    [Binding].[UserId],
    [User].[Name] AS [UserName],
    [Binding].[EmployeeId],
    [Employee].[EmployeeCode],
    [Employee].[DisplayName] AS [EmployeeDisplayName],
    [Binding].[BindingStatus],
    [Binding].[BoundUtc],
    [Binding].[BoundBy],
    [Binding].[EndedUtc],
    [Binding].[EndedBy],
    [Binding].[Reason]
FROM [dbo].[PortalBiz_UserEmployeeBindings] AS [Binding]
INNER JOIN [dbo].[PortalBiz_Employees] AS [Employee]
    ON [Employee].[EmployeeId] = [Binding].[EmployeeId]
INNER JOIN [dbo].[Portal_Users] AS [User]
    ON [User].[UserID] = [Binding].[UserId]
WHERE " + predicate + @"
ORDER BY [Binding].[BoundUtc] DESC, [Binding].[BindingId] DESC;";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把绑定 SQL 投影转换为跨层只读契约。</zh-CN>
        ///   <en>Converts a binding SQL projection into the cross-layer read-only contract.</en>
        /// </lang>
        /// </summary>
        private static IUserEmployeeBindingInfo CreateBindingInfo(UserEmployeeBindingProjection row)
        {
            return new UserEmployeeBindingInfo(
                row.BindingId,
                row.UserId,
                row.UserName,
                row.EmployeeId,
                row.EmployeeCode,
                row.EmployeeDisplayName,
                row.BindingStatus,
                row.BoundUtc,
                row.BoundBy,
                row.EndedUtc,
                row.EndedBy,
                row.Reason);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断员工状态是否允许建立有效绑定。</zh-CN>
        ///   <en>Determines whether an employee status may receive an active binding.</en>
        /// </lang>
        /// </summary>
        private static bool IsEmployeeStatusAllowedForActiveBinding(string status)
        {
            return string.Equals(status, PortalEmployeeStatuses.Active, StringComparison.Ordinal) ||
                   string.Equals(status, PortalEmployeeStatuses.Pending, StringComparison.Ordinal);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>归一化新增或替换绑定请求。</zh-CN>
        ///   <en>Normalizes a create-or-replace binding request.</en>
        /// </lang>
        /// </summary>
        private static UserEmployeeBindingSaveRequest NormalizeSaveRequest(UserEmployeeBindingSaveRequest request)
        {
            request = request ?? new UserEmployeeBindingSaveRequest();
            return new UserEmployeeBindingSaveRequest
            {
                UserId = request.UserId,
                EmployeeCode = NormalizeOptional(request.EmployeeCode),
                Reason = NormalizeReason(request.Reason),
                ActorName = NormalizeActor(request.ActorName)
            };
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>归一化结束绑定请求。</zh-CN>
        ///   <en>Normalizes an end-binding request.</en>
        /// </lang>
        /// </summary>
        private static UserEmployeeBindingEndRequest NormalizeEndRequest(UserEmployeeBindingEndRequest request)
        {
            request = request ?? new UserEmployeeBindingEndRequest();
            return new UserEmployeeBindingEndRequest
            {
                BindingId = request.BindingId,
                Reason = NormalizeReason(request.Reason),
                ActorName = NormalizeActor(request.ActorName)
            };
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>归一化可选文本字段。</zh-CN>
        ///   <en>Normalizes an optional text field.</en>
        /// </lang>
        /// </summary>
        private static string NormalizeOptional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>归一化绑定变更原因，并限制最大长度。</zh-CN>
        ///   <en>Normalizes a binding-change reason and enforces the maximum length.</en>
        /// </lang>
        /// </summary>
        private static string NormalizeReason(string value)
        {
            string reason = NormalizeOptional(value);
            return reason.Length > 200 ? reason.Substring(0, 200) : reason;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>归一化操作人标识，缺省时使用后台默认值。</zh-CN>
        ///   <en>Normalizes the actor identifier, using the administration default when missing.</en>
        /// </lang>
        /// </summary>
        private static string NormalizeActor(string value)
        {
            string actor = NormalizeOptional(value);
            if (string.IsNullOrEmpty(actor))
            {
                return "admin";
            }

            return actor.Length > 100 ? actor.Substring(0, 100) : actor;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建整数 SQL 参数。</zh-CN>
        ///   <en>Creates an integer SQL parameter.</en>
        /// </lang>
        /// </summary>
        private static SqlParameter IntParameter(string name, int value)
        {
            return new SqlParameter(name, SqlDbType.Int) { Value = value };
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建文本 SQL 参数，空文本按数据库空值传递。</zh-CN>
        ///   <en>Creates a text SQL parameter, passing empty text as a database null.</en>
        /// </lang>
        /// </summary>
        private static SqlParameter TextParameter(string name, string value, int size)
        {
            return new SqlParameter(name, SqlDbType.NVarChar, size)
            {
                Value = string.IsNullOrEmpty(value) ? (object)DBNull.Value : value
            };
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>用于绑定创建校验的员工轻量投影。</zh-CN>
        ///   <en>Lightweight employee projection used for binding-create validation.</en>
        /// </lang>
        /// </summary>
        private sealed class EmployeeBindingProjection
        {
            /// <summary>
            /// <lang>
            ///   <zh-CN>员工标识。</zh-CN>
            ///   <en>Employee identifier.</en>
            /// </lang>
            /// </summary>
            public int EmployeeId { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>员工号。</zh-CN>
            ///   <en>Employee code.</en>
            /// </lang>
            /// </summary>
            public string EmployeeCode { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>员工显示名。</zh-CN>
            ///   <en>Employee display name.</en>
            /// </lang>
            /// </summary>
            public string DisplayName { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>员工状态。</zh-CN>
            ///   <en>Employee status.</en>
            /// </lang>
            /// </summary>
            public string EmploymentStatus { get; set; }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>门户用户与员工绑定详情 SQL 投影。</zh-CN>
        ///   <en>SQL projection for Portal-user to employee binding details.</en>
        /// </lang>
        /// </summary>
        private sealed class UserEmployeeBindingProjection
        {
            /// <summary>
            /// <lang>
            ///   <zh-CN>绑定标识。</zh-CN>
            ///   <en>Binding identifier.</en>
            /// </lang>
            /// </summary>
            public int BindingId { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>门户用户标识。</zh-CN>
            ///   <en>Portal user identifier.</en>
            /// </lang>
            /// </summary>
            public int UserId { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>门户用户名。</zh-CN>
            ///   <en>Portal user name.</en>
            /// </lang>
            /// </summary>
            public string UserName { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>员工标识。</zh-CN>
            ///   <en>Employee identifier.</en>
            /// </lang>
            /// </summary>
            public int EmployeeId { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>员工号。</zh-CN>
            ///   <en>Employee code.</en>
            /// </lang>
            /// </summary>
            public string EmployeeCode { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>员工显示名。</zh-CN>
            ///   <en>Employee display name.</en>
            /// </lang>
            /// </summary>
            public string EmployeeDisplayName { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>绑定状态。</zh-CN>
            ///   <en>Binding status.</en>
            /// </lang>
            /// </summary>
            public string BindingStatus { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>绑定创建 UTC 时间。</zh-CN>
            ///   <en>Binding creation UTC time.</en>
            /// </lang>
            /// </summary>
            public DateTime BoundUtc { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>绑定创建人。</zh-CN>
            ///   <en>User who created the binding.</en>
            /// </lang>
            /// </summary>
            public string BoundBy { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>绑定结束 UTC 时间。</zh-CN>
            ///   <en>Binding end UTC time.</en>
            /// </lang>
            /// </summary>
            public DateTime? EndedUtc { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>绑定结束操作人。</zh-CN>
            ///   <en>User who ended the binding.</en>
            /// </lang>
            /// </summary>
            public string EndedBy { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>绑定变更原因。</zh-CN>
            ///   <en>Reason for the binding change.</en>
            /// </lang>
            /// </summary>
            public string Reason { get; set; }
        }
    }
}
