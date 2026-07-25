using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>基于 <see cref="PortalBizDbContext"/> 的员工组织目录只读数据访问实现。</zh-CN>
    ///   <en>Read-only employee and organization directory data-access implementation backed by <see cref="PortalBizDbContext"/>.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本实现只查询 P6.3 表，不写审计、不递增安全版本、不启用员工工号登录。缺少 P6.3 表时返回空集合或空值。</zh-CN>
    ///   <en>This implementation only reads P6.3 tables. It does not write audits, increment security versions, or enable employee-code sign-in. Missing P6.3 tables result in empty collections or null values.</en>
    /// </lang>
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
        /// <lang>
        ///   <zh-CN>初始化员工组织目录只读数据访问实现。</zh-CN>
        ///   <en>Initializes the employee and organization directory read-only data-access implementation.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>企业业务基础数据上下文。</zh-CN>
        ///   <en>Enterprise business foundation data context.</en>
        /// </l>
        /// </param>
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
                // <lang>
                //   <zh-CN>组织目录只暴露轻量层级字段；分页在 SQL 侧完成，避免后台页面一次性拉取完整组织树。</zh-CN>
                //   <en>The organization directory exposes only lightweight hierarchy fields; paging happens in SQL so administration pages do not pull the full organization tree at once.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>员工列表联接组织名称用于后台浏览和绑定辅助，不在此处计算权限或员工账号状态。</zh-CN>
                //   <en>The employee list joins organization names for administration browsing and binding assistance; it does not calculate authorization or employee-account state here.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>绑定列表联接用户表和员工表，作为后台核对视图；写入、结束绑定和审计由后台写入契约负责。</zh-CN>
                //   <en>The binding list joins user and employee tables as an administration review view; writes, binding termination, and audit are handled by the administration write contract.</en>
                // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查指定表是否存在并可通过当前连接访问。</zh-CN>
        ///   <en>Checks whether the specified table exists and is accessible through the current connection.</en>
        /// </lang>
        /// </summary>
        /// <param name="tableName">
        /// <l>
        ///   <zh-CN>不带架构前缀的表名。</zh-CN>
        ///   <en>Table name without schema prefix.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>表存在且检查成功时为 <c>true</c>；异常按不可用处理。</zh-CN>
        ///   <en><c>true</c> when the table exists and the check succeeds; exceptions are treated as unavailable.</en>
        /// </l>
        /// </returns>
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
        ///   <zh-CN>归一化员工目录查询条件，并套用分页上限。</zh-CN>
        ///   <en>Normalizes employee-directory query options and applies paging limits.</en>
        /// </lang>
        /// </summary>
        /// <param name="query">
        /// <l>
        ///   <zh-CN>调用方提供的查询条件；为空时使用默认条件。</zh-CN>
        ///   <en>Caller-provided query options; defaults are used when null.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>可安全传入 SQL 查询的归一化条件。</zh-CN>
        ///   <en>Normalized options safe to pass into SQL queries.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>把可选文本裁剪为空串或去除首尾空白后的值。</zh-CN>
        ///   <en>Normalizes optional text to an empty string or a trimmed value.</en>
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
        ///   <zh-CN>归一化后的文本。</zh-CN>
        ///   <en>Normalized text.</en>
        /// </l>
        /// </returns>
        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把关键字转换为 SQL Server LIKE 模式，并转义通配符字符。</zh-CN>
        ///   <en>Converts a keyword into a SQL Server LIKE pattern while escaping wildcard characters.</en>
        /// </lang>
        /// </summary>
        /// <param name="keyword">
        /// <l>
        ///   <zh-CN>已归一化关键字。</zh-CN>
        ///   <en>Normalized keyword.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>LIKE 模式；空关键字返回空串。</zh-CN>
        ///   <en>LIKE pattern, or an empty string for empty keywords.</en>
        /// </l>
        /// </returns>
        private static string ToLikePattern(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return string.Empty;
            }

            return "%" + keyword.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>生成当前有效绑定查询 SQL。</zh-CN>
        ///   <en>Builds SQL for querying the current active binding.</en>
        /// </lang>
        /// </summary>
        /// <param name="predicate">
        /// <l>
        ///   <zh-CN>已由调用方控制的附加谓词片段。</zh-CN>
        ///   <en>Additional predicate fragment controlled by the caller.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>可交给 EF SQL 查询执行的 SQL 文本。</zh-CN>
        ///   <en>SQL text suitable for execution through EF SQL query APIs.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>把 SQL 投影转换为跨层绑定信息契约。</zh-CN>
        ///   <en>Converts a SQL projection into the cross-layer binding-info contract.</en>
        /// </lang>
        /// </summary>
        /// <param name="row">
        /// <l>
        ///   <zh-CN>SQL 查询结果行。</zh-CN>
        ///   <en>SQL query result row.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>用户员工绑定只读信息。</zh-CN>
        ///   <en>Read-only user-employee binding information.</en>
        /// </l>
        /// </returns>
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
        ///   <zh-CN>组织单元 SQL 查询投影。</zh-CN>
        ///   <en>SQL query projection for an organization unit.</en>
        /// </lang>
        /// </summary>
        private sealed class OrganizationUnitProjection
        {
            /// <summary>
            /// <lang>
            ///   <zh-CN>组织单元标识。</zh-CN>
            ///   <en>Organization-unit identifier.</en>
            /// </lang>
            /// </summary>
            public int OrganizationUnitId { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>父级组织单元标识。</zh-CN>
            ///   <en>Parent organization-unit identifier.</en>
            /// </lang>
            /// </summary>
            public int? ParentOrganizationUnitId { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>组织编码。</zh-CN>
            ///   <en>Organization code.</en>
            /// </lang>
            /// </summary>
            public string OrganizationCode { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>组织显示名。</zh-CN>
            ///   <en>Organization display name.</en>
            /// </lang>
            /// </summary>
            public string DisplayName { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>同级显示顺序。</zh-CN>
            ///   <en>Sibling display order.</en>
            /// </lang>
            /// </summary>
            public int SortOrder { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>组织是否启用。</zh-CN>
            ///   <en>Whether the organization unit is active.</en>
            /// </lang>
            /// </summary>
            public bool IsActive { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>创建 UTC 时间。</zh-CN>
            ///   <en>Creation UTC time.</en>
            /// </lang>
            /// </summary>
            public DateTime CreatedUtc { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>最近更新 UTC 时间。</zh-CN>
            ///   <en>Latest update UTC time.</en>
            /// </lang>
            /// </summary>
            public DateTime UpdatedUtc { get; set; }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工 SQL 查询投影。</zh-CN>
        ///   <en>SQL query projection for an employee.</en>
        /// </lang>
        /// </summary>
        private sealed class EmployeeProjection
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
            ///   <zh-CN>偏好称呼。</zh-CN>
            ///   <en>Preferred name.</en>
            /// </lang>
            /// </summary>
            public string PreferredName { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>工作邮箱。</zh-CN>
            ///   <en>Work email.</en>
            /// </lang>
            /// </summary>
            public string WorkEmail { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>所属组织单元标识。</zh-CN>
            ///   <en>Owning organization-unit identifier.</en>
            /// </lang>
            /// </summary>
            public int? OrganizationUnitId { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>所属组织显示名。</zh-CN>
            ///   <en>Owning organization display name.</en>
            /// </lang>
            /// </summary>
            public string OrganizationDisplayName { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>员工状态。</zh-CN>
            ///   <en>Employee status.</en>
            /// </lang>
            /// </summary>
            public string EmploymentStatus { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>入职 UTC 时间。</zh-CN>
            ///   <en>Join UTC time.</en>
            /// </lang>
            /// </summary>
            public DateTime? JoinedUtc { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>离职 UTC 时间。</zh-CN>
            ///   <en>Leave UTC time.</en>
            /// </lang>
            /// </summary>
            public DateTime? LeftUtc { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>来源系统标识。</zh-CN>
            ///   <en>Source-system identifier.</en>
            /// </lang>
            /// </summary>
            public string SourceSystem { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>最近更新 UTC 时间。</zh-CN>
            ///   <en>Latest update UTC time.</en>
            /// </lang>
            /// </summary>
            public DateTime UpdatedUtc { get; set; }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>门户用户与员工绑定 SQL 查询投影。</zh-CN>
        ///   <en>SQL query projection for a Portal-user to employee binding.</en>
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
