using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户密码复杂度策略，用于企业用户生命周期的注册、建号和重置密码入口。</zh-CN>
    ///   <en>Portal password-complexity policy for the enterprise user lifecycle registration, account-creation, and password-reset entry points.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>组件层保留 8 位/3 类作为硬下限；Web 层可在启动时注入运行期策略选项，让系统管理设置 能控制更高强度的最小长度、类别数量、弱口令字典和账号上下文词限制。此类型不处理历史密码、 失败锁定或客户端加密协议。</zh-CN>
    ///   <en>The component layer keeps 8 characters / 3 categories as the hard lower bound. The Web layer may inject runtime policy options on startup so system settings can control stronger minimum length, category count, weak-password dictionary checks, and account-context-word restrictions. This type does not handle password history, failure lockout, or client-side encryption protocols.</en>
    /// </lang>
    /// </remarks>
    public static class PortalPasswordPolicy
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>当前基线策略要求的最小长度。</zh-CN>
        ///   <en>Minimum length required by the current baseline policy.</en>
        /// </lang>
        /// </summary>
        public const int MinimumLength = 8;

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前基线策略要求满足的字符类别数量。</zh-CN>
        ///   <en>Number of character categories required by the current baseline policy.</en>
        /// </lang>
        /// </summary>
        public const int RequiredCategoryCount = 3;

        /// <summary>
        /// <lang>
        ///   <zh-CN>保护运行期策略提供器引用的同步锁，避免启动配置与并发验证之间读取到部分状态。</zh-CN>
        ///   <en>Synchronization lock protecting the runtime policy-provider reference so startup configuration and concurrent validation cannot observe partial state.</en>
        /// </lang>
        /// </summary>
        private static readonly object OptionsProviderLock = new object();

        /// <summary>
        /// <lang>
        ///   <zh-CN>由 Web 启动期注入的运行期策略选项提供器；该委托不得记录、缓存或返回密码明文。</zh-CN>
        ///   <en>Runtime policy-options provider injected during Web startup; this delegate must not log, cache, or return plain passwords.</en>
        /// </lang>
        /// </summary>
        private static Func<PortalPasswordPolicyOptions> optionsProvider;

        /// <summary>
        /// <lang>
        ///   <zh-CN>受控常见弱口令字典，仅含公开低熵样例，不包含系统账户或用户密码。</zh-CN>
        ///   <en>Controlled common weak-password dictionary containing public low-entropy examples only, never system accounts or user passwords.</en>
        /// </lang>
        /// </summary>
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
        /// <lang>
        ///   <zh-CN>配置运行期密码策略选项提供器。</zh-CN>
        ///   <en>Configures the runtime password-policy options provider.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该入口用于避免组件层反向依赖 Web 配置读取器。提供器异常时会安全回退到组件默认策略； 调用方不得在提供器中记录或返回任何密码明文。</zh-CN>
        ///   <en>This entry avoids a reverse dependency from the component layer to the Web configuration resolver. Provider failures safely fall back to the component default policy; callers must not log or return any plain password values from the provider.</en>
        /// </lang>
        /// </remarks>
        /// <param name="provider">
        /// <l>
        ///   <zh-CN>返回当前有效策略选项的委托；传入 <c>null</c> 会恢复默认策略。</zh-CN>
        ///   <en>Delegate returning current effective policy options; <c>null</c> restores the default policy.</en>
        /// </l>
        /// </param>
        public static void ConfigureOptionsProvider(Func<PortalPasswordPolicyOptions> provider)
        {
            // <lang>
            //   <zh-CN>在同步边界内原子替换提供器；传入 null 明确恢复组件层默认策略。</zh-CN>
            //   <en>Replace the provider atomically within the synchronization boundary; null explicitly restores the component-layer default policy.</en>
            // </lang>
            lock (OptionsProviderLock)
            {
                optionsProvider = provider;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>校验密码是否满足当前复杂度基线策略。</zh-CN>
        ///   <en>Validates whether a password satisfies the current baseline complexity policy.</en>
        /// </lang>
        /// </summary>
        /// <param name="password">
        /// <l>
        ///   <zh-CN>一次性提交的密码输入；调用方不得记录。</zh-CN>
        ///   <en>One-time submitted password input; callers must not log it.</en>
        /// </l>
        /// </param>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>可显示给用户的失败说明。</zh-CN>
        ///   <en>Display-safe failure message.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>满足策略时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the policy is satisfied.</en>
        /// </l>
        /// </returns>
        public static bool TryValidate(string password, out string message)
        {
            // <lang>
            //   <zh-CN>将无上下文场景委托给完整入口，保持所有长度、类别和字典判断使用同一实现。</zh-CN>
            //   <en>Delegate the no-context scenario to the complete entry point so length, category, and dictionary checks use one implementation.</en>
            // </lang>
            return TryValidate(password, null, out message);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>校验密码是否满足当前复杂度策略，并可按账号上下文词阻止弱相关密码。</zh-CN>
        ///   <en>Validates whether a password satisfies the current complexity policy and can reject weak account-context-related passwords.</en>
        /// </lang>
        /// </summary>
        /// <param name="password">
        /// <l>
        ///   <zh-CN>一次性提交的密码输入；调用方不得记录。</zh-CN>
        ///   <en>One-time submitted password input; callers must not log it.</en>
        /// </l>
        /// </param>
        /// <param name="contextTerms">
        /// <l>
        ///   <zh-CN>可选的用户名、邮箱、员工号、显示名等上下文词；调用方不得记录。</zh-CN>
        ///   <en>Optional user name, email, employee code, display name, and similar context terms; callers must not log them.</en>
        /// </l>
        /// </param>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>可显示给用户的失败说明。</zh-CN>
        ///   <en>Display-safe failure message.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>满足策略时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the policy is satisfied.</en>
        /// </l>
        /// </returns>
        public static bool TryValidate(string password, IEnumerable<string> contextTerms, out string message)
        {
            // <lang>
            //   <zh-CN>读取已按硬下限规范化的当前策略；该对象只保存参数，不包含密码或上下文原文。</zh-CN>
            //   <en>Read the current policy normalized against hard lower bounds; this object holds parameters only, never password or raw context text.</en>
            // </lang>
            PortalPasswordPolicyOptions options = GetEffectiveOptions();
            if (password == null || password.Length < options.MinimumLength)
            {
                message = BuildDisplayMessage(options);
                return false;
            }

            // <lang>
            //   <zh-CN>统计密码实际覆盖的字符类别，结果只用于本次验证且不写入诊断。</zh-CN>
            //   <en>Count character categories actually covered by the password; the result is used only for this validation and never written to diagnostics.</en>
            // </lang>
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
        /// <lang>
        ///   <zh-CN>返回当前策略的可展示说明。</zh-CN>
        ///   <en>Returns a display-safe description of the current policy.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>策略说明。</zh-CN>
        ///   <en>Policy description.</en>
        /// </l>
        /// </returns>
        public static string BuildDisplayMessage()
        {
            // <lang>
            //   <zh-CN>按当前有效策略生成固定格式展示提示，不包含导致失败的密码或账号上下文。</zh-CN>
            //   <en>Build a fixed-format display prompt from the current effective policy without including the failed password or account context.</en>
            // </lang>
            return BuildDisplayMessage(GetEffectiveOptions());
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取当前有效密码策略选项。</zh-CN>
        ///   <en>Gets the current effective password-policy options.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>已按硬下限规范化的策略选项。</zh-CN>
        ///   <en>Policy options normalized against hard lower bounds.</en>
        /// </l>
        /// </returns>
        public static PortalPasswordPolicyOptions GetEffectiveOptions()
        {
            // <lang>
            //   <zh-CN>复制当前提供器引用，缩短锁持有时间并让外部提供器在锁外执行。</zh-CN>
            //   <en>Copy the current provider reference to shorten lock hold time and execute external providers outside the lock.</en>
            // </lang>
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
                // <lang>
                //   <zh-CN>调用提供器后立即规范化结果，防止配置把组件硬下限降低到不安全值。</zh-CN>
                //   <en>Normalize the provider result immediately, preventing configuration from lowering component hard bounds to unsafe values.</en>
                // </lang>
                return NormalizeOptions(provider());
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>提供器故障按 fail-safe 回退默认策略，且不记录可能包含敏感配置上下文的异常。</zh-CN>
                //   <en>Fail safely to the default policy when the provider faults, without logging an exception that could contain sensitive configuration context.</en>
                // </lang>
                return PortalPasswordPolicyOptions.CreateDefault();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>根据已规范化策略生成不回显密码的固定展示说明。</zh-CN>
        ///   <en>Builds a fixed display description from normalized policy without echoing a password.</en>
        /// </lang>
        /// </summary>
        private static string BuildDisplayMessage(PortalPasswordPolicyOptions options)
        {
            return string.Format(
                "密码至少 {0} 位，并且需要在大写字母、小写字母、数字、特殊字符中至少包含 {1} 类。",
                options.MinimumLength,
                options.RequiredCategoryCount);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将运行期策略限制在组件硬下限和四类字符的可表达范围内。</zh-CN>
        ///   <en>Constrains runtime policy to the component hard lower bounds and the expressible range of four character categories.</en>
        /// </lang>
        /// </summary>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>统计密码覆盖的大写、小写、数字和特殊字符类别数，不持久化输入。</zh-CN>
        ///   <en>Counts uppercase, lowercase, digit, and special categories covered by a password without persisting input.</en>
        /// </lang>
        /// </summary>
        private static int CountCategories(string password)
        {
            // <lang>
            //   <zh-CN>记录是否已出现大写字母；布尔状态只在本次迭代中存活。</zh-CN>
            //   <en>Record whether an uppercase letter appeared; this Boolean state lives only for the current iteration.</en>
            // </lang>
            bool hasUpper = false;

            // <lang>
            //   <zh-CN>记录是否已出现小写字母；不保存具体字符。</zh-CN>
            //   <en>Record whether a lowercase letter appeared without retaining the actual character.</en>
            // </lang>
            bool hasLower = false;

            // <lang>
            //   <zh-CN>记录是否已出现数字；不保存具体字符。</zh-CN>
            //   <en>Record whether a digit appeared without retaining the actual character.</en>
            // </lang>
            bool hasDigit = false;

            // <lang>
            //   <zh-CN>记录是否已出现其余特殊字符；类别判断不对密码文本产生副作用。</zh-CN>
            //   <en>Record whether any remaining special character appeared; categorization has no side effect on password text.</en>
            // </lang>
            bool hasSpecial = false;

            // <lang>
            //   <zh-CN>逐字符分类一次性输入，只累积类别布尔值，不输出、缓存或记录字符。</zh-CN>
            //   <en>Classify the one-time input character by character, accumulating Boolean categories only and never outputting, caching, or logging characters.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断规范化密码是否命中受控弱口令字典或单字符重复模式。</zh-CN>
        ///   <en>Determines whether a normalized password matches the controlled weak dictionary or a single-character repetition pattern.</en>
        /// </lang>
        /// </summary>
        private static bool IsWeakPassword(string password)
        {
            // <lang>
            //   <zh-CN>将一次性输入规范化为比较令牌；令牌只在当前调用中存活且不写入诊断。</zh-CN>
            //   <en>Normalize the one-time input into a comparison token that lives only for this call and is never written to diagnostics.</en>
            // </lang>
            string normalizedPassword = NormalizeToken(password);

            // <lang>
            //   <zh-CN>逐项比较公开弱口令样例，使用 ordinal 避免区域性规则改变安全判断。</zh-CN>
            //   <en>Compare public weak-password examples one by one using ordinal comparison so locale rules cannot change the security decision.</en>
            // </lang>
            foreach (string weakPassword in WeakPasswordDictionary)
            {
                if (string.Equals(normalizedPassword, weakPassword, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return IsSingleRepeatedCharacter(normalizedPassword);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断密码是否包含长度足够的账号上下文令牌，避免用户名、邮箱或员工号相关弱密码。</zh-CN>
        ///   <en>Determines whether a password contains an account-context token of sufficient length, avoiding weak passwords related to user names, email, or employee codes.</en>
        /// </lang>
        /// </summary>
        private static bool ContainsContextTerm(string password, IEnumerable<string> contextTerms)
        {
            if (contextTerms == null)
            {
                return false;
            }

            // <lang>
            //   <zh-CN>规范化密码用于 ordinal 子串比较；空令牌不可能命中上下文词。</zh-CN>
            //   <en>Normalize the password for ordinal substring comparison; an empty token cannot match a context term.</en>
            // </lang>
            string normalizedPassword = NormalizeToken(password);
            if (normalizedPassword.Length == 0)
            {
                return false;
            }

            // <lang>
            //   <zh-CN>逐个读取调用方提供的上下文文本；每项仅即时拆分，绝不持久化或记录。</zh-CN>
            //   <en>Read caller-provided context text one item at a time; each item is split only in place and never persisted or logged.</en>
            // </lang>
            foreach (string contextTerm in contextTerms)
            {
                // <lang>
                //   <zh-CN>按字母数字边界生成可比较令牌，过滤掉短片段以减少误拒绝。</zh-CN>
                //   <en>Generate comparable tokens by alphanumeric boundaries and filter short fragments to reduce false rejections.</en>
                // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>按非字母数字边界拆分上下文文本，并返回规范化比较令牌。</zh-CN>
        ///   <en>Splits context text at non-alphanumeric boundaries and returns normalized comparison tokens.</en>
        /// </lang>
        /// </summary>
        private static IEnumerable<string> SplitContextTokens(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                yield break;
            }

            // <lang>
            //   <zh-CN>记录当前字母数字令牌起点；-1 表示当前不在令牌内部。</zh-CN>
            //   <en>Track the current alphanumeric token start; -1 means no token is currently open.</en>
            // </lang>
            int tokenStart = -1;

            // <lang>
            //   <zh-CN>按原始上下文字符扫描边界，只产生规范化片段，不返回原始上下文文本。</zh-CN>
            //   <en>Scan boundaries across raw context characters, producing normalized fragments only and never returning raw context text.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>识别由单一重复字符组成的低熵令牌。</zh-CN>
        ///   <en>Identifies a low-entropy token made of one repeated character.</en>
        /// </lang>
        /// </summary>
        private static bool IsSingleRepeatedCharacter(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            // <lang>
            //   <zh-CN>从第二个字符起与首字符比较；发现差异即可确定不是单字符重复模式。</zh-CN>
            //   <en>Compare from the second character against the first; any difference proves the token is not a single-character repetition pattern.</en>
            // </lang>
            for (int index = 1; index < value.Length; index++)
            {
                if (value[index] != value[0])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将密码或上下文片段转换为去空白、invariant 小写的短生命周期比较令牌。</zh-CN>
        ///   <en>Converts a password or context fragment into a trimmed invariant-lowercase short-lived comparison token.</en>
        /// </lang>
        /// </summary>
        private static string NormalizeToken(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>密码策略运行期选项。</zh-CN>
    ///   <en>Runtime options for the password policy.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该类型只保存策略参数，不保存用户密码。Web 层可以从系统设置解析这些值后传入 <see cref="PortalPasswordPolicy.ConfigureOptionsProvider"/>。</zh-CN>
    ///   <en>This type stores policy parameters only, not user passwords. The Web layer can resolve these values from system settings and pass them into <see cref="PortalPasswordPolicy.ConfigureOptionsProvider"/>.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalPasswordPolicyOptions
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建密码策略运行期选项。</zh-CN>
        ///   <en>Creates runtime options for the password policy.</en>
        /// </lang>
        /// </summary>
        /// <param name="minimumLength">
        /// <l>
        ///   <zh-CN>最小长度。</zh-CN>
        ///   <en>Minimum length.</en>
        /// </l>
        /// </param>
        /// <param name="requiredCategoryCount">
        /// <l>
        ///   <zh-CN>必须满足的字符类别数。</zh-CN>
        ///   <en>Required character-category count.</en>
        /// </l>
        /// </param>
        /// <param name="weakDictionaryEnabled">
        /// <l>
        ///   <zh-CN>是否启用常见弱口令字典。</zh-CN>
        ///   <en>Whether the common weak-password dictionary is enabled.</en>
        /// </l>
        /// </param>
        /// <param name="disallowContextTerms">
        /// <l>
        ///   <zh-CN>是否禁止包含账号上下文词。</zh-CN>
        ///   <en>Whether account-context terms are disallowed.</en>
        /// </l>
        /// </param>
        public PortalPasswordPolicyOptions(
            int minimumLength,
            int requiredCategoryCount,
            bool weakDictionaryEnabled,
            bool disallowContextTerms)
        {
            // <lang>
            //   <zh-CN>保存请求的最小长度；实际使用前仍由策略类施加硬下限。</zh-CN>
            //   <en>Store the requested minimum length; the policy class still applies hard lower bounds before use.</en>
            // </lang>
            MinimumLength = minimumLength;

            // <lang>
            //   <zh-CN>保存请求的类别数量；实际使用前仍限制在四类可表达范围内。</zh-CN>
            //   <en>Store the requested category count; actual use remains limited to the expressible range of four categories.</en>
            // </lang>
            RequiredCategoryCount = requiredCategoryCount;

            // <lang>
            //   <zh-CN>保存弱口令字典开关；该开关不携带任何用户密码。</zh-CN>
            //   <en>Store the weak-dictionary switch; it carries no user password.</en>
            // </lang>
            WeakDictionaryEnabled = weakDictionaryEnabled;

            // <lang>
            //   <zh-CN>保存账号上下文词限制开关；调用方仍负责不记录上下文文本。</zh-CN>
            //   <en>Store the account-context-term restriction switch; callers remain responsible for not logging context text.</en>
            // </lang>
            DisallowContextTerms = disallowContextTerms;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建组件层默认策略选项。</zh-CN>
        ///   <en>Creates the component-layer default policy options.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>默认策略选项。</zh-CN>
        ///   <en>Default policy options.</en>
        /// </l>
        /// </returns>
        public static PortalPasswordPolicyOptions CreateDefault()
        {
            return new PortalPasswordPolicyOptions(
                PortalPasswordPolicy.MinimumLength,
                PortalPasswordPolicy.RequiredCategoryCount,
                true,
                true);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最小密码长度。</zh-CN>
        ///   <en>Minimum password length.</en>
        /// </lang>
        /// </summary>
        public int MinimumLength { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>必须满足的字符类别数量。</zh-CN>
        ///   <en>Required number of character categories.</en>
        /// </lang>
        /// </summary>
        public int RequiredCategoryCount { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否启用常见弱口令字典。</zh-CN>
        ///   <en>Whether the common weak-password dictionary is enabled.</en>
        /// </lang>
        /// </summary>
        public bool WeakDictionaryEnabled { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否禁止密码包含账号上下文词。</zh-CN>
        ///   <en>Whether passwords may contain account-context terms.</en>
        /// </lang>
        /// </summary>
        public bool DisallowContextTerms { get; private set; }
    }
}
