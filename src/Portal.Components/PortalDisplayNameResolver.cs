using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>按界面文化解析数据库承载的显示名（中文列 / 英文列 / 其它语言 JSON 列）。</zh-CN>
    ///   <en>Resolves database-carried display names (Chinese column / English column / other-languages JSON column) by UI culture.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>存在理由：门户壳层与模块正文走 `App_GlobalResources/lang.*` 资源，而页签名（`PortalCfg_Tabs.TabName`）与
    ///   模块标题（`PortalCfg_Modules.ModuleTitle`）是数据库单列文本，**不随语言切换**，于是同一张卡片里出现
    ///   "英文标题 + 中文正文"的中英混排。本解析器把这两类文本接入与资源同源的文化判定，混排才有解。
    ///
    ///   列设计（用户 2026-10-18 裁定）：中文一列、英文一列、其它所有语言合并为一个 JSON 列。
    ///   中文列复用既有列（`TabName` / `ModuleTitle`）而不是新增列 —— 既有列 `NOT NULL` 且是所有渲染点的默认来源，
    ///   复用它才能保证"永远有值可显示"。
    ///
    ///   解析顺序：
    ///     1. 两字母语言码 `zh`（含 zh-CN / zh-TW 等区域变体）→ 中文列，缺失回落英文列；
    ///     2. 两字母语言码 `en` → 英文列，缺失回落中文列；
    ///     3. 其它文化 → JSON 列按"完整文化名 → 父文化名 → 两字母码"三级匹配；
    ///     4. 三级都未命中 → 回落中文列 → 回落英文列 → 空字符串。
    ///   该回退链与既有 `PortalNavigationEntry.GetDisplayName` 的语义一致（中文文化取中文、英文文化取英文、
    ///   一侧缺失回落另一侧），避免两套显示名解析逻辑对同一文化给出不同结论。
    ///
    ///   证明边界：本类型只证明"按文化选出最合适的显示文本"，**不证明**文本本身翻译正确、
    ///   JSON 键是受支持的文化（键名白名单属于语言管理功能），也不做任何授权判断。
    ///   </zh-CN>
    ///   <en>Reason it exists: the portal shell and module body text come from `App_GlobalResources/lang.*` resources, while tab
    ///   names (`PortalCfg_Tabs.TabName`) and module titles (`PortalCfg_Modules.ModuleTitle`) are single-column database text that
    ///   **does not follow the language switch**, producing "English title + Chinese body" inside one card. This resolver connects
    ///   those texts to the same culture decision the resources use, which is what makes the mixing fixable.
    ///
    ///   Column design (user decision, 2026-10-18): one Chinese column, one English column, and one JSON column for every other
    ///   language. The Chinese side reuses the existing columns (`TabName` / `ModuleTitle`) rather than adding new ones — those
    ///   columns are `NOT NULL` and are the default source of every renderer, so reusing them guarantees there is always
    ///   something to display.
    ///
    ///   Resolution order:
    ///     1. two-letter language code `zh` (including zh-CN / zh-TW and other regional variants) → Chinese column, falling back
    ///        to the English column when missing;
    ///     2. two-letter code `en` → English column, falling back to the Chinese column when missing;
    ///     3. any other culture → three-tier JSON lookup "full culture name → parent culture name → two-letter code";
    ///     4. no hit → Chinese column → English column → empty string.
    ///   That fallback chain matches the existing `PortalNavigationEntry.GetDisplayName` semantics (Chinese cultures take the
    ///   Chinese name, English cultures the English one, and a missing side falls back to the other), so two display-name
    ///   resolvers cannot disagree about the same culture.
    ///
    ///   Proof boundary: this type proves only that "the most suitable display text was selected for the culture". It does
    ///   **not** prove the text is a correct translation, that JSON keys are supported cultures (that allowlist belongs to
    ///   language administration), and it makes no authorization decision.
    ///   </en>
    /// </lang>
    /// </remarks>
    public static class PortalDisplayNameResolver
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>JSON 列无法解析时写入诊断原因的稳定前缀；只进入诊断通道，不成为可见文案。</zh-CN>
        ///   <en>Stable prefix for the diagnostic reason written when the JSON column cannot be parsed; it only enters the
        ///   diagnostic channel and never becomes visible copy.</en>
        /// </lang>
        /// </summary>
        public const string JsonInvalidReasonPrefix = "DisplayName.JsonInvalid:";

        // <lang>
        //   <zh-CN>中文与英文的两字母语言码；判定用两字母码而非完整文化名，使 zh-TW / en-GB 等区域变体落在同一语言侧。</zh-CN>
        //   <en>Two-letter language codes for Chinese and English; the decision uses the two-letter code rather than the full
        //   culture name so regional variants such as zh-TW / en-GB land on the same language side.</en>
        // </lang>
        private const string ChineseLanguageCode = "zh";
        private const string EnglishLanguageCode = "en";

        /// <summary>
        /// <lang>
        ///   <zh-CN>按当前线程界面文化解析显示名；需要诊断原因时使用带 `diagnostic` 的重载。</zh-CN>
        ///   <en>Resolves the display name using the current thread UI culture; use the overload with `diagnostic` when the
        ///   reason is needed.</en>
        /// </lang>
        /// </summary>
        /// <param name="chineseText">
        /// <l>
        ///   <zh-CN>中文列兼默认回退列的值。</zh-CN>
        ///   <en>Value of the Chinese column, which is also the default fallback.</en>
        /// </l>
        /// </param>
        /// <param name="englishText">
        /// <l>
        ///   <zh-CN>英文列的值。</zh-CN>
        ///   <en>Value of the English column.</en>
        /// </l>
        /// </param>
        /// <param name="localizedJson">
        /// <l>
        ///   <zh-CN>其它语言列；以文化名为键的 JSON 对象文本，可为空。</zh-CN>
        ///   <en>Other-languages column: culture-keyed JSON object text, may be empty.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>可直接显示的文本；全部候选为空时返回空字符串。</zh-CN>
        ///   <en>Text ready to display; an empty string when every candidate is blank.</en>
        /// </l>
        /// </returns>
        public static string Resolve(string chineseText, string englishText, string localizedJson)
        {
            string unusedDiagnostic;
            return Resolve(chineseText, englishText, localizedJson, null, out unusedDiagnostic);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按指定界面文化解析显示名，并通过 `diagnostic` 输出降级原因。</zh-CN>
        ///   <en>Resolves the display name for the supplied UI culture and reports the fallback reason through `diagnostic`.</en>
        /// </lang>
        /// </summary>
        /// <param name="chineseText">
        /// <l>
        ///   <zh-CN>中文列兼默认回退列的值。</zh-CN>
        ///   <en>Value of the Chinese column, which is also the default fallback.</en>
        /// </l>
        /// </param>
        /// <param name="englishText">
        /// <l>
        ///   <zh-CN>英文列的值。</zh-CN>
        ///   <en>Value of the English column.</en>
        /// </l>
        /// </param>
        /// <param name="localizedJson">
        /// <l>
        ///   <zh-CN>其它语言列；以文化名为键的 JSON 对象文本，可为空。</zh-CN>
        ///   <en>Other-languages column: culture-keyed JSON object text, may be empty.</en>
        /// </l>
        /// </param>
        /// <param name="culture">
        /// <l>
        ///   <zh-CN>目标界面文化；为空时使用当前线程界面文化。</zh-CN>
        ///   <en>Target UI culture; when null the current thread UI culture is used.</en>
        /// </l>
        /// </param>
        /// <param name="diagnostic">
        /// <l>
        ///   <zh-CN>降级原因；正常命中或无其它语言可查时为空字符串。调用方可把它写入诊断日志，不应直接显示给用户。</zh-CN>
        ///   <en>Fallback reason; an empty string on a normal hit or when no other languages exist. Callers may write it to a
        ///   diagnostic log but should not show it to users.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>可直接显示的文本；全部候选为空时返回空字符串。</zh-CN>
        ///   <en>Text ready to display; an empty string when every candidate is blank.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>本方法**不抛异常**：JSON 列损坏、文化缺失或候选全空都走回退链。理由是它处在门户页头与模块标题的
        ///   渲染路径上 —— 一条脏数据就让整页 500，比"显示回退文本"糟糕得多；是否报警由调用方凭 `diagnostic` 决定。</zh-CN>
        ///   <en>This method **never throws**: a damaged JSON column, a missing culture, or all-blank candidates all follow the
        ///   fallback chain. It sits on the rendering path of the portal header and module titles, so one dirty row turning the
        ///   whole page into a 500 is far worse than showing fallback text; whether to raise an alarm is the caller's decision
        ///   based on `diagnostic`.</en>
        /// </lang>
        /// </remarks>
        public static string Resolve(
            string chineseText,
            string englishText,
            string localizedJson,
            CultureInfo culture,
            out string diagnostic)
        {
            diagnostic = string.Empty;

            // <lang>
            //   <zh-CN>未显式传入文化时取当前线程界面文化；两者都缺失时按中文处理，与门户默认语言一致。</zh-CN>
            //   <en>Use the current thread UI culture when none is supplied; if both are missing, treat it as Chinese, matching the portal default language.</en>
            // </lang>
            CultureInfo effectiveCulture = culture ?? CultureInfo.CurrentUICulture;
            string languageCode = effectiveCulture == null
                ? ChineseLanguageCode
                : effectiveCulture.TwoLetterISOLanguageName;

            // <lang>
            //   <zh-CN>中文侧：命中中文列，缺失时回落英文列（不因缺中文就显示空标签）。</zh-CN>
            //   <en>Chinese side: take the Chinese column and fall back to the English one when missing, so a missing Chinese value never renders an empty label.</en>
            // </lang>
            if (string.Equals(languageCode, ChineseLanguageCode, StringComparison.OrdinalIgnoreCase))
            {
                return FirstNonBlank(chineseText, englishText);
            }

            // <lang>
            //   <zh-CN>英文侧：命中英文列，缺失时回落中文列。</zh-CN>
            //   <en>English side: take the English column and fall back to the Chinese one when missing.</en>
            // </lang>
            if (string.Equals(languageCode, EnglishLanguageCode, StringComparison.OrdinalIgnoreCase))
            {
                return FirstNonBlank(englishText, chineseText);
            }

            // <lang>
            //   <zh-CN>其它文化：先在 JSON 列里按三级键匹配，未命中再回落到中英两列。</zh-CN>
            //   <en>Other cultures: first try the three-tier key lookup in the JSON column, then fall back to the Chinese and English columns.</en>
            // </lang>
            string jsonValue;
            string jsonError;
            if (TryGetJsonValue(localizedJson, effectiveCulture, out jsonValue, out jsonError))
            {
                return jsonValue;
            }

            if (!string.IsNullOrEmpty(jsonError))
            {
                diagnostic = JsonInvalidReasonPrefix + jsonError;
            }

            return FirstNonBlank(chineseText, englishText);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从其它语言 JSON 列中按"完整文化名 → 父文化名 → 两字母码"三级取回该文化的文本。</zh-CN>
        ///   <en>Retrieves the text for a culture from the other-languages JSON column using the three-tier lookup
        ///   "full culture name → parent culture name → two-letter code".</en>
        /// </lang>
        /// </summary>
        /// <param name="localizedJson">
        /// <l>
        ///   <zh-CN>以文化名为键的 JSON 对象文本；空白视为"没有其它语言"，不算错误。</zh-CN>
        ///   <en>Culture-keyed JSON object text; blank means "no other languages" and is not an error.</en>
        /// </l>
        /// </param>
        /// <param name="culture">
        /// <l>
        ///   <zh-CN>目标界面文化。</zh-CN>
        ///   <en>Target UI culture.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>命中时返回该文化的文本；未命中返回 `null`。</zh-CN>
        ///   <en>Text for the culture when found; `null` when not found.</en>
        /// </l>
        /// </param>
        /// <param name="error">
        /// <l>
        ///   <zh-CN>JSON 无法解析时返回原因摘要；内容取自异常消息的类型层面信息，不含整段原始数据。</zh-CN>
        ///   <en>Reason summary when the JSON cannot be parsed; it carries type-level information from the exception and never the whole raw value.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>命中且文本非空时为 true。</zh-CN>
        ///   <en>True when a hit occurred and the text is non-blank.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>解析失败一律走"未命中 + 原因"，绝不抛出：JSON 列由管理员手填，允许出现数组、嵌套或键名拼错，
        ///   这些都应表现为"回落到默认语言"，而不是把门户页头打成错误页。
        ///   键比较使用大小写不敏感：文化名的大小写差异（`ja-jp` 与 `ja-JP`）不应导致取不到值。</zh-CN>
        ///   <en>A parse failure always becomes "no hit plus a reason" and never throws: the JSON column is hand-edited by
        ///   administrators, so arrays, nesting, or mistyped keys can occur, and all of them should degrade to the default
        ///   language rather than turning the portal header into an error page.
        ///   Key comparison is case-insensitive: casing differences in culture names (`ja-jp` versus `ja-JP`) must not lose the value.</en>
        /// </lang>
        /// </remarks>
        private static bool TryGetJsonValue(string localizedJson, CultureInfo culture, out string value, out string error)
        {
            value = null;
            error = null;

            if (string.IsNullOrWhiteSpace(localizedJson) || culture == null)
            {
                return false;
            }

            Dictionary<string, object> parsed;
            try
            {
                // <lang>
                //   <zh-CN>使用框架自带的 `JavaScriptSerializer`：它直接支持 `{"ja-JP":"值"}` 形态（无需
                //   DataContractJsonSerializer 的字典开关），且避免为此引入第三方依赖。
                //   反序列化目标用 `object` 值字典，避免"值是数字或 null"时强转字符串抛异常。</zh-CN>
                //   <en>Use the framework-provided `JavaScriptSerializer`: it handles the `{"ja-JP":"value"}` shape directly (no
                //   DataContractJsonSerializer dictionary switch needed) and avoids adding a third-party dependency for this.
                //   The deserialization target uses an `object`-valued dictionary so a numeric or null value cannot throw on a
                //   string cast.</en>
                // </lang>
                var serializer = new JavaScriptSerializer();
                parsed = serializer.Deserialize<Dictionary<string, object>>(localizedJson);
            }
            catch (ArgumentException exception)
            {
                error = exception.GetType().Name;
                return false;
            }
            catch (InvalidOperationException exception)
            {
                error = exception.GetType().Name;
                return false;
            }

            if (parsed == null)
            {
                return false;
            }

            // <lang>
            //   <zh-CN>三级键：完整文化名（`ja-JP`）→ 父文化名（`ja`）→ 两字母码（`ja`）。
            //   父文化与两字母码在多数文化下相同，但保留两级可以覆盖"自定义文化没有父级"等边界。</zh-CN>
            //   <en>Three-tier keys: full culture name (`ja-JP`) → parent culture name (`ja`) → two-letter code (`ja`). The parent
            //   and the two-letter code coincide for most cultures, yet keeping both covers boundaries such as custom cultures with no parent.</en>
            // </lang>
            foreach (string candidateKey in GetCultureCandidateKeys(culture))
            {
                foreach (string actualKey in parsed.Keys)
                {
                    if (!string.Equals(actualKey, candidateKey, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    object rawValue = parsed[actualKey];
                    string text = rawValue == null ? null : rawValue as string;
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    value = text;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>生成"完整文化名 → 父文化名 → 两字母码"的候选键序列，跳过空白与非特定文化。</zh-CN>
        ///   <en>Produces the candidate key sequence "full culture name → parent culture name → two-letter code", skipping blanks and the invariant culture.</en>
        /// </lang>
        /// </summary>
        /// <param name="culture">
        /// <l>
        ///   <zh-CN>目标界面文化。</zh-CN>
        ///   <en>Target UI culture.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>去空白后按优先级排列的候选键。</zh-CN>
        ///   <en>Trimmed candidate keys ordered by priority.</en>
        /// </l>
        /// </returns>
        private static IEnumerable<string> GetCultureCandidateKeys(CultureInfo culture)
        {
            var keys = new List<string>();
            if (culture == null)
            {
                return keys;
            }

            // <lang>
            //   <zh-CN>完整文化名优先：管理员按 `ja-JP` 填写时不应被 `ja` 抢先命中，地区差异才有意义。</zh-CN>
            //   <en>The full culture name comes first: when an administrator writes `ja-JP`, a bare `ja` must not win, so regional differences stay meaningful.</en>
            // </lang>
            if (!string.IsNullOrWhiteSpace(culture.Name))
            {
                keys.Add(culture.Name);
            }

            // <lang>
            //   <zh-CN>父文化名：非特定文化（Invariant）没有意义，跳过以避免用空键去匹配。</zh-CN>
            //   <en>Parent culture name: the invariant culture carries no meaning and is skipped so no empty key is matched.</en>
            // </lang>
            CultureInfo parent = culture.Parent;
            if (parent != null && !string.IsNullOrWhiteSpace(parent.Name))
            {
                keys.Add(parent.Name);
            }

            if (!string.IsNullOrWhiteSpace(culture.TwoLetterISOLanguageName))
            {
                keys.Add(culture.TwoLetterISOLanguageName);
            }

            return keys;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回首个非空白候选；全部空白时返回空字符串，保证调用方拿到的永远是可显示文本而不是 `null`。</zh-CN>
        ///   <en>Returns the first non-blank candidate, or an empty string when all are blank, so callers always receive displayable text rather than `null`.</en>
        /// </lang>
        /// </summary>
        /// <param name="candidates">
        /// <l>
        ///   <zh-CN>按优先级排列的候选文本。</zh-CN>
        ///   <en>Candidate texts ordered by priority.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>首个非空白候选，或空字符串。</zh-CN>
        ///   <en>The first non-blank candidate, or an empty string.</en>
        /// </l>
        /// </returns>
        private static string FirstNonBlank(params string[] candidates)
        {
            foreach (string candidate in candidates)
            {
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    return candidate;
                }
            }

            return string.Empty;
        }
    }
}
