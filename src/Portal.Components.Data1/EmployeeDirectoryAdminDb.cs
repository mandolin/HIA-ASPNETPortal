using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>基于 P6.3 表的员工和组织后台最小维护实现。</zh-CN>
    ///   <en>Minimal administration maintenance implementation for employees and organization units backed by P6.3 tables.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本实现只负责组织和员工主数据写入。运营审计由页面在成功后写入；账号员工绑定、安全版本递增和员工工号登录留给 P6.3-S5。</zh-CN>
    ///   <en>This implementation writes only organization and employee master data. Pages write operations audit after success; user-employee binding, security-version increments, and employee-code sign-in remain in P6.3-S5.</en>
    /// </lang>
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
        /// <lang>
        ///   <zh-CN>初始化员工和组织后台维护实现。</zh-CN>
        ///   <en>Initializes the employee and organization administration maintenance implementation.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>企业业务基础数据上下文。</zh-CN>
        ///   <en>Enterprise business foundation data context.</en>
        /// </l>
        /// </param>
        public EmployeeDirectoryAdminDb(PortalBizDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查 P6.3 员工组织目录表是否可用于维护。</zh-CN>
        ///   <en>Checks whether the P6.3 employee-directory tables are available for maintenance.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>依赖表存在且可访问时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the required tables exist and can be accessed.</en>
        /// </l>
        /// </returns>
        public bool IsSchemaAvailable()
        {
            return HasTable(OrganizationTableName) &&
                   HasTable(EmployeeTableName) &&
                   HasTable(BindingTableName);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按标识读取一个组织单元。</zh-CN>
        ///   <en>Reads one organization unit by id.</en>
        /// </lang>
        /// </summary>
        /// <param name="organizationUnitId">
        /// <l>
        ///   <zh-CN>组织单元标识。</zh-CN>
        ///   <en>Organization-unit identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>组织单元只读信息；不存在时为空。</zh-CN>
        ///   <en>Read-only organization-unit information, or null when it does not exist.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>按标识读取一个员工。</zh-CN>
        ///   <en>Reads one employee by id.</en>
        /// </lang>
        /// </summary>
        /// <param name="employeeId">
        /// <l>
        ///   <zh-CN>员工标识。</zh-CN>
        ///   <en>Employee identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>员工只读信息；不存在时为空。</zh-CN>
        ///   <en>Read-only employee information, or null when it does not exist.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>新增或更新组织单元。</zh-CN>
        ///   <en>Creates or updates an organization unit.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>组织单元保存请求，包含父级、编码、名称、排序和启用状态。</zh-CN>
        ///   <en>Organization-unit save request containing parent, code, name, sort order, and enabled state.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>写入结果，包含成功状态、目标标识和可显示错误。</zh-CN>
        ///   <en>Write result containing success state, target identifier, and displayable errors.</en>
        /// </l>
        /// </returns>
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

            // <lang>
            //   <zh-CN>新增组织不依赖 EF change tracking，直接执行参数化 SQL 并返回数据库生成的主键，便于与旧存储过程风格保持一致。</zh-CN>
            //   <en>New organization units bypass EF change tracking and execute parameterized SQL directly, returning the database-generated key to stay aligned with the legacy stored-procedure style.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>更新组织时必须带上加载时的 UTC 更新时间；它是当前最小并发令牌，避免覆盖其他管理员刚保存的变更。</zh-CN>
            //   <en>Organization updates must include the UTC update timestamp read during load; it is the current minimal concurrency token and prevents overwriting another administrator's recent save.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>零行受影响不一定代表记录缺失，也可能是并发版本已变化；这里再读一次存在性，以便给页面更准确的提示。</zh-CN>
            //   <en>Zero affected rows can mean either a missing row or a changed concurrency token; this rechecks existence so the page can show a more precise message.</en>
            // </lang>
            return OrganizationExists(normalized.OrganizationUnitId)
                ? EmployeeDirectoryWriteResult.ConcurrencyConflict("The organization unit was changed by another request. Reload before saving again.")
                : EmployeeDirectoryWriteResult.Missing("The organization unit no longer exists.");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>新增或更新员工主数据。</zh-CN>
        ///   <en>Creates or updates employee master data.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>员工保存请求，包含员工号、姓名、邮箱、组织、状态和更新并发信息。</zh-CN>
        ///   <en>Employee save request containing employee code, names, email, organization, status, and update concurrency data.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>写入结果，包含成功状态、目标标识和可显示错误。</zh-CN>
        ///   <en>Write result containing success state, target identifier, and displayable errors.</en>
        /// </l>
        /// </returns>
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

            // <lang>
            //   <zh-CN>新增员工同样使用参数化 SQL 写入最小主数据字段；高敏个人信息不在当前表和请求对象中扩展。</zh-CN>
            //   <en>New employees are also inserted through parameterized SQL with the minimal master-data fields; highly sensitive personal data is not expanded into the current table or request object.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>员工更新使用加载时的 UTC 更新时间作为并发条件；这比静默覆盖更适合后台主数据维护。</zh-CN>
            //   <en>Employee updates use the UTC update timestamp from load time as the concurrency condition, which is safer for administration master-data maintenance than silent overwrite.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>与组织更新相同，零行受影响后再次判断记录是否仍存在，从而区分并发冲突和已删除。</zh-CN>
            //   <en>As with organization updates, zero affected rows triggers an existence check so concurrency conflicts and deleted rows can be distinguished.</en>
            // </lang>
            return EmployeeExists(normalized.EmployeeId)
                ? EmployeeDirectoryWriteResult.ConcurrencyConflict("The employee was changed by another request. Reload before saving again.")
                : EmployeeDirectoryWriteResult.Missing("The employee no longer exists.");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>校验组织保存请求的层级、名称、编码和并发目标。</zh-CN>
        ///   <en>Validates hierarchy, names, codes, and concurrency target for an organization save request.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>已经归一化的组织保存请求。</zh-CN>
        ///   <en>Normalized organization save request.</en>
        /// </l>
        /// </param>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>校验失败时返回给后台页面的非敏感提示。</zh-CN>
        ///   <en>Non-sensitive message returned to the administration page when validation fails.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>请求可写入当前最小主数据表时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the request can be written to the current minimal master-data table.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>校验员工保存请求的工号、显示名、状态、组织引用和唯一性。</zh-CN>
        ///   <en>Validates employee code, display name, status, organization reference, and uniqueness for an employee save request.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>已经归一化的员工保存请求。</zh-CN>
        ///   <en>Normalized employee save request.</en>
        /// </l>
        /// </param>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>校验失败时返回给后台页面的非敏感提示。</zh-CN>
        ///   <en>Non-sensitive message returned to the administration page when validation fails.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>请求可写入当前最小员工主数据表时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the request can be written to the current minimal employee master-data table.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>生成组织保存用的规整请求，统一处理空白字符串和默认操作者。</zh-CN>
        ///   <en>Creates a normalized organization save request, centralizing blank-string handling and default actor selection.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>页面传入的原始组织保存请求。</zh-CN>
        ///   <en>Raw organization save request passed by the page.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>可用于校验和 SQL 参数构造的规整请求。</zh-CN>
        ///   <en>Normalized request suitable for validation and SQL parameter construction.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>生成员工保存用的规整请求，并把缺省来源系统落到 Portal。</zh-CN>
        ///   <en>Creates a normalized employee save request and defaults the source system to Portal when omitted.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>页面传入的原始员工保存请求。</zh-CN>
        ///   <en>Raw employee save request passed by the page.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>可用于校验和 SQL 参数构造的规整请求。</zh-CN>
        ///   <en>Normalized request suitable for validation and SQL parameter construction.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>检测 P6.3 最小业务表是否存在；异常按不可用处理，不向页面泄露底层错误。</zh-CN>
        ///   <en>Checks whether a P6.3 minimal business table exists; exceptions are treated as unavailable without leaking lower-level errors to the page.</en>
        /// </lang>
        /// </summary>
        /// <param name="tableName">
        /// <l>
        ///   <zh-CN>受信任的内部表名常量。</zh-CN>
        ///   <en>Trusted internal table-name constant.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>表存在且可查询时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the table exists and can be queried.</en>
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
        ///   <zh-CN>判断组织单元记录是否存在。</zh-CN>
        ///   <en>Determines whether an organization-unit row exists.</en>
        /// </lang>
        /// </summary>
        /// <param name="organizationUnitId">
        /// <l>
        ///   <zh-CN>组织单元标识。</zh-CN>
        ///   <en>Organization-unit identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>找到记录时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when a row is found.</en>
        /// </l>
        /// </returns>
        private bool OrganizationExists(int organizationUnitId)
        {
            return ScalarInt(
                "SELECT COUNT(*) FROM [dbo].[PortalBiz_OrganizationUnits] WHERE [OrganizationUnitId] = @OrganizationUnitId;",
                IntParameter("@OrganizationUnitId", organizationUnitId)) > 0;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断员工记录是否存在。</zh-CN>
        ///   <en>Determines whether an employee row exists.</en>
        /// </lang>
        /// </summary>
        /// <param name="employeeId">
        /// <l>
        ///   <zh-CN>员工标识。</zh-CN>
        ///   <en>Employee identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>找到记录时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when a row is found.</en>
        /// </l>
        /// </returns>
        private bool EmployeeExists(int employeeId)
        {
            return ScalarInt(
                "SELECT COUNT(*) FROM [dbo].[PortalBiz_Employees] WHERE [EmployeeId] = @EmployeeId;",
                IntParameter("@EmployeeId", employeeId)) > 0;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查组织编码是否已被其他组织单元使用。</zh-CN>
        ///   <en>Checks whether an organization code is already used by another organization unit.</en>
        /// </lang>
        /// </summary>
        /// <param name="organizationCode">
        /// <l>
        ///   <zh-CN>待校验组织编码。</zh-CN>
        ///   <en>Organization code to check.</en>
        /// </l>
        /// </param>
        /// <param name="excludingOrganizationUnitId">
        /// <l>
        ///   <zh-CN>更新当前记录时要排除的组织单元标识。</zh-CN>
        ///   <en>Organization-unit id to exclude while updating the current row.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>其他记录已使用该编码时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when another row already uses the code.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查员工工号是否已被其他员工使用。</zh-CN>
        ///   <en>Checks whether an employee code is already used by another employee.</en>
        /// </lang>
        /// </summary>
        /// <param name="employeeCode">
        /// <l>
        ///   <zh-CN>待校验员工工号。</zh-CN>
        ///   <en>Employee code to check.</en>
        /// </l>
        /// </param>
        /// <param name="excludingEmployeeId">
        /// <l>
        ///   <zh-CN>更新当前记录时要排除的员工标识。</zh-CN>
        ///   <en>Employee id to exclude while updating the current row.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>其他记录已使用该工号时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when another row already uses the employee code.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查把组织挂到指定父级后是否会形成环。</zh-CN>
        ///   <en>Checks whether assigning the proposed parent would create an organization cycle.</en>
        /// </lang>
        /// </summary>
        /// <param name="organizationUnitId">
        /// <l>
        ///   <zh-CN>正在保存的组织单元标识；新增组织没有历史节点，因此不会形成环。</zh-CN>
        ///   <en>Organization-unit id being saved; new rows have no historical node and therefore cannot create a cycle.</en>
        /// </l>
        /// </param>
        /// <param name="parentOrganizationUnitId">
        /// <l>
        ///   <zh-CN>候选父级组织单元标识。</zh-CN>
        ///   <en>Candidate parent organization-unit id.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>发现自引用、祖先回指或异常重复链路时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when self-reference, ancestor loopback, or repeated chain traversal is detected.</en>
        /// </l>
        /// </returns>
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
                // <lang>
                //   <zh-CN>沿父链向上查找，既检测目标节点本身，也防止异常数据形成无限循环。</zh-CN>
                //   <en>Walks upward through the parent chain, checking both the target node and abnormal existing loops to avoid infinite traversal.</en>
                // </lang>
                if (currentId.Value == organizationUnitId || !visited.Add(currentId.Value))
                {
                    return true;
                }

                currentId = GetParentOrganizationUnitId(currentId.Value);
            }

            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定组织单元的父级标识。</zh-CN>
        ///   <en>Reads the parent id for an organization unit.</en>
        /// </lang>
        /// </summary>
        /// <param name="organizationUnitId">
        /// <l>
        ///   <zh-CN>组织单元标识。</zh-CN>
        ///   <en>Organization-unit identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>父级组织单元标识；无父级或记录缺失时为空。</zh-CN>
        ///   <en>Parent organization-unit id; null when there is no parent or the row is missing.</en>
        /// </l>
        /// </returns>
        private int? GetParentOrganizationUnitId(int organizationUnitId)
        {
            return _context.Database.SqlQuery<int?>(
                @"
SELECT [ParentOrganizationUnitId]
FROM [dbo].[PortalBiz_OrganizationUnits]
WHERE [OrganizationUnitId] = @OrganizationUnitId;",
                IntParameter("@OrganizationUnitId", organizationUnitId)).SingleOrDefault();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>执行返回单个整数的参数化 SQL 查询。</zh-CN>
        ///   <en>Executes a parameterized SQL query that returns one integer.</en>
        /// </lang>
        /// </summary>
        /// <param name="sql">
        /// <l>
        ///   <zh-CN>内部固定 SQL 文本。</zh-CN>
        ///   <en>Internal fixed SQL text.</en>
        /// </l>
        /// </param>
        /// <param name="parameters">
        /// <l>
        ///   <zh-CN>SQL 参数集合。</zh-CN>
        ///   <en>SQL parameter collection.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>查询得到的整数值。</zh-CN>
        ///   <en>Integer value returned by the query.</en>
        /// </l>
        /// </returns>
        private int ScalarInt(string sql, params object[] parameters)
        {
            return _context.Database.SqlQuery<int>(sql, parameters).Single();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把组织 SQL 投影转换为跨层只读契约。</zh-CN>
        ///   <en>Converts an organization SQL projection to the cross-layer read-only contract.</en>
        /// </lang>
        /// </summary>
        /// <param name="row">
        /// <l>
        ///   <zh-CN>数据库查询投影。</zh-CN>
        ///   <en>Database query projection.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>组织只读信息对象。</zh-CN>
        ///   <en>Organization read-only information object.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>把员工 SQL 投影转换为跨层只读契约。</zh-CN>
        ///   <en>Converts an employee SQL projection to the cross-layer read-only contract.</en>
        /// </lang>
        /// </summary>
        /// <param name="row">
        /// <l>
        ///   <zh-CN>数据库查询投影。</zh-CN>
        ///   <en>Database query projection.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>员工只读信息对象。</zh-CN>
        ///   <en>Employee read-only information object.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>规整必填文本；空白输入归一为空字符串，便于统一校验。</zh-CN>
        ///   <en>Normalizes required text; blank input becomes an empty string so validation is centralized.</en>
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
        ///   <zh-CN>去除首尾空白后的文本，或空字符串。</zh-CN>
        ///   <en>Trimmed text, or an empty string.</en>
        /// </l>
        /// </returns>
        private static string NormalizeRequired(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>规整可选文本；当前存储层用空字符串表示未填写。</zh-CN>
        ///   <en>Normalizes optional text; the current storage layer represents omitted values as empty strings.</en>
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
        ///   <zh-CN>去除首尾空白后的文本，或空字符串。</zh-CN>
        ///   <en>Trimmed text, or an empty string.</en>
        /// </l>
        /// </returns>
        private static string NormalizeOptional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>规整审计操作者标识，并提供旧后台兼容默认值。</zh-CN>
        ///   <en>Normalizes the audit actor identifier and supplies a legacy-administration compatible default.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>页面传入的操作者标识。</zh-CN>
        ///   <en>Actor identifier passed by the page.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>不超过 100 个字符的操作者标识。</zh-CN>
        ///   <en>Actor identifier capped at 100 characters.</en>
        /// </l>
        /// </returns>
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
        ///   <zh-CN>创建 SQL Server <c>int</c> 参数。</zh-CN>
        ///   <en>Creates a SQL Server <c>int</c> parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>参数名。</zh-CN>
        ///   <en>Parameter name.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>参数值。</zh-CN>
        ///   <en>Parameter value.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>已指定类型和值的参数。</zh-CN>
        ///   <en>Parameter with type and value assigned.</en>
        /// </l>
        /// </returns>
        private static SqlParameter IntParameter(string name, int value)
        {
            return new SqlParameter(name, SqlDbType.Int) { Value = value };
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建可空 SQL Server <c>int</c> 参数。</zh-CN>
        ///   <en>Creates a nullable SQL Server <c>int</c> parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>参数名。</zh-CN>
        ///   <en>Parameter name.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>可空整数值。</zh-CN>
        ///   <en>Nullable integer value.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>空值会转换为 <see cref="DBNull.Value"/> 的参数。</zh-CN>
        ///   <en>Parameter that maps null to <see cref="DBNull.Value"/>.</en>
        /// </l>
        /// </returns>
        private static SqlParameter NullableIntParameter(string name, int? value)
        {
            return new SqlParameter(name, SqlDbType.Int) { Value = value.HasValue ? (object)value.Value : DBNull.Value };
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建 SQL Server <c>bit</c> 参数。</zh-CN>
        ///   <en>Creates a SQL Server <c>bit</c> parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>参数名。</zh-CN>
        ///   <en>Parameter name.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>布尔值。</zh-CN>
        ///   <en>Boolean value.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>已指定类型和值的参数。</zh-CN>
        ///   <en>Parameter with type and value assigned.</en>
        /// </l>
        /// </returns>
        private static SqlParameter BoolParameter(string name, bool value)
        {
            return new SqlParameter(name, SqlDbType.Bit) { Value = value };
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建 SQL Server <c>nvarchar</c> 参数。</zh-CN>
        ///   <en>Creates a SQL Server <c>nvarchar</c> parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>参数名。</zh-CN>
        ///   <en>Parameter name.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>文本值；空字符串会按数据库空值写入。</zh-CN>
        ///   <en>Text value; empty strings are written as database nulls.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>空文本会转换为 <see cref="DBNull.Value"/> 的参数。</zh-CN>
        ///   <en>Parameter that maps empty text to <see cref="DBNull.Value"/>.</en>
        /// </l>
        /// </returns>
        private static SqlParameter TextParameter(string name, string value)
        {
            return new SqlParameter(name, SqlDbType.NVarChar) { Value = string.IsNullOrEmpty(value) ? (object)DBNull.Value : value };
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建 SQL Server <c>datetime2</c> 参数。</zh-CN>
        ///   <en>Creates a SQL Server <c>datetime2</c> parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>参数名。</zh-CN>
        ///   <en>Parameter name.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>UTC 时间值。</zh-CN>
        ///   <en>UTC date-time value.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>已指定类型和值的参数。</zh-CN>
        ///   <en>Parameter with type and value assigned.</en>
        /// </l>
        /// </returns>
        private static SqlParameter DateTime2Parameter(string name, DateTime value)
        {
            return new SqlParameter(name, SqlDbType.DateTime2) { Value = value };
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建可空 SQL Server <c>datetime2</c> 参数。</zh-CN>
        ///   <en>Creates a nullable SQL Server <c>datetime2</c> parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>参数名。</zh-CN>
        ///   <en>Parameter name.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>可空 UTC 时间值。</zh-CN>
        ///   <en>Nullable UTC date-time value.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>空值会转换为 <see cref="DBNull.Value"/> 的参数。</zh-CN>
        ///   <en>Parameter that maps null to <see cref="DBNull.Value"/>.</en>
        /// </l>
        /// </returns>
        private static SqlParameter NullableDateTime2Parameter(string name, DateTime? value)
        {
            return new SqlParameter(name, SqlDbType.DateTime2) { Value = value.HasValue ? (object)value.Value : DBNull.Value };
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>组织查询用的内部 SQL 投影。</zh-CN>
        ///   <en>Internal SQL projection used for organization queries.</en>
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
            ///   <zh-CN>排序值。</zh-CN>
            ///   <en>Sort order.</en>
            /// </lang>
            /// </summary>
            public int SortOrder { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>是否启用。</zh-CN>
            ///   <en>Whether the organization unit is active.</en>
            /// </lang>
            /// </summary>
            public bool IsActive { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>创建时间 UTC。</zh-CN>
            ///   <en>Creation time in UTC.</en>
            /// </lang>
            /// </summary>
            public DateTime CreatedUtc { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>最近更新时间 UTC。</zh-CN>
            ///   <en>Last update time in UTC.</en>
            /// </lang>
            /// </summary>
            public DateTime UpdatedUtc { get; set; }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工查询用的内部 SQL 投影。</zh-CN>
        ///   <en>Internal SQL projection used for employee queries.</en>
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
            ///   <zh-CN>员工工号。</zh-CN>
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
            ///   <en>Work email address.</en>
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
            ///   <zh-CN>员工生命周期状态。</zh-CN>
            ///   <en>Employee lifecycle status.</en>
            /// </lang>
            /// </summary>
            public string EmploymentStatus { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>入职时间 UTC。</zh-CN>
            ///   <en>Joined time in UTC.</en>
            /// </lang>
            /// </summary>
            public DateTime? JoinedUtc { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>离职时间 UTC。</zh-CN>
            ///   <en>Left time in UTC.</en>
            /// </lang>
            /// </summary>
            public DateTime? LeftUtc { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>来源系统。</zh-CN>
            ///   <en>Source system.</en>
            /// </lang>
            /// </summary>
            public string SourceSystem { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>最近更新时间 UTC。</zh-CN>
            ///   <en>Last update time in UTC.</en>
            /// </lang>
            /// </summary>
            public DateTime UpdatedUtc { get; set; }
        }
    }
}
