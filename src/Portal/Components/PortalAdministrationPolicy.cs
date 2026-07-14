using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：集中处理旧后台页面可安全复用的文本和角色名称输入规则。
    ///
    /// English: Centralizes text and role-name input rules that legacy administration pages can safely reuse.
    /// </summary>
    /// <remarks>
    /// 中文：该策略只覆盖本阶段已有字段的格式和长度边界，不替代未来的本地化、细粒度权限、
    /// 完整用户资料校验或业务专用的命名规则。
    ///
    /// English: This policy covers only format and length boundaries for fields in the current phase. It does not
    /// replace future localization, fine-grained authorization, full user-profile validation, or domain-specific
    /// naming rules.
    /// </remarks>
    public static class PortalAdministrationPolicy
    {
        /// <summary>
        /// 中文：旧角色表 <c>RoleName</c> 的最大字符数。
        ///
        /// English: Maximum character count of the legacy <c>RoleName</c> column.
        /// </summary>
        public const int MaximumRoleNameLength = 50;

        /// <summary>
        /// 中文：将必填单行后台文本去除两端空白并校验长度与控制字符。
        ///
        /// English: Trims and validates required single-line administration text for length and control characters.
        /// </summary>
        /// <param name="candidate">中文：用户提交的候选文本。English: Candidate text submitted by the user.</param>
        /// <param name="maximumLength">中文：目标存储字段允许的最大字符数。English: Maximum characters allowed by the target storage field.</param>
        /// <param name="normalizedValue">中文：成功时返回规范文本；失败时为空。English: Normalized text on success; otherwise empty.</param>
        /// <returns>中文：文本非空、单行且未超过限制时为 <c>true</c>。English: <c>true</c> when text is nonempty, single-line, and within the limit.</returns>
        public static bool TryNormalizeRequiredSingleLineText(
            string candidate,
            int maximumLength,
            out string normalizedValue)
        {
            normalizedValue = Normalize(candidate);
            return maximumLength > 0 && normalizedValue.Length > 0 && normalizedValue.Length <= maximumLength &&
                   !ContainsControlCharacter(normalizedValue);
        }

        /// <summary>
        /// 中文：将可为空的单行后台文本去除两端空白并校验长度与控制字符。
        ///
        /// English: Trims and validates optional single-line administration text for length and control characters.
        /// </summary>
        /// <param name="candidate">中文：用户提交的候选文本。English: Candidate text submitted by the user.</param>
        /// <param name="maximumLength">中文：目标存储字段允许的最大字符数。English: Maximum characters allowed by the target storage field.</param>
        /// <param name="normalizedValue">中文：成功时返回规范文本；失败时为空。English: Normalized text on success; otherwise empty.</param>
        /// <returns>中文：文本为空或为合法单行文本时为 <c>true</c>。English: <c>true</c> when text is empty or valid single-line text.</returns>
        public static bool TryNormalizeOptionalSingleLineText(
            string candidate,
            int maximumLength,
            out string normalizedValue)
        {
            normalizedValue = Normalize(candidate);
            return maximumLength > 0 && normalizedValue.Length <= maximumLength &&
                   !ContainsControlCharacter(normalizedValue);
        }

        /// <summary>
        /// 中文：校验旧门户角色名称，排除分号分隔符和 <c>All Users</c> 虚拟角色。
        ///
        /// English: Validates a legacy Portal role name, excluding the semicolon delimiter and the <c>All Users</c> virtual role.
        /// </summary>
        /// <param name="candidate">中文：管理员提交的候选角色名称。English: Candidate role name submitted by an administrator.</param>
        /// <param name="normalizedRoleName">中文：成功时返回规范角色名；失败时为空。English: Normalized role name on success; otherwise empty.</param>
        /// <returns>中文：角色名可安全存入旧分号角色契约时为 <c>true</c>。English: <c>true</c> when the role name can safely enter the legacy semicolon role contract.</returns>
        public static bool TryNormalizeRoleName(string candidate, out string normalizedRoleName)
        {
            if (!TryNormalizeRequiredSingleLineText(candidate, MaximumRoleNameLength, out normalizedRoleName))
            {
                return false;
            }

            return normalizedRoleName.IndexOf(';') < 0 &&
                   !string.Equals(normalizedRoleName, PortalRoleNames.AllUsers, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 中文：判断名称是否是当前阶段使用名称约定保护的后台 Tab。
        ///
        /// English: Determines whether a name identifies the administration Tab protected by the current naming convention.
        /// </summary>
        /// <param name="tabName">中文：Tab 显示名称。English: Tab display name.</param>
        /// <returns>中文：名称为 <c>Admin</c> 时为 <c>true</c>。English: <c>true</c> when the name is <c>Admin</c>.</returns>
        public static bool IsProtectedAdministrationTabName(string tabName)
        {
            return string.Equals(tabName, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static bool ContainsControlCharacter(string value)
        {
            foreach (char character in value)
            {
                if (char.IsControl(character))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
