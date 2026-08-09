using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>集中处理旧后台页面可安全复用的文本和角色名称输入规则。</zh-CN>
    ///   <en>Centralizes text and role-name input rules that legacy administration pages can safely reuse.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该策略只覆盖本阶段已有字段的格式和长度边界，不替代未来的本地化、细粒度权限、 完整用户资料校验或业务专用的命名规则。</zh-CN>
    ///   <en>This policy covers only format and length boundaries for fields in the current phase. It does not replace future localization, fine-grained authorization, full user-profile validation, or domain-specific naming rules.</en>
    /// </lang>
    /// </remarks>
    public static class PortalAdministrationPolicy
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>旧角色表 <c>RoleName</c> 的最大字符数。</zh-CN>
        ///   <en>Maximum character count of the legacy <c>RoleName</c> column.</en>
        /// </lang>
        /// </summary>
        public const int MaximumRoleNameLength = 50;

        /// <summary>
        /// <lang>
        ///   <zh-CN>将必填单行后台文本去除两端空白并校验长度与控制字符。</zh-CN>
        ///   <en>Trims and validates required single-line administration text for length and control characters.</en>
        /// </lang>
        /// </summary>
        /// <param name="candidate">
        /// <l>
        ///   <zh-CN>用户提交的候选文本。</zh-CN>
        ///   <en>Candidate text submitted by the user.</en>
        /// </l>
        /// </param>
        /// <param name="maximumLength">
        /// <l>
        ///   <zh-CN>目标存储字段允许的最大字符数。</zh-CN>
        ///   <en>Maximum characters allowed by the target storage field.</en>
        /// </l>
        /// </param>
        /// <param name="normalizedValue">
        /// <l>
        ///   <zh-CN>成功时返回规范文本；失败时为空。</zh-CN>
        ///   <en>Normalized text on success; otherwise empty.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>文本非空、单行且未超过限制时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when text is nonempty, single-line, and within the limit.</en>
        /// </l>
        /// </returns>
        public static bool TryNormalizeRequiredSingleLineText(
            string candidate,
            int maximumLength,
            out string normalizedValue)
        {
            // <lang>
            //   <zh-CN>先统一空值和两端空白，再由同一规范值承担长度与控制字符检查；失败时 out 值仍保留可诊断的规范结果。</zh-CN>
            //   <en>Normalize null and surrounding whitespace first, then apply length and control-character checks to that same value; the out value remains the diagnostic normalized result even on failure.</en>
            // </lang>
            normalizedValue = Normalize(candidate);
            // <lang>
            //   <zh-CN>必填文本必须同时满足正长度、非空、目标字段长度和无控制字符四项约束。</zh-CN>
            //   <en>Required text must satisfy positive length, nonempty content, the target-field limit, and no-control-character constraints together.</en>
            // </lang>
            return maximumLength > 0 && normalizedValue.Length > 0 && normalizedValue.Length <= maximumLength &&
                   !ContainsControlCharacter(normalizedValue);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将可为空的单行后台文本去除两端空白并校验长度与控制字符。</zh-CN>
        ///   <en>Trims and validates optional single-line administration text for length and control characters.</en>
        /// </lang>
        /// </summary>
        /// <param name="candidate">
        /// <l>
        ///   <zh-CN>用户提交的候选文本。</zh-CN>
        ///   <en>Candidate text submitted by the user.</en>
        /// </l>
        /// </param>
        /// <param name="maximumLength">
        /// <l>
        ///   <zh-CN>目标存储字段允许的最大字符数。</zh-CN>
        ///   <en>Maximum characters allowed by the target storage field.</en>
        /// </l>
        /// </param>
        /// <param name="normalizedValue">
        /// <l>
        ///   <zh-CN>成功时返回规范文本；失败时为空。</zh-CN>
        ///   <en>Normalized text on success; otherwise empty.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>文本为空或为合法单行文本时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when text is empty or valid single-line text.</en>
        /// </l>
        /// </returns>
        public static bool TryNormalizeOptionalSingleLineText(
            string candidate,
            int maximumLength,
            out string normalizedValue)
        {
            // <lang>
            //   <zh-CN>可选字段同样先取得规范值；空字符串由长度检查自然放行，但控制字符和无效上限仍拒绝。</zh-CN>
            //   <en>Optional fields still produce a normalized value first; an empty string passes naturally, while control characters and an invalid limit remain rejected.</en>
            // </lang>
            normalizedValue = Normalize(candidate);
            // <lang>
            //   <zh-CN>允许空值但不放宽字段上限或控制字符边界。</zh-CN>
            //   <en>Allow an empty value without relaxing the field limit or control-character boundary.</en>
            // </lang>
            return maximumLength > 0 && normalizedValue.Length <= maximumLength &&
                   !ContainsControlCharacter(normalizedValue);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>校验旧门户角色名称，排除分号分隔符和 <c>All Users</c> 虚拟角色。</zh-CN>
        ///   <en>Validates a legacy Portal role name, excluding the semicolon delimiter and the <c>All Users</c> virtual role.</en>
        /// </lang>
        /// </summary>
        /// <param name="candidate">
        /// <l>
        ///   <zh-CN>管理员提交的候选角色名称。</zh-CN>
        ///   <en>Candidate role name submitted by an administrator.</en>
        /// </l>
        /// </param>
        /// <param name="normalizedRoleName">
        /// <l>
        ///   <zh-CN>成功时返回规范角色名；失败时为空。</zh-CN>
        ///   <en>Normalized role name on success; otherwise empty.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>角色名可安全存入旧分号角色契约时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the role name can safely enter the legacy semicolon role contract.</en>
        /// </l>
        /// </returns>
        public static bool TryNormalizeRoleName(string candidate, out string normalizedRoleName)
        {
            if (!TryNormalizeRequiredSingleLineText(candidate, MaximumRoleNameLength, out normalizedRoleName))
            {
                return false;
            }

            // <lang>
            //   <zh-CN>旧角色存储使用分号分隔且保留 All Users 虚拟角色；两者都不能作为新角色名写入。</zh-CN>
            //   <en>The legacy role store uses semicolons as delimiters and reserves the All Users virtual role; neither may be written as a new role name.</en>
            // </lang>
            return normalizedRoleName.IndexOf(';') < 0 &&
                   !string.Equals(normalizedRoleName, PortalRoleNames.AllUsers, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断名称是否是当前阶段使用名称约定保护的后台 Tab。</zh-CN>
        ///   <en>Determines whether a name identifies the administration Tab protected by the current naming convention.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabName">
        /// <l>
        ///   <zh-CN>Tab 显示名称。</zh-CN>
        ///   <en>Tab display name.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>名称为 <c>Admin</c> 时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the name is <c>Admin</c>.</en>
        /// </l>
        /// </returns>
        public static bool IsProtectedAdministrationTabName(string tabName)
        {
            return string.Equals(tabName, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将可为空的后台文本规范化为空字符串或去除两端空白的值。</zh-CN>
        ///   <en>Normalizes optional administration text to an empty string or a value trimmed at both ends.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>待规范化的候选文本，可为 <c>null</c>。</zh-CN>
        ///   <en>Candidate text to normalize; may be <c>null</c>.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>空白输入返回空字符串，否则返回去除两端空白的文本。</zh-CN>
        ///   <en>An empty string for blank input; otherwise text trimmed at both ends.</en>
        /// </l>
        /// </returns>
        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查规范文本是否包含会破坏单行后台字段契约的控制字符。</zh-CN>
        ///   <en>Checks whether normalized text contains control characters that would violate a single-line administration-field contract.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>已经完成空白规范化的文本。</zh-CN>
        ///   <en>Text that has already been whitespace-normalized.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>发现任一控制字符时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when any control character is present.</en>
        /// </l>
        /// </returns>
        private static bool ContainsControlCharacter(string value)
        {
            // <lang>
            //   <zh-CN>逐字符检查以覆盖换行、制表等不可进入单行字段的控制码；调用方已保证 value 非空。</zh-CN>
            //   <en>Inspect each character to cover newline, tab, and other control codes excluded from single-line fields; callers provide a non-null value.</en>
            // </lang>
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
