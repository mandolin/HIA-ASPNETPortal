using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：基于 <see cref="PortalBizDbContext"/> 的员工组织目录只读数据访问实现。
    ///
    /// English: Read-only employee and organization directory data-access implementation backed by <see cref="PortalBizDbContext"/>.
    /// </summary>
    /// <remarks>
    /// 中文：本实现只查询 P6.3 表，不写审计、不递增安全版本、不启用员工工号登录。缺少 P6.3 表时返回空集合或空值。
    ///
    /// English: This implementation only reads P6.3 tables. It does not write audits, increment security versions,
    /// or enable employee-code sign-in. Missing P6.3 tables result in empty collections or null values.
    /// </remarks>
    public class EmployeeDirectoryDb : IEmployeeDirectoryDb
    {
        private const int DefaultPageSize = 100;
        private const int MaxPageSize = 500;
        private const string OrganizationTableName = "PortalBiz_OrganizationUnits";
        private const string EmployeeTableName = "PortalBiz_Employees";
        private const string BindingTableName = "PortalBiz_UserEmployeeBindings";
        private readonly PortalBizDbContext _context;

        /// <summary>
        /// 中文：初始化员工组织目录只读数据访问实现。
        ///
        /// English: Initializes the employee and organization directory read-only data-access implementation.
        /// </summary>
        /// <param name="context">中文：企业业务基础数据上下文。English: Enterprise business foundation data context.</param>
        public EmployeeDirectoryDb(PortalBizDbContext context)
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
        public IEnumerable<IOrganizationUnitInfo> GetOrganizationUnits(EmployeeDirectoryQuery query)
        {
            if (!IsSchemaAvailable())
            {
                return Enumerable.Empty<IOrganizationUnitInfo>();
            }

            EmployeeDirectoryQuery normalizedQuery = NormalizeQuery(query);
            string keyword = Normalize(normalizedQuery.Keyword);
            string keywordPattern = ToLikePattern(keyword);

            try
            {
                var rows = _context.Database.SqlQuery<OrganizationUnitProjection>(
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
WHERE (@p0 = N'' OR [OrganizationCode] LIKE @p1 OR [DisplayName] LIKE @p1)
    AND (@p2 = 1 OR [IsActive] = 1)
ORDER BY [ParentOrganizationUnitId], [SortOrder], [DisplayName], [OrganizationUnitId]
OFFSET @p3 ROWS FETCH NEXT @p4 ROWS ONLY;",
                    keyword,
                    keywordPattern,
                    normalizedQuery.IncludeInactiveOrganizations,
                    normalizedQuery.Skip,
                    normalizedQuery.Take).ToList();

                return rows.Select(row => new OrganizationUnitInfo(
                    row.OrganizationUnitId,
                    row.ParentOrganizationUnitId,
                    row.OrganizationCode,
                    row.DisplayName,
                    row.SortOrder,
                    row.IsActive,
                    row.CreatedUtc,
                    row.UpdatedUtc)).ToList();
            }
            catch (Exception)
            {
                return Enumerable.Empty<IOrganizationUnitInfo>();
            }
        }

        /// <inheritdoc />
        public IEnumerable<IEmployeeInfo> GetEmployees(EmployeeDirectoryQuery query)
        {
            if (!IsSchemaAvailable())
            {
                return Enumerable.Empty<IEmployeeInfo>();
            }

            EmployeeDirectoryQuery normalizedQuery = NormalizeQuery(query);
            string keyword = Normalize(normalizedQuery.Keyword);
            string status = Normalize(normalizedQuery.Status);
            if (!string.IsNullOrEmpty(status) && !PortalEmployeeStatuses.IsKnown(status))
            {
                return Enumerable.Empty<IEmployeeInfo>();
            }

            string keywordPattern = ToLikePattern(keyword);

            try
            {
                var rows = _context.Database.SqlQuery<EmployeeProjection>(
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
WHERE (@p0 = N'' OR [Employee].[EmployeeCode] LIKE @p1 OR [Employee].[DisplayName] LIKE @p1
    OR [Employee].[PreferredName] LIKE @p1 OR [Employee].[WorkEmail] LIKE @p1
    OR [Organization].[OrganizationCode] LIKE @p1 OR [Organization].[DisplayName] LIKE @p1)
    AND (@p2 = N'' OR [Employee].[EmploymentStatus] = @p2)
ORDER BY [Employee].[EmployeeCode], [Employee].[EmployeeId]
OFFSET @p3 ROWS FETCH NEXT @p4 ROWS ONLY;",
                    keyword,
                    keywordPattern,
                    status,
                    normalizedQuery.Skip,
                    normalizedQuery.Take).ToList();

                return rows.Select(row => new EmployeeInfo(
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
                    row.UpdatedUtc)).ToList();
            }
            catch (Exception)
            {
                return Enumerable.Empty<IEmployeeInfo>();
            }
        }

        /// <inheritdoc />
        public IEnumerable<IUserEmployeeBindingInfo> GetUserEmployeeBindings(EmployeeDirectoryQuery query)
        {
            if (!IsSchemaAvailable())
            {
                return Enumerable.Empty<IUserEmployeeBindingInfo>();
            }

            EmployeeDirectoryQuery normalizedQuery = NormalizeQuery(query);
            string keyword = Normalize(normalizedQuery.Keyword);
            string status = Normalize(normalizedQuery.Status);
            if (!string.IsNullOrEmpty(status) && !PortalUserEmployeeBindingStatuses.IsKnown(status))
            {
                return Enumerable.Empty<IUserEmployeeBindingInfo>();
            }

            string keywordPattern = ToLikePattern(keyword);

            try
            {
                var rows = _context.Database.SqlQuery<UserEmployeeBindingProjection>(
                    @"
SELECT
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
WHERE (@p0 = N'' OR [User].[Name] LIKE @p1 OR [Employee].[EmployeeCode] LIKE @p1
    OR [Employee].[DisplayName] LIKE @p1)
    AND (@p2 = N'' OR [Binding].[BindingStatus] = @p2)
ORDER BY [Binding].[BoundUtc] DESC, [Binding].[BindingId] DESC
OFFSET @p3 ROWS FETCH NEXT @p4 ROWS ONLY;",
                    keyword,
                    keywordPattern,
                    status,
                    normalizedQuery.Skip,
                    normalizedQuery.Take).ToList();

                return rows.Select(CreateBindingInfo).ToList();
            }
            catch (Exception)
            {
                return Enumerable.Empty<IUserEmployeeBindingInfo>();
            }
        }

        /// <inheritdoc />
        public IUserEmployeeBindingInfo GetActiveBindingByUserId(int userId)
        {
            if (userId <= 0 || !IsSchemaAvailable())
            {
                return null;
            }

            try
            {
                var row = _context.Database.SqlQuery<UserEmployeeBindingProjection>(
                    GetActiveBindingSql("[Binding].[UserId] = @p0"),
                    userId).SingleOrDefault();
                return row == null ? null : CreateBindingInfo(row);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <inheritdoc />
        public IUserEmployeeBindingInfo GetActiveBindingByEmployeeCode(string employeeCode)
        {
            string normalizedEmployeeCode = Normalize(employeeCode);
            if (string.IsNullOrEmpty(normalizedEmployeeCode) || !IsSchemaAvailable())
            {
                return null;
            }

            try
            {
                var row = _context.Database.SqlQuery<UserEmployeeBindingProjection>(
                    GetActiveBindingSql("[Employee].[EmployeeCode] = @p0"),
                    normalizedEmployeeCode).SingleOrDefault();
                return row == null ? null : CreateBindingInfo(row);
            }
            catch (Exception)
            {
                return null;
            }
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

        private static EmployeeDirectoryQuery NormalizeQuery(EmployeeDirectoryQuery query)
        {
            var normalized = query ?? new EmployeeDirectoryQuery();
            int take = normalized.Take <= 0 ? DefaultPageSize : normalized.Take;
            if (take > MaxPageSize)
            {
                take = MaxPageSize;
            }

            return new EmployeeDirectoryQuery
            {
                Keyword = Normalize(normalized.Keyword),
                Status = Normalize(normalized.Status),
                Skip = normalized.Skip < 0 ? 0 : normalized.Skip,
                Take = take,
                IncludeInactiveOrganizations = normalized.IncludeInactiveOrganizations
            };
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string ToLikePattern(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return string.Empty;
            }

            return "%" + keyword.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
        }

        private static string GetActiveBindingSql(string predicate)
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
WHERE [Binding].[BindingStatus] = N'Active'
    AND " + predicate + @"
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
