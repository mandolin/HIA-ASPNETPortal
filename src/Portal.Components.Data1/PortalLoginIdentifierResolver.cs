using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>把登录输入解析为唯一门户用户标识的内部服务。</zh-CN>
    ///   <en>Internal service that resolves a sign-in input to one unique Portal user identifier.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>此服务只负责“输入是谁”，不验证密码，不决定角色，也不把员工工号当作凭据。P6.3-S5 起，员工号只有在 Active 员工和 Active 账号绑定同时存在时才会解析为门户账号。</zh-CN>
    ///   <en>This service only answers "who does this input identify"; it does not validate passwords, decide roles, or treat employee codes as credentials. Starting in P6.3-S5, employee codes resolve to Portal accounts only when both an active employee and an active user binding exist.</en>
    /// </lang>
    /// </remarks>
    internal sealed class PortalLoginIdentifierResolver
    {
        private readonly PortalSecurityDbContext context;
        private readonly bool userProfilesAvailable;
        private readonly bool employeeCodeSignInAvailable;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化登录标识解析器。</zh-CN>
        ///   <en>Initializes the login-identifier resolver.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>安全相关 EF 上下文。</zh-CN>
        ///   <en>Security-related EF context.</en>
        /// </l>
        /// </param>
        /// <param name="userProfilesAvailable">
        /// <l>
        ///   <zh-CN>用户资料扩展表是否可用。</zh-CN>
        ///   <en>Whether the user-profile extension table is available.</en>
        /// </l>
        /// </param>
        /// <param name="employeeCodeSignInAvailable">
        /// <l>
        ///   <zh-CN>工号登录解析所需员工/绑定表是否可用。</zh-CN>
        ///   <en>Whether employee and binding tables required for employee-code sign-in are available.</en>
        /// </l>
        /// </param>
        internal PortalLoginIdentifierResolver(
            PortalSecurityDbContext context,
            bool userProfilesAvailable,
            bool employeeCodeSignInAvailable)
        {
            this.context = context;
            this.userProfilesAvailable = userProfilesAvailable;
            this.employeeCodeSignInAvailable = employeeCodeSignInAvailable;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把一次登录输入解析为唯一门户用户。</zh-CN>
        ///   <en>Resolves one sign-in input to a unique Portal user.</en>
        /// </lang>
        /// </summary>
        /// <param name="input">
        /// <l>
        ///   <zh-CN>用户输入的登录名、邮箱或员工工号。</zh-CN>
        ///   <en>User-entered login name, email address or employee code.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>解析结果；可能是唯一命中、歧义、未命中或未作终止判断。</zh-CN>
        ///   <en>Resolution result; it may be uniquely found, ambiguous, not found or non-terminal.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>解析顺序优先使用新用户资料登录名，其次旧用户表名称，再尝试工号，最后合并邮箱结果。前两个唯一字段出现未命中时允许继续向后查找。</zh-CN>
        ///   <en>The resolver first checks the new profile login name, then the legacy user name, then employee code, and finally merged email results. Not-found results from the first two unique-field checks may continue to later checks.</en>
        /// </lang>
        /// </remarks>
        internal PortalLoginIdentifierResolution Resolve(string input)
        {
            string normalizedInput = Normalize(input);
            if (string.IsNullOrEmpty(normalizedInput))
            {
                return PortalLoginIdentifierResolution.CreateNotFound();
            }

            if (userProfilesAvailable)
            {
                PortalLoginIdentifierResolution profileLoginName = ResolveSingle(
                    "SELECT TOP (2) [UserId] FROM [dbo].[PortalBiz_UserProfiles] WHERE [LoginName] = @p0",
                    normalizedInput);
                if (profileLoginName.HasDecision)
                {
                    return profileLoginName;
                }
            }

            PortalLoginIdentifierResolution legacyName = ResolveSingle(
                "SELECT TOP (2) [UserID] FROM [dbo].[Portal_Users] WHERE [Name] = @p0",
                normalizedInput);
            if (legacyName.HasDecision)
            {
                return legacyName;
            }

            if (employeeCodeSignInAvailable)
            {
                PortalLoginIdentifierResolution employeeCode = ResolveEmployeeCode(normalizedInput);
                if (employeeCode.HasDecision)
                {
                    return employeeCode;
                }
            }

            return ResolveEmail(normalizedInput);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按员工工号解析当前有效门户账号。</zh-CN>
        ///   <en>Resolves the current active Portal account by employee code.</en>
        /// </lang>
        /// </summary>
        /// <param name="normalizedInput">
        /// <l>
        ///   <zh-CN>已裁剪的登录输入。</zh-CN>
        ///   <en>Trimmed sign-in input.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>员工和绑定均 Active 时的解析结果。</zh-CN>
        ///   <en>Resolution result when both employee and binding are active.</en>
        /// </l>
        /// </returns>
        private PortalLoginIdentifierResolution ResolveEmployeeCode(string normalizedInput)
        {
            return ResolveSingle(
                @"
SELECT TOP (2) [Binding].[UserId]
FROM [dbo].[PortalBiz_UserEmployeeBindings] AS [Binding]
INNER JOIN [dbo].[PortalBiz_Employees] AS [Employee]
    ON [Employee].[EmployeeId] = [Binding].[EmployeeId]
WHERE [Binding].[BindingStatus] = N'Active'
  AND [Employee].[EmploymentStatus] = N'Active'
  AND [Employee].[EmployeeCode] = @p0",
                normalizedInput);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按邮箱在新旧资料来源之间合并解析门户账号。</zh-CN>
        ///   <en>Resolves a Portal account by merging email matches from new and legacy profile sources.</en>
        /// </lang>
        /// </summary>
        /// <param name="normalizedInput">
        /// <l>
        ///   <zh-CN>已裁剪的邮箱输入。</zh-CN>
        ///   <en>Trimmed email input.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>邮箱唯一命中、歧义或未命中的最终结果。</zh-CN>
        ///   <en>Final email result: unique match, ambiguous match or not found.</en>
        /// </l>
        /// </returns>
        private PortalLoginIdentifierResolution ResolveEmail(string normalizedInput)
        {
            var ids = new List<int>();

            if (userProfilesAvailable)
            {
                ids.AddRange(QueryIds(
                    "SELECT [UserId] FROM [dbo].[PortalBiz_UserProfiles] WHERE [PreferredEmail] = @p0",
                    normalizedInput));
            }

            ids.AddRange(QueryIds(
                "SELECT [UserID] FROM [dbo].[Portal_Users] WHERE [Email] = @p0",
                normalizedInput));

            List<int> distinctIds = ids.Distinct().Take(2).ToList();
            if (distinctIds.Count == 1)
            {
                return PortalLoginIdentifierResolution.CreateFound(distinctIds[0]);
            }

            return distinctIds.Count > 1
                ? PortalLoginIdentifierResolution.CreateAmbiguous()
                : PortalLoginIdentifierResolution.CreateNotFound();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>执行一个应当唯一的标识查询。</zh-CN>
        ///   <en>Executes an identifier query that should be unique.</en>
        /// </lang>
        /// </summary>
        /// <param name="sql">
        /// <l>
        ///   <zh-CN>受控 SQL 模板。</zh-CN>
        ///   <en>Controlled SQL template.</en>
        /// </l>
        /// </param>
        /// <param name="normalizedInput">
        /// <l>
        ///   <zh-CN>已裁剪的查询值。</zh-CN>
        ///   <en>Trimmed query value.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>唯一命中、歧义或未命中且不终止解析的结果。</zh-CN>
        ///   <en>Unique, ambiguous or not-found-without-decision result.</en>
        /// </l>
        /// </returns>
        private PortalLoginIdentifierResolution ResolveSingle(string sql, string normalizedInput)
        {
            List<int> ids = QueryIds(sql, normalizedInput).Distinct().Take(2).ToList();
            if (ids.Count == 1)
            {
                return PortalLoginIdentifierResolution.CreateFound(ids[0]);
            }

            return ids.Count > 1
                ? PortalLoginIdentifierResolution.CreateAmbiguous()
                : PortalLoginIdentifierResolution.CreateNotFoundWithoutDecision();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>执行登录标识查询并返回最多两个候选用户标识。</zh-CN>
        ///   <en>Executes a login-identifier query and returns up to two candidate user ids.</en>
        /// </lang>
        /// </summary>
        /// <param name="sql">
        /// <l>
        ///   <zh-CN>受控 SQL 模板。</zh-CN>
        ///   <en>Controlled SQL template.</en>
        /// </l>
        /// </param>
        /// <param name="normalizedInput">
        /// <l>
        ///   <zh-CN>已裁剪的查询值。</zh-CN>
        ///   <en>Trimmed query value.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>候选用户标识列表；查询异常时返回空集合。</zh-CN>
        ///   <en>Candidate user id list; empty when querying fails.</en>
        /// </l>
        /// </returns>
        private List<int> QueryIds(string sql, string normalizedInput)
        {
            try
            {
                return context.Database.SqlQuery<int>(sql, normalizedInput).ToList();
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>解析扩展表失败时按未命中处理，调用方仍可走旧路径或通用失败。</zh-CN>
                //   <en>Treat extension-table resolution failures as no match so callers can still use legacy paths or return a generic failure.</en>
                // </lang>
                return new List<int>();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>裁剪登录输入。</zh-CN>
        ///   <en>Trims a sign-in input.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>原始输入。</zh-CN>
        ///   <en>Raw input.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>裁剪后的输入；空白值返回空字符串。</zh-CN>
        ///   <en>Trimmed input, or an empty string for blank values.</en>
        /// </l>
        /// </returns>
        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>登录标识解析结果。</zh-CN>
    ///   <en>Login-identifier resolution result.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>`HasDecision` 用于区分“此路径确认失败”和“此路径未命中但可继续尝试其他路径”。</zh-CN>
    ///   <en>`HasDecision` distinguishes "this path definitively failed" from "this path did not match and later paths may still be tried".</en>
    /// </lang>
    /// </remarks>
    internal sealed class PortalLoginIdentifierResolution
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化不可变解析结果。</zh-CN>
        ///   <en>Initializes an immutable resolution result.</en>
        /// </lang>
        /// </summary>
        /// <param name="found">
        /// <l>
        ///   <zh-CN>是否唯一命中用户。</zh-CN>
        ///   <en>Whether one user was uniquely found.</en>
        /// </l>
        /// </param>
        /// <param name="ambiguous">
        /// <l>
        ///   <zh-CN>是否出现多个候选。</zh-CN>
        ///   <en>Whether multiple candidates were found.</en>
        /// </l>
        /// </param>
        /// <param name="hasDecision">
        /// <l>
        ///   <zh-CN>当前解析路径是否已经给出终止性结论。</zh-CN>
        ///   <en>Whether the current resolution path has made a terminal decision.</en>
        /// </l>
        /// </param>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>唯一命中的门户用户标识。</zh-CN>
        ///   <en>Uniquely matched Portal user id.</en>
        /// </l>
        /// </param>
        private PortalLoginIdentifierResolution(bool found, bool ambiguous, bool hasDecision, int userId)
        {
            Found = found;
            Ambiguous = ambiguous;
            HasDecision = hasDecision;
            UserId = userId;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否唯一命中用户。</zh-CN>
        ///   <en>Whether one user was uniquely found.</en>
        /// </lang>
        /// </summary>
        internal bool Found { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否出现多个候选用户。</zh-CN>
        ///   <en>Whether multiple candidate users were found.</en>
        /// </lang>
        /// </summary>
        internal bool Ambiguous { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前路径是否已经给出终止性结论。</zh-CN>
        ///   <en>Whether the current path has made a terminal decision.</en>
        /// </lang>
        /// </summary>
        internal bool HasDecision { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>唯一命中的门户用户标识。</zh-CN>
        ///   <en>Uniquely matched Portal user id.</en>
        /// </lang>
        /// </summary>
        internal int UserId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建唯一命中结果。</zh-CN>
        ///   <en>Creates a uniquely found result.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>唯一命中的门户用户标识。</zh-CN>
        ///   <en>Uniquely matched Portal user id.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>终止性的成功解析结果。</zh-CN>
        ///   <en>A terminal successful resolution result.</en>
        /// </l>
        /// </returns>
        internal static PortalLoginIdentifierResolution CreateFound(int userId)
        {
            return new PortalLoginIdentifierResolution(true, false, true, userId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建歧义结果。</zh-CN>
        ///   <en>Creates an ambiguous result.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>终止性的歧义解析结果。</zh-CN>
        ///   <en>A terminal ambiguous resolution result.</en>
        /// </l>
        /// </returns>
        internal static PortalLoginIdentifierResolution CreateAmbiguous()
        {
            return new PortalLoginIdentifierResolution(false, true, true, 0);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建终止性未命中结果。</zh-CN>
        ///   <en>Creates a terminal not-found result.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>没有其他路径需要继续尝试的未命中结果。</zh-CN>
        ///   <en>A not-found result that does not need later paths to continue.</en>
        /// </l>
        /// </returns>
        internal static PortalLoginIdentifierResolution CreateNotFound()
        {
            return new PortalLoginIdentifierResolution(false, false, true, 0);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建非终止性未命中结果。</zh-CN>
        ///   <en>Creates a non-terminal not-found result.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>允许调用方继续尝试其他解析路径的未命中结果。</zh-CN>
        ///   <en>A not-found result that allows callers to continue trying later resolution paths.</en>
        /// </l>
        /// </returns>
        internal static PortalLoginIdentifierResolution CreateNotFoundWithoutDecision()
        {
            return new PortalLoginIdentifierResolution(false, false, false, 0);
        }
    }
}
