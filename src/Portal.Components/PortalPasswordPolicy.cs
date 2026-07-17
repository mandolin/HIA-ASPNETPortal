using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户临时密码复杂度策略，用于 P6.2 企业用户生命周期的注册、建号和重置密码入口。
    ///
    /// English: Temporary Portal password-complexity policy for the P6.2 enterprise user lifecycle registration,
    /// account-creation, and password-reset entry points.
    /// </summary>
    /// <remarks>
    /// 中文：这是完整企业合规矩阵形成前的最小策略，当前要求不少于 8 位，并在大写字母、小写字母、
    /// 数字和特殊字符中至少满足 3 类。它不处理历史密码、字典检测、失败锁定或客户端加密协议。
    ///
    /// English: This is the minimum policy before the full enterprise compliance matrix is finalized. It currently
    /// requires at least 8 characters and at least 3 of uppercase letters, lowercase letters, digits, and special
    /// characters. It does not handle historical passwords, dictionary checks, failure lockout, or client-side
    /// encryption protocols.
    /// </remarks>
    public static class PortalPasswordPolicy
    {
        /// <summary>
        /// 中文：当前临时策略要求的最小长度。
        ///
        /// English: Minimum length required by the current temporary policy.
        /// </summary>
        public const int MinimumLength = 8;

        /// <summary>
        /// 中文：当前临时策略要求满足的字符类别数量。
        ///
        /// English: Number of character categories required by the current temporary policy.
        /// </summary>
        public const int RequiredCategoryCount = 3;

        /// <summary>
        /// 中文：校验密码是否满足当前临时复杂度策略。
        ///
        /// English: Validates whether a password satisfies the current temporary complexity policy.
        /// </summary>
        /// <param name="password">中文：一次性提交的密码输入；调用方不得记录。English: One-time submitted password input; callers must not log it.</param>
        /// <param name="message">中文：可显示给用户的失败说明。English: Display-safe failure message.</param>
        /// <returns>中文：满足策略时为 <c>true</c>。English: <c>true</c> when the policy is satisfied.</returns>
        public static bool TryValidate(string password, out string message)
        {
            if (password == null || password.Length < MinimumLength)
            {
                message = BuildDisplayMessage();
                return false;
            }

            int categoryCount = CountCategories(password);
            if (categoryCount < RequiredCategoryCount)
            {
                message = BuildDisplayMessage();
                return false;
            }

            message = string.Empty;
            return true;
        }

        /// <summary>
        /// 中文：返回当前策略的可展示说明。
        ///
        /// English: Returns a display-safe description of the current policy.
        /// </summary>
        /// <returns>中文：策略说明。English: Policy description.</returns>
        public static string BuildDisplayMessage()
        {
            return "密码至少 8 位，并且需要在大写字母、小写字母、数字、特殊字符中至少包含 3 类。";
        }

        private static int CountCategories(string password)
        {
            bool hasUpper = false;
            bool hasLower = false;
            bool hasDigit = false;
            bool hasSpecial = false;

            foreach (char character in password)
            {
                if (char.IsUpper(character))
                {
                    hasUpper = true;
                }
                else if (char.IsLower(character))
                {
                    hasLower = true;
                }
                else if (char.IsDigit(character))
                {
                    hasDigit = true;
                }
                else
                {
                    hasSpecial = true;
                }
            }

            return Convert.ToInt32(hasUpper) +
                   Convert.ToInt32(hasLower) +
                   Convert.ToInt32(hasDigit) +
                   Convert.ToInt32(hasSpecial);
        }
    }
}
