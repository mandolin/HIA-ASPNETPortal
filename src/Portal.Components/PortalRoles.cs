using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>集中保存旧门户约定的稳定角色名称，避免页面代码散写魔法字符串。</zh-CN>
    ///   <en>Centralizes stable role names defined by the legacy Portal and avoids magic strings in page code.</en>
    /// </lang>
    /// </summary>
    public static class PortalRoleNames
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>旧门户约定的管理员角色名称。</zh-CN>
        ///   <en>Administrator role name defined by the legacy Portal.</en>
        /// </lang>
        /// </summary>
        public const string Administrators = "Admins";

        /// <summary>
        /// <lang>
        ///   <zh-CN>旧门户约定的所有用户虚拟角色名称；访问判断将其视为当前请求可访问的通配项。</zh-CN>
        ///   <en>Legacy virtual role for all users; access checks treat it as a wildcard that allows the current request.</en>
        /// </lang>
        /// </summary>
        public const string AllUsers = "All Users";
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>处理旧门户用分号保存角色列表的兼容格式，例如 <c>Admins;All Users;</c>。</zh-CN>
    ///   <en>Handles the legacy semicolon-separated role-list format, such as <c>Admins;All Users;</c>.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>解析会忽略空项和两端空白，并按不区分大小写去重。写出时保持分号分隔，但不会强制补末尾分号； 两种形式都能由 <see cref="Parse"/> 正确读取。</zh-CN>
    ///   <en>Parsing ignores empty entries and surrounding whitespace, then deduplicates case-insensitively. Writing preserves semicolon separation but does not force a trailing semicolon; both forms are readable by <see cref="Parse"/>.</en>
    /// </lang>
    /// </remarks>
    public static class PortalRoleParser
    {
        private static readonly char[] RoleSeparators = { ';' };

        /// <summary>
        /// <lang>
        ///   <zh-CN>将旧格式角色字符串解析为去空白、去空项且不区分大小写去重后的角色数组。</zh-CN>
        ///   <en>Parses a legacy role string into a trimmed, non-empty, case-insensitively deduplicated role array.</en>
        /// </lang>
        /// </summary>
        /// <param name="roles">
        /// <l>
        ///   <zh-CN>可为空的分号角色字符串。</zh-CN>
        ///   <en>Nullable semicolon-separated role string.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>规范化角色数组；空白输入返回空数组。</zh-CN>
        ///   <en>Normalized role array; blank input returns an empty array.</en>
        /// </l>
        /// </returns>
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
        /// <lang>
        ///   <zh-CN>判断旧格式角色字符串中是否包含指定角色，比较时不区分大小写。</zh-CN>
        ///   <en>Determines whether a legacy role string contains the specified role using case-insensitive comparison.</en>
        /// </lang>
        /// </summary>
        /// <param name="roles">
        /// <l>
        ///   <zh-CN>可为空的分号角色字符串。</zh-CN>
        ///   <en>Nullable semicolon-separated role string.</en>
        /// </l>
        /// </param>
        /// <param name="roleName">
        /// <l>
        ///   <zh-CN>要查找的角色名称。</zh-CN>
        ///   <en>Role name to find.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>存在匹配角色时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when a matching role exists.</en>
        /// </l>
        /// </returns>
        public static bool Contains(string roles, string roleName)
        {
            return Parse(roles).Any(role => string.Equals(role, roleName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将角色集合写成旧门户兼容的分号分隔格式。</zh-CN>
        ///   <en>Writes a role collection in the legacy Portal-compatible semicolon-separated format.</en>
        /// </lang>
        /// </summary>
        /// <param name="roles">
        /// <l>
        ///   <zh-CN>可为空的角色集合。</zh-CN>
        ///   <en>Nullable role collection.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>去空白、去空项和去重后的分号字符串；不会强制附加末尾分号。</zh-CN>
        ///   <en>Trimmed, non-empty, deduplicated semicolon string without a forced trailing semicolon.</en>
        /// </l>
        /// </returns>
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
