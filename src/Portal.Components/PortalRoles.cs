using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：集中保存旧门户约定的稳定角色名称，避免页面代码散写魔法字符串。
    ///
    /// English: Centralizes stable role names defined by the legacy Portal and avoids magic strings in page code.
    /// </summary>
    public static class PortalRoleNames
    {
        /// <summary>
        /// 中文：旧门户约定的管理员角色名称。
        ///
        /// English: Administrator role name defined by the legacy Portal.
        /// </summary>
        public const string Administrators = "Admins";

        /// <summary>
        /// 中文：旧门户约定的所有用户虚拟角色名称；访问判断将其视为当前请求可访问的通配项。
        ///
        /// English: Legacy virtual role for all users; access checks treat it as a wildcard that allows the current request.
        /// </summary>
        public const string AllUsers = "All Users";
    }

    /// <summary>
    /// 中文：处理旧门户用分号保存角色列表的兼容格式，例如 <c>Admins;All Users;</c>。
    ///
    /// English: Handles the legacy semicolon-separated role-list format, such as <c>Admins;All Users;</c>.
    /// </summary>
    /// <remarks>
    /// 中文：解析会忽略空项和两端空白，并按不区分大小写去重。写出时保持分号分隔，但不会强制补末尾分号；
    /// 两种形式都能由 <see cref="Parse"/> 正确读取。
    ///
    /// English: Parsing ignores empty entries and surrounding whitespace, then deduplicates case-insensitively.
    /// Writing preserves semicolon separation but does not force a trailing semicolon; both forms are readable by <see cref="Parse"/>.
    /// </remarks>
    public static class PortalRoleParser
    {
        private static readonly char[] RoleSeparators = { ';' };

        /// <summary>
        /// 中文：将旧格式角色字符串解析为去空白、去空项且不区分大小写去重后的角色数组。
        ///
        /// English: Parses a legacy role string into a trimmed, non-empty, case-insensitively deduplicated role array.
        /// </summary>
        /// <param name="roles">中文：可为空的分号角色字符串。English: Nullable semicolon-separated role string.</param>
        /// <returns>中文：规范化角色数组；空白输入返回空数组。English: Normalized role array; blank input returns an empty array.</returns>
        public static string[] Parse(string roles)
        {
            if (string.IsNullOrWhiteSpace(roles))
            {
                return new string[0];
            }

            return roles
                .Split(RoleSeparators, StringSplitOptions.RemoveEmptyEntries)
                .Select(role => role.Trim())
                .Where(role => role.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        /// <summary>
        /// 中文：判断旧格式角色字符串中是否包含指定角色，比较时不区分大小写。
        ///
        /// English: Determines whether a legacy role string contains the specified role using case-insensitive comparison.
        /// </summary>
        /// <param name="roles">中文：可为空的分号角色字符串。English: Nullable semicolon-separated role string.</param>
        /// <param name="roleName">中文：要查找的角色名称。English: Role name to find.</param>
        /// <returns>中文：存在匹配角色时为 <c>true</c>。English: <c>true</c> when a matching role exists.</returns>
        public static bool Contains(string roles, string roleName)
        {
            return Parse(roles).Any(role => string.Equals(role, roleName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 中文：将角色集合写成旧门户兼容的分号分隔格式。
        ///
        /// English: Writes a role collection in the legacy Portal-compatible semicolon-separated format.
        /// </summary>
        /// <param name="roles">中文：可为空的角色集合。English: Nullable role collection.</param>
        /// <returns>中文：去空白、去空项和去重后的分号字符串；不会强制附加末尾分号。English: Trimmed, non-empty, deduplicated semicolon string without a forced trailing semicolon.</returns>
        public static string Join(IEnumerable<string> roles)
        {
            if (roles == null)
            {
                return string.Empty;
            }

            return string.Join(
                ";",
                roles
                    .Where(role => !string.IsNullOrWhiteSpace(role))
                    .Select(role => role.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase));
        }
    }
}
