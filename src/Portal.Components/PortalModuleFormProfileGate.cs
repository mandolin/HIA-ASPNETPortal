using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>形态启用判定的**纯内存只读判定器**：把形态维度（宿主框架 / 入口形态）的启用判定
    ///   纳入"部署级允许集"这一输入，**显式缺省关闭**（允许集为空或未声明即不启用）、未知值 fail-closed。
    ///   **与装配态开关严格分离**：本类不读配置、不读文件、不引用装配态台账或 `AllowCodeFile`，
    ///   不接入 `PortalModuleProfileSnapshot.IsPackageAllowed` 装载链，不改变既有包解析 / Profile 判定结果。</zh-CN>
    ///   <en>Pure in-memory read-only gate for form-aware enablement: decides whether a form dimension
    ///   (host framework / entry form) is enabled under a caller-supplied deployment-level allow-set, with an **explicit
    ///   default-off** posture (an empty/undeclared allow set enables nothing) and fail-closed unknown values.
    ///   It is **strictly separated from the assembly-state switch**: no config/file reads, no reference to the assembly
    ///   ledger or `AllowCodeFile`, no wiring into `PortalModuleProfileSnapshot.IsPackageAllowed`, and no change to existing
    ///   package parsing or profile decisions.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>调用方负责从部署配置提取允许集并传入；本类只做判定，不负责配置解析。
    ///   允许集用精确比较（`StringComparison.Ordinal`），大小写与空白变体不自动放行。</zh-CN>
    ///   <en>Callers extract allow-sets from deployment configuration and pass them in; this class only decides.
    ///   Allow-sets use exact (`StringComparison.Ordinal`) comparison — case and whitespace variants are not auto-allowed.</en>
    /// </lang>
    /// </remarks>
    public static class PortalModuleFormProfileGate
    {
        /// <summary><lang><zh-CN>缺省判定原因：允许集未提供或为空（显式缺省关闭）。</zh-CN><en>Default reason: allow-set absent or empty (explicit default-off).</en></lang></summary>
        public const string ReasonAllowSetEmpty = "FormProfile.AllowSetEmpty";

        /// <summary><lang><zh-CN>缺省判定原因：形态未声明（兼容既有包，不启用新形态）。</zh-CN><en>Default reason: form dimension undeclared (legacy compatible, no new form enabled).</en></lang></summary>
        public const string ReasonFormUndeclared = "FormProfile.FormUndeclared";

        /// <summary><lang><zh-CN>缺省判定原因：形态值不在允许集内（fail-closed）。</zh-CN><en>Default reason: form value not in the allow-set (fail closed).</en></lang></summary>
        public const string ReasonFormNotAllowed = "FormProfile.FormNotAllowed";

        /// <summary><lang><zh-CN>判定结果：是否启用 + 受控原因（启用时为空文本）。</zh-CN><en>Decision result: enabled flag plus a controlled reason (empty string when enabled).</en></lang></summary>
        public struct Decision
        {
            /// <summary>是否启用该形态。</summary>
            public bool Enabled;

            /// <summary>受控原因（启用时为空文本）。</summary>
            public string Reason;

            /// <summary><zh-CN>构造启用结果。</zh-CN><en>Builds an enabled decision.</en></summary>
            public static Decision Allow()
            {
                return new Decision { Enabled = true, Reason = string.Empty };
            }

            /// <summary><zh-CN>构造拒绝结果（受控原因）。</zh-CN><en>Builds a denied decision with a controlled reason.</en></summary>
            public static Decision Deny(string reason)
            {
                return new Decision { Enabled = false, Reason = reason ?? string.Empty };
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判定宿主框架形态是否启用。`allowSet` 为 null 或空集 → 缺省关闭；
        ///   `form` 未声明 → 不启用新形态（兼容既有包）；否则精确匹配允许集，未命中即 fail-closed。</zh-CN>
        ///   <en>Decides whether a host-framework form is enabled. A null/empty `allowSet` means default-off;
        ///   an undeclared `form` enables no new form (legacy compatible); otherwise an exact allow-set match is required
        ///   and a miss fails closed.</en>
        /// </lang>
        /// </summary>
        /// <param name="form">形态字段输入（由调用方从 manifest 提取）。</param>
        /// <param name="allowSet">部署级允许集；null 或空表示缺省关闭。</param>
        /// <returns>判定结果（启用标志 + 受控原因）。</returns>
        public static Decision IsHostFrameworkEnabled(PortalModuleFormContract.FieldInput form, IList<string> allowSet)
        {
            return Evaluate(form, allowSet);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判定入口形态是否启用。规则与宿主框架相同（缺省关闭、未声明不启用、精确匹配、fail-closed）。</zh-CN>
        ///   <en>Decides whether an entry form is enabled. Same rules as host framework (default-off, undeclared enables
        ///   nothing, exact match, fail-closed).</en>
        /// </lang>
        /// </summary>
        /// <param name="form">形态字段输入（由调用方从 manifest 提取）。</param>
        /// <param name="allowSet">部署级允许集；null 或空表示缺省关闭。</param>
        /// <returns>判定结果（启用标志 + 受控原因）。</returns>
        public static Decision IsEntryKindEnabled(PortalModuleFormContract.FieldInput form, IList<string> allowSet)
        {
            return Evaluate(form, allowSet);
        }

        private static Decision Evaluate(PortalModuleFormContract.FieldInput form, IList<string> allowSet)
        {
            if (allowSet == null || allowSet.Count == 0)
            {
                return Decision.Deny(ReasonAllowSetEmpty);
            }

            if (!form.Declared)
            {
                return Decision.Deny(ReasonFormUndeclared);
            }

            if (!form.IsString || string.IsNullOrWhiteSpace(form.Value))
            {
                return Decision.Deny(ReasonFormNotAllowed);
            }

            for (int i = 0; i < allowSet.Count; i++)
            {
                string candidate = allowSet[i];
                if (!string.IsNullOrWhiteSpace(candidate) && string.Equals(candidate.Trim(), form.Value, StringComparison.Ordinal))
                {
                    return Decision.Allow();
                }
            }

            return Decision.Deny(ReasonFormNotAllowed);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>校验部署级允许集本身：只允许封闭词表中的值，未知值 fail-closed。
        ///   这确保"配置写错"不会变成"默认放行"。</zh-CN>
        ///   <en>Validates a deployment allow-set itself: only closed-vocabulary values are accepted; unknown values fail closed.
        ///   This ensures a misconfiguration cannot become a default-allow.</en>
        /// </lang>
        /// </summary>
        /// <param name="allowSet">部署级允许集。</param>
        /// <param name="isRegistered">封闭词表判定函数。</param>
        /// <returns>允许集全为已登记值返回 true，否则 false。</returns>
        public static bool IsAllowSetWellFormed(IList<string> allowSet, Func<string, bool> isRegistered)
        {
            if (allowSet == null || isRegistered == null)
            {
                return false;
            }

            for (int i = 0; i < allowSet.Count; i++)
            {
                string candidate = allowSet[i];
                if (string.IsNullOrWhiteSpace(candidate) || !isRegistered(candidate.Trim()))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
