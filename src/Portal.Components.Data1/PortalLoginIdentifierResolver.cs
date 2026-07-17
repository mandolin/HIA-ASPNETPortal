using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：把登录输入解析为唯一门户用户标识的内部服务。
    ///
    /// English: Internal service that resolves a sign-in input to one unique Portal user identifier.
    /// </summary>
    /// <remarks>
    /// 中文：此服务只负责“输入是谁”，不验证密码，不决定角色，也不把员工工号当作凭据。P6.2 中员工工号
    /// 登录路径保持关闭，待 P6.3 正式员工表和当前有效绑定表完成后再启用。
    ///
    /// English: This service only answers "who does this input identify"; it does not validate passwords, decide roles,
    /// or treat employee codes as credentials. Employee-code sign-in remains disabled in P6.2 until P6.3 provides the
    /// formal employee table and current binding table.
    /// </remarks>
    internal sealed class PortalLoginIdentifierResolver
    {
        private readonly PortalSecurityDbContext context;
        private readonly bool userProfilesAvailable;

        internal PortalLoginIdentifierResolver(PortalSecurityDbContext context, bool userProfilesAvailable)
        {
            this.context = context;
            this.userProfilesAvailable = userProfilesAvailable;
        }

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

            return ResolveEmail(normalizedInput);
        }

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

        private List<int> QueryIds(string sql, string normalizedInput)
        {
            try
            {
                return context.Database.SqlQuery<int>(sql, normalizedInput).ToList();
            }
            catch (Exception)
            {
                // 中文：解析扩展表失败时按未命中处理，调用方仍可走旧路径或通用失败。
                // English: Treat extension-table resolution failures as no match so callers can still use legacy paths
                // or return a generic failure.
                return new List<int>();
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    internal sealed class PortalLoginIdentifierResolution
    {
        private PortalLoginIdentifierResolution(bool found, bool ambiguous, bool hasDecision, int userId)
        {
            Found = found;
            Ambiguous = ambiguous;
            HasDecision = hasDecision;
            UserId = userId;
        }

        internal bool Found { get; private set; }

        internal bool Ambiguous { get; private set; }

        internal bool HasDecision { get; private set; }

        internal int UserId { get; private set; }

        internal static PortalLoginIdentifierResolution CreateFound(int userId)
        {
            return new PortalLoginIdentifierResolution(true, false, true, userId);
        }

        internal static PortalLoginIdentifierResolution CreateAmbiguous()
        {
            return new PortalLoginIdentifierResolution(false, true, true, 0);
        }

        internal static PortalLoginIdentifierResolution CreateNotFound()
        {
            return new PortalLoginIdentifierResolution(false, false, true, 0);
        }

        internal static PortalLoginIdentifierResolution CreateNotFoundWithoutDecision()
        {
            return new PortalLoginIdentifierResolution(false, false, false, 0);
        }
    }
}
