using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：基于 P6.3 表的门户账号与员工绑定后台写入实现。
    ///
    /// English: Administration write implementation for Portal-user to employee bindings backed by P6.3 tables.
    /// </summary>
    /// <remarks>
    /// 中文：本实现只维护绑定表本身。运营审计、安全版本递增和登录票据失效由调用方在成功后完成，
    /// 以便页面层能够把用户上下文和审计摘要控制在安全范围内。
    ///
    /// English: This implementation maintains only the binding table. Operations audit, security-version increments,
    /// and ticket invalidation are completed by callers after success so page code can control user context and safe
    /// audit summaries.
    /// </remarks>
    public class UserEmployeeBindingAdminDb : IUserEmployeeBindingAdminDb
    {
        private const string EmployeeTableName = "PortalBiz_Employees";
        private const string BindingTableName = "PortalBiz_UserEmployeeBindings";
        private readonly PortalBizDbContext _context;

        /// <summary>
        /// 中文：初始化门户账号与员工绑定写入实现。
        ///
        /// English: Initializes the Portal-user to employee binding write implementation.
        /// </summary>
        /// <param name="context">中文：企业业务基础数据上下文。English: Enterprise business foundation data context.</param>
        public UserEmployeeBindingAdminDb(PortalBizDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public bool IsSchemaAvailable()
        {
            return HasTable(EmployeeTableName) &&
                   HasTable(BindingTableName) &&
                   HasTable("Portal_Users");
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        private bool UserExists(int userId)
        {
            return ScalarInt(
                "SELECT COUNT(*) FROM [dbo].[Portal_Users] WHERE [UserID] = @UserId",
                IntParameter("@UserId", userId)) > 0;
        }

        private bool BindingExists(int bindingId)
        {
            return ScalarInt(
                "SELECT COUNT(*) FROM [dbo].[PortalBiz_UserEmployeeBindings] WHERE [BindingId] = @BindingId",
                IntParameter("@BindingId", bindingId)) > 0;
        }

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

        private int ScalarInt(string sql, params object[] parameters)
        {
            return _context.Database.SqlQuery<int>(sql, parameters).Single();
        }

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

        private static bool IsEmployeeStatusAllowedForActiveBinding(string status)
        {
            return string.Equals(status, PortalEmployeeStatuses.Active, StringComparison.Ordinal) ||
                   string.Equals(status, PortalEmployeeStatuses.Pending, StringComparison.Ordinal);
        }

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

        private static string NormalizeOptional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string NormalizeReason(string value)
        {
            string reason = NormalizeOptional(value);
            return reason.Length > 200 ? reason.Substring(0, 200) : reason;
        }

        private static string NormalizeActor(string value)
        {
            string actor = NormalizeOptional(value);
            if (string.IsNullOrEmpty(actor))
            {
                return "admin";
            }

            return actor.Length > 100 ? actor.Substring(0, 100) : actor;
        }

        private static SqlParameter IntParameter(string name, int value)
        {
            return new SqlParameter(name, SqlDbType.Int) { Value = value };
        }

        private static SqlParameter TextParameter(string name, string value, int size)
        {
            return new SqlParameter(name, SqlDbType.NVarChar, size)
            {
                Value = string.IsNullOrEmpty(value) ? (object)DBNull.Value : value
            };
        }

        private sealed class EmployeeBindingProjection
        {
            public int EmployeeId { get; set; }
            public string EmployeeCode { get; set; }
            public string DisplayName { get; set; }
            public string EmploymentStatus { get; set; }
        }

        private sealed class UserEmployeeBindingProjection
        {
            public int BindingId { get; set; }
            public int UserId { get; set; }
            public string UserName { get; set; }
            public int EmployeeId { get; set; }
            public string EmployeeCode { get; set; }
            public string EmployeeDisplayName { get; set; }
            public string BindingStatus { get; set; }
            public DateTime BoundUtc { get; set; }
            public string BoundBy { get; set; }
            public DateTime? EndedUtc { get; set; }
            public string EndedBy { get; set; }
            public string Reason { get; set; }
        }
    }
}
