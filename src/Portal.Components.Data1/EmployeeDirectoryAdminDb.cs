using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：基于 P6.3 表的员工和组织后台最小维护实现。
    ///
    /// English: Minimal administration maintenance implementation for employees and organization units backed by P6.3 tables.
    /// </summary>
    /// <remarks>
    /// 中文：本实现只负责组织和员工主数据写入。运营审计由页面在成功后写入；账号员工绑定、安全版本递增和员工工号登录留给 P6.3-S5。
    ///
    /// English: This implementation writes only organization and employee master data. Pages write operations audit
    /// after success; user-employee binding, security-version increments, and employee-code sign-in remain in P6.3-S5.
    /// </remarks>
    public class EmployeeDirectoryAdminDb : IEmployeeDirectoryAdminDb
    {
        private const string OrganizationTableName = "PortalBiz_OrganizationUnits";
        private const string EmployeeTableName = "PortalBiz_Employees";
        private const string BindingTableName = "PortalBiz_UserEmployeeBindings";
        private static readonly Regex EmployeeCodePattern = new Regex(
            "^[A-Za-z0-9._-]{2,64}$",
            RegexOptions.Compiled);

        private readonly PortalBizDbContext _context;

        /// <summary>
        /// 中文：初始化员工和组织后台维护实现。
        ///
        /// English: Initializes the employee and organization administration maintenance implementation.
        /// </summary>
        /// <param name="context">中文：企业业务基础数据上下文。English: Enterprise business foundation data context.</param>
        public EmployeeDirectoryAdminDb(PortalBizDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public bool IsSchemaAvailable()
        {
            return HasTable(OrganizationTableName) &&
                   HasTable(EmployeeTableName) &&
                   HasTable(BindingTableName);
        }

        /// <inheritdoc />
        public IOrganizationUnitInfo GetOrganizationUnitById(int organizationUnitId)
        {
            if (organizationUnitId <= 0 || !IsSchemaAvailable())
            {
                return null;
            }

            OrganizationUnitProjection row = _context.Database.SqlQuery<OrganizationUnitProjection>(
                @"
SELECT
    [OrganizationUnitId],
    [ParentOrganizationUnitId],
    [OrganizationCode],
    [DisplayName],
    [SortOrder],
    [IsActive],
    [CreatedUtc],
    [UpdatedUtc]
FROM [dbo].[PortalBiz_OrganizationUnits]
WHERE [OrganizationUnitId] = @OrganizationUnitId;",
                IntParameter("@OrganizationUnitId", organizationUnitId)).SingleOrDefault();

            return row == null ? null : CreateOrganizationInfo(row);
        }

        /// <inheritdoc />
        public IEmployeeInfo GetEmployeeById(int employeeId)
        {
            if (employeeId <= 0 || !IsSchemaAvailable())
            {
                return null;
            }

            EmployeeProjection row = _context.Database.SqlQuery<EmployeeProjection>(
                @"
SELECT
    [Employee].[EmployeeId],
    [Employee].[EmployeeCode],
    [Employee].[DisplayName],
    [Employee].[PreferredName],
    [Employee].[WorkEmail],
    [Employee].[OrganizationUnitId],
    [Organization].[DisplayName] AS [OrganizationDisplayName],
    [Employee].[EmploymentStatus],
    [Employee].[JoinedUtc],
    [Employee].[LeftUtc],
    [Employee].[SourceSystem],
    [Employee].[UpdatedUtc]
FROM [dbo].[PortalBiz_Employees] AS [Employee]
LEFT JOIN [dbo].[PortalBiz_OrganizationUnits] AS [Organization]
    ON [Organization].[OrganizationUnitId] = [Employee].[OrganizationUnitId]
WHERE [Employee].[EmployeeId] = @EmployeeId;",
                IntParameter("@EmployeeId", employeeId)).SingleOrDefault();

            return row == null ? null : CreateEmployeeInfo(row);
        }

        /// <inheritdoc />
        public EmployeeDirectoryWriteResult SaveOrganizationUnit(OrganizationUnitSaveRequest request)
        {
            if (!IsSchemaAvailable())
            {
                return EmployeeDirectoryWriteResult.Failed("P6.3 schema is unavailable.");
            }

            OrganizationUnitSaveRequest normalized = NormalizeOrganizationRequest(request);
            string validationMessage;
            if (!TryValidateOrganizationRequest(normalized, out validationMessage))
            {
                return EmployeeDirectoryWriteResult.Failed(validationMessage);
            }

            if (normalized.OrganizationUnitId <= 0)
            {
                int newId = _context.Database.SqlQuery<int>(
                    @"
INSERT INTO [dbo].[PortalBiz_OrganizationUnits]
    ([ParentOrganizationUnitId], [OrganizationCode], [DisplayName], [SortOrder], [IsActive], [CreatedBy], [UpdatedBy])
VALUES
    (@ParentOrganizationUnitId, @OrganizationCode, @DisplayName, @SortOrder, @IsActive, @ActorName, @ActorName);
SELECT CAST(SCOPE_IDENTITY() AS int);",
                    NullableIntParameter("@ParentOrganizationUnitId", normalized.ParentOrganizationUnitId),
                    TextParameter("@OrganizationCode", normalized.OrganizationCode),
                    TextParameter("@DisplayName", normalized.DisplayName),
                    IntParameter("@SortOrder", normalized.SortOrder),
                    BoolParameter("@IsActive", normalized.IsActive),
                    TextParameter("@ActorName", normalized.ActorName)).Single();

                return EmployeeDirectoryWriteResult.Success(newId, "Organization unit saved.");
            }

            if (!normalized.OriginalUpdatedUtc.HasValue)
            {
                return EmployeeDirectoryWriteResult.Failed("The organization unit was not loaded with an update timestamp.");
            }

            int affectedRows = _context.Database.ExecuteSqlCommand(
                @"
UPDATE [dbo].[PortalBiz_OrganizationUnits]
SET [ParentOrganizationUnitId] = @ParentOrganizationUnitId,
    [OrganizationCode] = @OrganizationCode,
    [DisplayName] = @DisplayName,
    [SortOrder] = @SortOrder,
    [IsActive] = @IsActive,
    [UpdatedUtc] = SYSUTCDATETIME(),
    [UpdatedBy] = @ActorName
WHERE [OrganizationUnitId] = @OrganizationUnitId
  AND [UpdatedUtc] = @OriginalUpdatedUtc;",
                NullableIntParameter("@ParentOrganizationUnitId", normalized.ParentOrganizationUnitId),
                TextParameter("@OrganizationCode", normalized.OrganizationCode),
                TextParameter("@DisplayName", normalized.DisplayName),
                IntParameter("@SortOrder", normalized.SortOrder),
                BoolParameter("@IsActive", normalized.IsActive),
                TextParameter("@ActorName", normalized.ActorName),
                IntParameter("@OrganizationUnitId", normalized.OrganizationUnitId),
                DateTime2Parameter("@OriginalUpdatedUtc", normalized.OriginalUpdatedUtc.Value));

            if (affectedRows == 1)
            {
                return EmployeeDirectoryWriteResult.Success(normalized.OrganizationUnitId, "Organization unit saved.");
            }

            return OrganizationExists(normalized.OrganizationUnitId)
                ? EmployeeDirectoryWriteResult.ConcurrencyConflict("The organization unit was changed by another request. Reload before saving again.")
                : EmployeeDirectoryWriteResult.Missing("The organization unit no longer exists.");
        }

        /// <inheritdoc />
        public EmployeeDirectoryWriteResult SaveEmployee(EmployeeSaveRequest request)
        {
            if (!IsSchemaAvailable())
            {
                return EmployeeDirectoryWriteResult.Failed("P6.3 schema is unavailable.");
            }

            EmployeeSaveRequest normalized = NormalizeEmployeeRequest(request);
            string validationMessage;
            if (!TryValidateEmployeeRequest(normalized, out validationMessage))
            {
                return EmployeeDirectoryWriteResult.Failed(validationMessage);
            }

            if (normalized.EmployeeId <= 0)
            {
                int newId = _context.Database.SqlQuery<int>(
                    @"
INSERT INTO [dbo].[PortalBiz_Employees]
    ([EmployeeCode], [DisplayName], [PreferredName], [WorkEmail], [OrganizationUnitId], [EmploymentStatus],
     [JoinedUtc], [LeftUtc], [SourceSystem], [CreatedBy], [UpdatedBy])
VALUES
    (@EmployeeCode, @DisplayName, @PreferredName, @WorkEmail, @OrganizationUnitId, @EmploymentStatus,
     @JoinedUtc, @LeftUtc, @SourceSystem, @ActorName, @ActorName);
SELECT CAST(SCOPE_IDENTITY() AS int);",
                    TextParameter("@EmployeeCode", normalized.EmployeeCode),
                    TextParameter("@DisplayName", normalized.DisplayName),
                    TextParameter("@PreferredName", normalized.PreferredName),
                    TextParameter("@WorkEmail", normalized.WorkEmail),
                    NullableIntParameter("@OrganizationUnitId", normalized.OrganizationUnitId),
                    TextParameter("@EmploymentStatus", normalized.EmploymentStatus),
                    NullableDateTime2Parameter("@JoinedUtc", normalized.JoinedUtc),
                    NullableDateTime2Parameter("@LeftUtc", normalized.LeftUtc),
                    TextParameter("@SourceSystem", normalized.SourceSystem),
                    TextParameter("@ActorName", normalized.ActorName)).Single();

                return EmployeeDirectoryWriteResult.Success(newId, "Employee saved.");
            }

            if (!normalized.OriginalUpdatedUtc.HasValue)
            {
                return EmployeeDirectoryWriteResult.Failed("The employee was not loaded with an update timestamp.");
            }

            int affectedRows = _context.Database.ExecuteSqlCommand(
                @"
UPDATE [dbo].[PortalBiz_Employees]
SET [EmployeeCode] = @EmployeeCode,
    [DisplayName] = @DisplayName,
    [PreferredName] = @PreferredName,
    [WorkEmail] = @WorkEmail,
    [OrganizationUnitId] = @OrganizationUnitId,
    [EmploymentStatus] = @EmploymentStatus,
    [JoinedUtc] = @JoinedUtc,
    [LeftUtc] = @LeftUtc,
    [SourceSystem] = @SourceSystem,
    [UpdatedUtc] = SYSUTCDATETIME(),
    [UpdatedBy] = @ActorName
WHERE [EmployeeId] = @EmployeeId
  AND [UpdatedUtc] = @OriginalUpdatedUtc;",
                TextParameter("@EmployeeCode", normalized.EmployeeCode),
                TextParameter("@DisplayName", normalized.DisplayName),
                TextParameter("@PreferredName", normalized.PreferredName),
                TextParameter("@WorkEmail", normalized.WorkEmail),
                NullableIntParameter("@OrganizationUnitId", normalized.OrganizationUnitId),
                TextParameter("@EmploymentStatus", normalized.EmploymentStatus),
                NullableDateTime2Parameter("@JoinedUtc", normalized.JoinedUtc),
                NullableDateTime2Parameter("@LeftUtc", normalized.LeftUtc),
                TextParameter("@SourceSystem", normalized.SourceSystem),
                TextParameter("@ActorName", normalized.ActorName),
                IntParameter("@EmployeeId", normalized.EmployeeId),
                DateTime2Parameter("@OriginalUpdatedUtc", normalized.OriginalUpdatedUtc.Value));

            if (affectedRows == 1)
            {
                return EmployeeDirectoryWriteResult.Success(normalized.EmployeeId, "Employee saved.");
            }

            return EmployeeExists(normalized.EmployeeId)
                ? EmployeeDirectoryWriteResult.ConcurrencyConflict("The employee was changed by another request. Reload before saving again.")
                : EmployeeDirectoryWriteResult.Missing("The employee no longer exists.");
        }

        private bool TryValidateOrganizationRequest(OrganizationUnitSaveRequest request, out string message)
        {
            message = string.Empty;
            if (request == null)
            {
                message = "Organization unit data is required.";
                return false;
            }

            if (request.OrganizationUnitId < 0)
            {
                message = "Organization unit id is invalid.";
                return false;
            }

            if (string.IsNullOrEmpty(request.DisplayName) || request.DisplayName.Length > 150)
            {
                message = "Organization display name is required and must not exceed 150 characters.";
                return false;
            }

            if (!string.IsNullOrEmpty(request.OrganizationCode) && request.OrganizationCode.Length > 100)
            {
                message = "Organization code must not exceed 100 characters.";
                return false;
            }

            if (request.ParentOrganizationUnitId.HasValue)
            {
                if (request.ParentOrganizationUnitId.Value <= 0)
                {
                    message = "Parent organization id is invalid.";
                    return false;
                }

                if (request.OrganizationUnitId > 0 && request.ParentOrganizationUnitId.Value == request.OrganizationUnitId)
                {
                    message = "An organization unit cannot be its own parent.";
                    return false;
                }

                if (!OrganizationExists(request.ParentOrganizationUnitId.Value))
                {
                    message = "Parent organization unit does not exist.";
                    return false;
                }

                if (WouldCreateOrganizationCycle(request.OrganizationUnitId, request.ParentOrganizationUnitId.Value))
                {
                    message = "The selected parent would create an organization cycle.";
                    return false;
                }
            }

            if (request.OrganizationUnitId > 0 && !OrganizationExists(request.OrganizationUnitId))
            {
                message = "Organization unit does not exist.";
                return false;
            }

            if (!string.IsNullOrEmpty(request.OrganizationCode) &&
                OrganizationCodeExists(request.OrganizationCode, request.OrganizationUnitId))
            {
                message = "Organization code already exists.";
                return false;
            }

            return true;
        }

        private bool TryValidateEmployeeRequest(EmployeeSaveRequest request, out string message)
        {
            message = string.Empty;
            if (request == null)
            {
                message = "Employee data is required.";
                return false;
            }

            if (request.EmployeeId < 0)
            {
                message = "Employee id is invalid.";
                return false;
            }

            if (!EmployeeCodePattern.IsMatch(request.EmployeeCode ?? string.Empty))
            {
                message = "Employee code must be 2-64 characters and use only letters, digits, dot, underscore, or hyphen.";
                return false;
            }

            if (string.IsNullOrEmpty(request.DisplayName) || request.DisplayName.Length > 150)
            {
                message = "Employee display name is required and must not exceed 150 characters.";
                return false;
            }

            if (request.PreferredName.Length > 100)
            {
                message = "Preferred name must not exceed 100 characters.";
                return false;
            }

            if (request.WorkEmail.Length > 256)
            {
                message = "Work email must not exceed 256 characters.";
                return false;
            }

            if (string.IsNullOrEmpty(request.SourceSystem) || request.SourceSystem.Length > 80)
            {
                message = "Source system is required and must not exceed 80 characters.";
                return false;
            }

            if (!PortalEmployeeStatuses.IsKnown(request.EmploymentStatus))
            {
                message = "Employee status is invalid.";
                return false;
            }

            if (string.Equals(request.EmploymentStatus, PortalEmployeeStatuses.Left, StringComparison.Ordinal) &&
                !request.LeftUtc.HasValue)
            {
                message = "Left UTC is required when status is Left.";
                return false;
            }

            if (!string.Equals(request.EmploymentStatus, PortalEmployeeStatuses.Left, StringComparison.Ordinal) &&
                request.LeftUtc.HasValue)
            {
                message = "Left UTC can be set only when status is Left.";
                return false;
            }

            if (request.OrganizationUnitId.HasValue && !OrganizationExists(request.OrganizationUnitId.Value))
            {
                message = "Organization unit does not exist.";
                return false;
            }

            if (request.EmployeeId > 0 && !EmployeeExists(request.EmployeeId))
            {
                message = "Employee does not exist.";
                return false;
            }

            if (EmployeeCodeExists(request.EmployeeCode, request.EmployeeId))
            {
                message = "Employee code already exists.";
                return false;
            }

            return true;
        }

        private OrganizationUnitSaveRequest NormalizeOrganizationRequest(OrganizationUnitSaveRequest request)
        {
            request = request ?? new OrganizationUnitSaveRequest();
            return new OrganizationUnitSaveRequest
            {
                OrganizationUnitId = request.OrganizationUnitId,
                ParentOrganizationUnitId = request.ParentOrganizationUnitId,
                OrganizationCode = NormalizeOptional(request.OrganizationCode),
                DisplayName = NormalizeRequired(request.DisplayName),
                SortOrder = request.SortOrder,
                IsActive = request.IsActive,
                OriginalUpdatedUtc = request.OriginalUpdatedUtc,
                ActorName = NormalizeActor(request.ActorName)
            };
        }

        private EmployeeSaveRequest NormalizeEmployeeRequest(EmployeeSaveRequest request)
        {
            request = request ?? new EmployeeSaveRequest();
            return new EmployeeSaveRequest
            {
                EmployeeId = request.EmployeeId,
                EmployeeCode = NormalizeRequired(request.EmployeeCode),
                DisplayName = NormalizeRequired(request.DisplayName),
                PreferredName = NormalizeOptional(request.PreferredName),
                WorkEmail = NormalizeOptional(request.WorkEmail),
                OrganizationUnitId = request.OrganizationUnitId,
                EmploymentStatus = NormalizeRequired(request.EmploymentStatus),
                JoinedUtc = request.JoinedUtc,
                LeftUtc = request.LeftUtc,
                SourceSystem = string.IsNullOrWhiteSpace(request.SourceSystem) ? "Portal" : NormalizeRequired(request.SourceSystem),
                OriginalUpdatedUtc = request.OriginalUpdatedUtc,
                ActorName = NormalizeActor(request.ActorName)
            };
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

        private bool OrganizationExists(int organizationUnitId)
        {
            return ScalarInt(
                "SELECT COUNT(*) FROM [dbo].[PortalBiz_OrganizationUnits] WHERE [OrganizationUnitId] = @OrganizationUnitId;",
                IntParameter("@OrganizationUnitId", organizationUnitId)) > 0;
        }

        private bool EmployeeExists(int employeeId)
        {
            return ScalarInt(
                "SELECT COUNT(*) FROM [dbo].[PortalBiz_Employees] WHERE [EmployeeId] = @EmployeeId;",
                IntParameter("@EmployeeId", employeeId)) > 0;
        }

        private bool OrganizationCodeExists(string organizationCode, int excludingOrganizationUnitId)
        {
            if (string.IsNullOrEmpty(organizationCode))
            {
                return false;
            }

            return ScalarInt(
                @"
SELECT COUNT(*)
FROM [dbo].[PortalBiz_OrganizationUnits]
WHERE [OrganizationCode] = @OrganizationCode
  AND [OrganizationUnitId] <> @OrganizationUnitId;",
                TextParameter("@OrganizationCode", organizationCode),
                IntParameter("@OrganizationUnitId", Math.Max(0, excludingOrganizationUnitId))) > 0;
        }

        private bool EmployeeCodeExists(string employeeCode, int excludingEmployeeId)
        {
            return ScalarInt(
                @"
SELECT COUNT(*)
FROM [dbo].[PortalBiz_Employees]
WHERE [EmployeeCode] = @EmployeeCode
  AND [EmployeeId] <> @EmployeeId;",
                TextParameter("@EmployeeCode", employeeCode),
                IntParameter("@EmployeeId", Math.Max(0, excludingEmployeeId))) > 0;
        }

        private bool WouldCreateOrganizationCycle(int organizationUnitId, int parentOrganizationUnitId)
        {
            if (organizationUnitId <= 0)
            {
                return false;
            }

            int? currentId = parentOrganizationUnitId;
            var visited = new HashSet<int>();
            while (currentId.HasValue)
            {
                if (currentId.Value == organizationUnitId || !visited.Add(currentId.Value))
                {
                    return true;
                }

                currentId = GetParentOrganizationUnitId(currentId.Value);
            }

            return false;
        }

        private int? GetParentOrganizationUnitId(int organizationUnitId)
        {
            return _context.Database.SqlQuery<int?>(
                @"
SELECT [ParentOrganizationUnitId]
FROM [dbo].[PortalBiz_OrganizationUnits]
WHERE [OrganizationUnitId] = @OrganizationUnitId;",
                IntParameter("@OrganizationUnitId", organizationUnitId)).SingleOrDefault();
        }

        private int ScalarInt(string sql, params object[] parameters)
        {
            return _context.Database.SqlQuery<int>(sql, parameters).Single();
        }

        private static IOrganizationUnitInfo CreateOrganizationInfo(OrganizationUnitProjection row)
        {
            return new OrganizationUnitInfo(
                row.OrganizationUnitId,
                row.ParentOrganizationUnitId,
                row.OrganizationCode,
                row.DisplayName,
                row.SortOrder,
                row.IsActive,
                row.CreatedUtc,
                row.UpdatedUtc);
        }

        private static IEmployeeInfo CreateEmployeeInfo(EmployeeProjection row)
        {
            return new EmployeeInfo(
                row.EmployeeId,
                row.EmployeeCode,
                row.DisplayName,
                row.PreferredName,
                row.WorkEmail,
                row.OrganizationUnitId,
                row.OrganizationDisplayName,
                row.EmploymentStatus,
                row.JoinedUtc,
                row.LeftUtc,
                row.SourceSystem,
                row.UpdatedUtc);
        }

        private static string NormalizeRequired(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string NormalizeOptional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
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

        private static SqlParameter NullableIntParameter(string name, int? value)
        {
            return new SqlParameter(name, SqlDbType.Int) { Value = value.HasValue ? (object)value.Value : DBNull.Value };
        }

        private static SqlParameter BoolParameter(string name, bool value)
        {
            return new SqlParameter(name, SqlDbType.Bit) { Value = value };
        }

        private static SqlParameter TextParameter(string name, string value)
        {
            return new SqlParameter(name, SqlDbType.NVarChar) { Value = string.IsNullOrEmpty(value) ? (object)DBNull.Value : value };
        }

        private static SqlParameter DateTime2Parameter(string name, DateTime value)
        {
            return new SqlParameter(name, SqlDbType.DateTime2) { Value = value };
        }

        private static SqlParameter NullableDateTime2Parameter(string name, DateTime? value)
        {
            return new SqlParameter(name, SqlDbType.DateTime2) { Value = value.HasValue ? (object)value.Value : DBNull.Value };
        }

        private sealed class OrganizationUnitProjection
        {
            public int OrganizationUnitId { get; set; }
            public int? ParentOrganizationUnitId { get; set; }
            public string OrganizationCode { get; set; }
            public string DisplayName { get; set; }
            public int SortOrder { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedUtc { get; set; }
            public DateTime UpdatedUtc { get; set; }
        }

        private sealed class EmployeeProjection
        {
            public int EmployeeId { get; set; }
            public string EmployeeCode { get; set; }
            public string DisplayName { get; set; }
            public string PreferredName { get; set; }
            public string WorkEmail { get; set; }
            public int? OrganizationUnitId { get; set; }
            public string OrganizationDisplayName { get; set; }
            public string EmploymentStatus { get; set; }
            public DateTime? JoinedUtc { get; set; }
            public DateTime? LeftUtc { get; set; }
            public string SourceSystem { get; set; }
            public DateTime UpdatedUtc { get; set; }
        }
    }
}
