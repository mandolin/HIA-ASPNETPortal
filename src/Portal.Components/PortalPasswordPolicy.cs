using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户密码复杂度策略，用于企业用户生命周期的注册、建号和重置密码入口。
    ///
    /// English: Portal password-complexity policy for the enterprise user lifecycle registration,
    /// account-creation, and password-reset entry points.
    /// </summary>
    /// <remarks>
    /// 中文：组件层保留 8 位/3 类作为硬下限；Web 层可在启动时注入运行期策略选项，让系统管理设置
    /// 能控制更高强度的最小长度、类别数量、弱口令字典和账号上下文词限制。此类型不处理历史密码、
    /// 失败锁定或客户端加密协议。
    ///
    /// English: The component layer keeps 8 characters / 3 categories as the hard lower bound. The Web layer may
    /// inject runtime policy options on startup so system settings can control stronger minimum length, category
    /// count, weak-password dictionary checks, and account-context-word restrictions. This type does not handle
    /// password history, failure lockout, or client-side encryption protocols.
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

        private static readonly object OptionsProviderLock = new object();
        private static Func<PortalPasswordPolicyOptions> optionsProvider;

        private static readonly string[] WeakPasswordDictionary =
        {
            "123456",
            "12345678",
            "123456789",
            "111111",
            "000000",
            "888888",
            "password",
            "password1",
            "password123",
            "admin",
            "admin123",
            "administrator",
            "qwerty",
            "abc123",
            "welcome",
            "letmein",
            "iloveyou"
        };

        /// <summary>
        /// 中文：配置运行期密码策略选项提供器。
        ///
        /// English: Configures the runtime password-policy options provider.
        /// </summary>
        /// <remarks>
        /// 中文：该入口用于避免组件层反向依赖 Web 配置读取器。提供器异常时会安全回退到组件默认策略；
        /// 调用方不得在提供器中记录或返回任何密码明文。
        ///
        /// English: This entry avoids a reverse dependency from the component layer to the Web configuration
        /// resolver. Provider failures safely fall back to the component default policy; callers must not log or
        /// return any plain password values from the provider.
        /// </remarks>
        /// <param name="provider">中文：返回当前有效策略选项的委托；传入 <c>null</c> 会恢复默认策略。English: Delegate returning current effective policy options; <c>null</c> restores the default policy.</param>
        public static void ConfigureOptionsProvider(Func<PortalPasswordPolicyOptions> provider)
        {
            lock (OptionsProviderLock)
            {
                optionsProvider = provider;
            }
        }

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
            return TryValidate(password, null, out message);
        }

        /// <summary>
        /// 中文：校验密码是否满足当前复杂度策略，并可按账号上下文词阻止弱相关密码。
        ///
        /// English: Validates whether a password satisfies the current complexity policy and can reject weak
        /// account-context-related passwords.
        /// </summary>
        /// <param name="password">中文：一次性提交的密码输入；调用方不得记录。English: One-time submitted password input; callers must not log it.</param>
        /// <param name="contextTerms">中文：可选的用户名、邮箱、员工号、显示名等上下文词；调用方不得记录。English: Optional user name, email, employee code, display name, and similar context terms; callers must not log them.</param>
        /// <param name="message">中文：可显示给用户的失败说明。English: Display-safe failure message.</param>
        /// <returns>中文：满足策略时为 <c>true</c>。English: <c>true</c> when the policy is satisfied.</returns>
        public static bool TryValidate(string password, IEnumerable<string> contextTerms, out string message)
        {
            PortalPasswordPolicyOptions options = GetEffectiveOptions();
            if (password == null || password.Length < options.MinimumLength)
            {
                message = BuildDisplayMessage(options);
                return false;
            }

            int categoryCount = CountCategories(password);
            if (categoryCount < options.RequiredCategoryCount)
            {
                message = BuildDisplayMessage(options);
                return false;
            }

            if (options.WeakDictionaryEnabled && IsWeakPassword(password))
            {
                message = "密码过于常见或容易猜测，请更换为不在弱口令字典中的密码。";
                return false;
            }

            if (options.DisallowContextTerms && ContainsContextTerm(password, contextTerms))
            {
                message = "密码不能包含用户名、邮箱、员工号、显示名等账号相关信息。";
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
            return BuildDisplayMessage(GetEffectiveOptions());
        }

        /// <summary>
        /// 中文：获取当前有效密码策略选项。
        ///
        /// English: Gets the current effective password-policy options.
        /// </summary>
        /// <returns>中文：已按硬下限规范化的策略选项。English: Policy options normalized against hard lower bounds.</returns>
        public static PortalPasswordPolicyOptions GetEffectiveOptions()
        {
            Func<PortalPasswordPolicyOptions> provider;
            lock (OptionsProviderLock)
            {
                provider = optionsProvider;
            }

            if (provider == null)
            {
                return PortalPasswordPolicyOptions.CreateDefault();
            }

            try
            {
                return NormalizeOptions(provider());
            }
            catch (Exception)
            {
                return PortalPasswordPolicyOptions.CreateDefault();
            }
        }

        private static string BuildDisplayMessage(PortalPasswordPolicyOptions options)
        {
            return string.Format(
                "密码至少 {0} 位，并且需要在大写字母、小写字母、数字、特殊字符中至少包含 {1} 类。",
                options.MinimumLength,
                options.RequiredCategoryCount);
        }

        private static PortalPasswordPolicyOptions NormalizeOptions(PortalPasswordPolicyOptions options)
        {
            if (options == null)
            {
                return PortalPasswordPolicyOptions.CreateDefault();
            }

            return new PortalPasswordPolicyOptions(
                Math.Max(MinimumLength, options.MinimumLength),
                Math.Max(RequiredCategoryCount, Math.Min(4, options.RequiredCategoryCount)),
                options.WeakDictionaryEnabled,
                options.DisallowContextTerms);
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

        private static bool IsWeakPassword(string password)
        {
            string normalizedPassword = NormalizeToken(password);
            foreach (string weakPassword in WeakPasswordDictionary)
            {
                if (string.Equals(normalizedPassword, weakPassword, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return IsSingleRepeatedCharacter(normalizedPassword);
        }

        private static bool ContainsContextTerm(string password, IEnumerable<string> contextTerms)
        {
            if (contextTerms == null)
            {
                return false;
            }

            string normalizedPassword = NormalizeToken(password);
            if (normalizedPassword.Length == 0)
            {
                return false;
            }

            foreach (string contextTerm in contextTerms)
            {
                foreach (string token in SplitContextTokens(contextTerm))
                {
                    if (token.Length >= 4 && normalizedPassword.IndexOf(token, StringComparison.Ordinal) >= 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static IEnumerable<string> SplitContextTokens(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                yield break;
            }

            int tokenStart = -1;
            for (int index = 0; index < value.Length; index++)
            {
                if (char.IsLetterOrDigit(value[index]))
                {
                    if (tokenStart < 0)
                    {
                        tokenStart = index;
                    }
                }
                else if (tokenStart >= 0)
                {
                    yield return NormalizeToken(value.Substring(tokenStart, index - tokenStart));
                    tokenStart = -1;
                }
            }

            if (tokenStart >= 0)
            {
                yield return NormalizeToken(value.Substring(tokenStart));
            }
        }

        private static bool IsSingleRepeatedCharacter(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (int index = 1; index < value.Length; index++)
            {
                if (value[index] != value[0])
                {
                    return false;
                }
            }

            return true;
        }

        private static string NormalizeToken(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }
    }

    /// <summary>
    /// 中文：密码策略运行期选项。
    ///
    /// English: Runtime options for the password policy.
    /// </summary>
    /// <remarks>
    /// 中文：该类型只保存策略参数，不保存用户密码。Web 层可以从系统设置解析这些值后传入
    /// <see cref="PortalPasswordPolicy.ConfigureOptionsProvider"/>。
    ///
    /// English: This type stores policy parameters only, not user passwords. The Web layer can resolve these
    /// values from system settings and pass them into <see cref="PortalPasswordPolicy.ConfigureOptionsProvider"/>.
    /// </remarks>
    public sealed class PortalPasswordPolicyOptions
    {
        /// <summary>
        /// 中文：创建密码策略运行期选项。
        ///
        /// English: Creates runtime options for the password policy.
        /// </summary>
        /// <param name="minimumLength">中文：最小长度。English: Minimum length.</param>
        /// <param name="requiredCategoryCount">中文：必须满足的字符类别数。English: Required character-category count.</param>
        /// <param name="weakDictionaryEnabled">中文：是否启用常见弱口令字典。English: Whether the common weak-password dictionary is enabled.</param>
        /// <param name="disallowContextTerms">中文：是否禁止包含账号上下文词。English: Whether account-context terms are disallowed.</param>
        public PortalPasswordPolicyOptions(
            int minimumLength,
            int requiredCategoryCount,
            bool weakDictionaryEnabled,
            bool disallowContextTerms)
        {
            MinimumLength = minimumLength;
            RequiredCategoryCount = requiredCategoryCount;
            WeakDictionaryEnabled = weakDictionaryEnabled;
            DisallowContextTerms = disallowContextTerms;
        }

        /// <summary>
        /// 中文：创建组件层默认策略选项。
        ///
        /// English: Creates the component-layer default policy options.
        /// </summary>
        /// <returns>中文：默认策略选项。English: Default policy options.</returns>
        public static PortalPasswordPolicyOptions CreateDefault()
        {
            return new PortalPasswordPolicyOptions(
                PortalPasswordPolicy.MinimumLength,
                PortalPasswordPolicy.RequiredCategoryCount,
                true,
                true);
        }

        /// <summary>
        /// 中文：最小密码长度。
        ///
        /// English: Minimum password length.
        /// </summary>
        public int MinimumLength { get; private set; }

        /// <summary>
        /// 中文：必须满足的字符类别数量。
        ///
        /// English: Required number of character categories.
        /// </summary>
        public int RequiredCategoryCount { get; private set; }

        /// <summary>
        /// 中文：是否启用常见弱口令字典。
        ///
        /// English: Whether the common weak-password dictionary is enabled.
        /// </summary>
        public bool WeakDictionaryEnabled { get; private set; }

        /// <summary>
        /// 中文：是否禁止密码包含账号上下文词。
        ///
        /// English: Whether passwords may contain account-context terms.
        /// </summary>
        public bool DisallowContextTerms { get; private set; }
    }
}
