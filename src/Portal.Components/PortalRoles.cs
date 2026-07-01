using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 统一保存旧门户中约定俗成的角色名称，避免在页面代码中散写魔法字符串。
    /// </summary>
    public static class PortalRoleNames
    {
        /// <summary>
        /// 旧门户约定的管理员角色名称。
        /// </summary>
        public const string Administrators = "Admins";

        /// <summary>
        /// 旧门户约定的所有用户虚拟角色名称。
        /// </summary>
        public const string AllUsers = "All Users";
    }

    /// <summary>
    /// 处理旧门户用分号保存角色列表的兼容格式，例如 "Admins;All Users;"。
    /// </summary>
    public static class PortalRoleParser
    {
        private static readonly char[] RoleSeparators = { ';' };

        /// <summary>
        /// 将旧格式角色字符串解析为去空白、去空项后的角色数组。
        /// </summary>
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
        /// 判断旧格式角色字符串中是否包含指定角色。
        /// </summary>
        public static bool Contains(string roles, string roleName)
        {
            return Parse(roles).Any(role => string.Equals(role, roleName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 将角色集合重新写成旧门户兼容的分号分隔格式。
        /// </summary>
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
