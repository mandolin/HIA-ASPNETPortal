using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>HIA 外围能力描述契约的当前草案基线。</zh-CN>
    ///   <en>Current draft baseline for the HIA peripheral capability contract.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该类型只定义门户拥有的、可离线验证的描述 envelope；它不加载外部程序集、不开启网络 transport，也不表示用户、权限、审计或业务数据已经成为跨系统协议。</zh-CN>
    ///   <en>This type defines a portal-owned, offline-verifiable descriptor envelope only. It does not load external assemblies, enable network transport, or make users, authorization, auditing, or business data cross-system APIs.</en>
    /// </lang>
    /// </remarks>
    public static class PortalHiaBoundaryContract
    {
        /// <summary>
        /// <l>
        ///   <zh-CN>当前外围契约的稳定名称。</zh-CN>
        ///   <en>Stable name of the current peripheral contract.</en>
        /// </l>
        /// </summary>
        public const string ContractName = "hia.portal.peripheral";

        /// <summary>
        /// <l>
        ///   <zh-CN>当前外围契约的草案版本。</zh-CN>
        ///   <en>Current draft version of the peripheral contract.</en>
        /// </l>
        /// </summary>
        public const string CurrentContractVersion = "0.1.0-draft";

        /// <summary>
        /// <l>
        ///   <zh-CN>当前门户 producer 的稳定标识。</zh-CN>
        ///   <en>Stable producer identifier for the current portal.</en>
        /// </l>
        /// </summary>
        public const string ProducerId = "hia-aspnetportal";

        /// <summary>
        /// <lang>
        ///   <zh-CN>限制部署级实例标识的字符集和长度，阻止路径、空白及敏感内容进入契约。</zh-CN>
        ///   <en>Restricts deployment instance identifiers by character set and length so paths, whitespace, and sensitive content cannot enter the contract.</en>
        /// </lang>
        /// </summary>
        private static readonly Regex InstanceIdPattern = new Regex(
            @"^[a-z0-9][a-z0-9._-]{0,127}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// <lang>
        ///   <zh-CN>约束 producer 版本为 invariant 的语义版本文本。</zh-CN>
        ///   <en>Constrains producer versions to invariant semantic-version text.</en>
        /// </lang>
        /// </summary>
        private static readonly Regex SemanticVersionPattern = new Regex(
            @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// <lang>
        ///   <zh-CN>外围契约允许发布的能力类型白名单。</zh-CN>
        ///   <en>Allowlist of capability kinds that the peripheral contract may publish.</en>
        /// </lang>
        /// </summary>
        private static readonly ISet<string> AllowedKinds = new HashSet<string>(StringComparer.Ordinal)
        {
            "portal.module-capability",
            "portal.theme-capability",
            "portal.setting-capability",
            "portal.health-capability",
            "portal.diagnostic-reference"
        };

        /// <summary>
        /// <lang>
        ///   <zh-CN>按能力类型保存 payload 必填/可选字段规则；规则只用于离线验证，不启用外部连接。</zh-CN>
        ///   <en>Stores required and optional payload-field rules by capability kind; rules serve offline validation only and enable no external connection.</en>
        /// </lang>
        /// </summary>
        private static readonly IDictionary<string, PayloadRule> PayloadRules =
            new Dictionary<string, PayloadRule>(StringComparer.Ordinal)
            {
                {
                    "portal.module-capability",
                    new PayloadRule(
                        new[] { "descriptorVersion", "packageId", "displayName", "packageVersion", "state", "capabilities" },
                        new string[0])
                },
                {
                    "portal.theme-capability",
                    new PayloadRule(
                        new[] { "descriptorVersion", "themeName", "packageVersion", "isAvailable", "capabilities" },
                        new string[0])
                },
                {
                    "portal.setting-capability",
                    new PayloadRule(
                        new[] { "descriptorVersion", "settingKey", "valueType", "isSensitive", "sourceLevel", "canEditOnline" },
                        new string[0])
                },
                {
                    "portal.health-capability",
                    new PayloadRule(
                        new[] { "descriptorVersion", "componentId", "status" },
                        new string[0])
                },
                {
                    "portal.diagnostic-reference",
                    new PayloadRule(
                        new[] { "descriptorVersion", "eventId", "level", "category" },
                        new string[0])
                }
            };

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证一个外围能力 envelope 是否符合当前草案、字段范围和隐私边界。</zh-CN>
        ///   <en>Validates whether a peripheral capability envelope meets the current draft, field scope, and privacy boundary.</en>
        /// </lang>
        /// </summary>
        /// <param name="envelope">
        /// <l>
        ///   <zh-CN>待验证的门户拥有 envelope。</zh-CN>
        ///   <en>Portal-owned envelope to validate.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>不包含 payload 原文的结构化验证结果。</zh-CN>
        ///   <en>Structured validation result without raw payload content.</en>
        /// </l>
        /// </returns>
        public static PortalHiaBoundaryValidationResult Validate(PortalHiaPeripheralEnvelope envelope)
        {
            if (envelope == null)
            {
                return Failure("HIA_PERIPHERAL_INVALID_ENVELOPE", "The peripheral envelope is required.");
            }

            if (!string.Equals(envelope.Contract, ContractName, StringComparison.Ordinal))
            {
                return Failure("HIA_PERIPHERAL_UNSUPPORTED_CONTRACT", "The peripheral contract name is unsupported.");
            }

            if (!string.Equals(envelope.ContractVersion, CurrentContractVersion, StringComparison.Ordinal))
            {
                return Failure("HIA_PERIPHERAL_UNSUPPORTED_VERSION", "The peripheral contract version is unsupported.");
            }

            // <lang>
            //   <zh-CN>暂存规范化实例标识；验证结果只返回状态，不把原始输入回显给调用方。</zh-CN>
            //   <en>Hold the normalized instance identifier; validation returns status only and never echoes the raw input.</en>
            // </lang>
            string normalizedInstanceId;
            if (!TryNormalizePortalInstanceId(envelope.PortalInstanceId, out normalizedInstanceId))
            {
                return Failure("HIA_PERIPHERAL_INVALID_INSTANCE_ID", "The portal instance identifier is invalid.");
            }

            if (envelope.Producer == null ||
                !string.Equals(envelope.Producer.Id, ProducerId, StringComparison.Ordinal) ||
                !IsSemanticVersion(envelope.Producer.Version))
            {
                return Failure("HIA_PERIPHERAL_INVALID_PRODUCER", "The producer descriptor is invalid.");
            }

            if (!AllowedKinds.Contains(envelope.Kind ?? string.Empty))
            {
                return Failure("HIA_PERIPHERAL_UNSUPPORTED_KIND", "The capability kind is unsupported.");
            }

            if (!IsUtcTimestamp(envelope.OccurredUtc))
            {
                return Failure("HIA_PERIPHERAL_INVALID_TIMESTAMP", "The UTC timestamp is invalid.");
            }

            // <lang>
            //   <zh-CN>先验证按 kind 约束的 payload，使未知字段和敏感字段在 metadata 之前被拒绝。</zh-CN>
            //   <en>Validate the kind-scoped payload first so unknown and sensitive fields are rejected before metadata processing.</en>
            // </lang>
            PortalHiaBoundaryValidationResult payloadResult = ValidatePayload(envelope.Kind, envelope.Payload);
            if (!payloadResult.IsValid)
            {
                return payloadResult;
            }

            // <lang>
            //   <zh-CN>最后验证可忽略 metadata，并只返回成功或稳定失败结果，不携带 payload 原文。</zh-CN>
            //   <en>Validate ignorable metadata last and return only success or a stable failure without raw payload content.</en>
            // </lang>
            PortalHiaBoundaryValidationResult metadataResult = ValidateMetadata(envelope.Metadata);
            return metadataResult.IsValid ? Success() : metadataResult;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证并规范化部署级门户实例标识。</zh-CN>
        ///   <en>Validates and normalizes a deployment-level portal instance identifier.</en>
        /// </lang>
        /// </summary>
        /// <param name="candidate">
        /// <l>
        ///   <zh-CN>部署配置提供的候选标识。</zh-CN>
        ///   <en>Candidate identifier supplied by deployment configuration.</en>
        /// </l>
        /// </param>
        /// <param name="normalizedInstanceId">
        /// <l>
        ///   <zh-CN>成功时返回小写受限标识或规范 GUID。</zh-CN>
        ///   <en>Normalized restricted identifier or canonical GUID when successful.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>候选值可作为非敏感稳定实例标识时为 true。</zh-CN>
        ///   <en>True when the candidate can serve as a non-sensitive stable instance identifier.</en>
        /// </l>
        /// </returns>
        public static bool TryNormalizePortalInstanceId(string candidate, out string normalizedInstanceId)
        {
            // <lang>
            //   <zh-CN>先清空输出，确保空值、格式错误或长度超限时不会残留上一次结果。</zh-CN>
            //   <en>Clear the output first so null, malformed, or overlong input cannot retain a previous result.</en>
            // </lang>
            normalizedInstanceId = string.Empty;
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return false;
            }

            // <lang>
            //   <zh-CN>去除部署配置外围空白，再按 GUID 优先、受限文本其次的顺序规范化。</zh-CN>
            //   <en>Trim deployment-configuration whitespace, then normalize in GUID-first and restricted-text order.</en>
            // </lang>
            string trimmed = candidate.Trim();

            // <lang>
            //   <zh-CN>保存 GUID 解析结果，以统一 D 格式输出不含大括号的稳定标识。</zh-CN>
            //   <en>Hold the GUID parse result so the stable identifier is emitted in brace-free D format.</en>
            // </lang>
            Guid guid;
            if (Guid.TryParse(trimmed, out guid))
            {
                normalizedInstanceId = guid.ToString("D");
                return true;
            }

            // <lang>
            //   <zh-CN>将非 GUID 候选转为 invariant 小写，再由正则执行字符和长度门禁。</zh-CN>
            //   <en>Convert non-GUID candidates to invariant lowercase before the regex enforces character and length bounds.</en>
            // </lang>
            string normalized = trimmed.ToLowerInvariant();
            if (!InstanceIdPattern.IsMatch(normalized))
            {
                return false;
            }

            normalizedInstanceId = normalized;
            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证 payload 的必填字段、未知字段和字段值安全性。</zh-CN>
        ///   <en>Validates required payload fields, unknown fields, and field-value safety.</en>
        /// </lang>
        /// </summary>
        private static PortalHiaBoundaryValidationResult ValidatePayload(
            string kind,
            IDictionary<string, object> payload)
        {
            if (payload == null)
            {
                return Failure("HIA_PERIPHERAL_INVALID_PAYLOAD", "The capability payload is required.");
            }

            // <lang>
            //   <zh-CN>按 capability kind 取得字段白名单；未知 kind 立即失败，避免宽松接受未定义 payload。</zh-CN>
            //   <en>Resolve the field allowlist by capability kind; fail unknown kinds immediately instead of accepting undefined payloads.</en>
            // </lang>
            PayloadRule rule;
            if (!PayloadRules.TryGetValue(kind, out rule))
            {
                return Failure("HIA_PERIPHERAL_UNSUPPORTED_KIND", "The capability kind has no payload rule.");
            }

            // <lang>
            //   <zh-CN>逐项确认契约声明的必填字段都存在，缺失时返回稳定错误码。</zh-CN>
            //   <en>Confirm every contract-declared required field is present and return a stable error code when one is missing.</en>
            // </lang>
            foreach (string requiredKey in rule.RequiredKeys)
            {
                if (!payload.ContainsKey(requiredKey))
                {
                    return Failure("HIA_PERIPHERAL_MISSING_FIELD", "A required capability field is missing.");
                }
            }

            // <lang>
            //   <zh-CN>遍历实际 payload，依次执行敏感字段拒绝、白名单校验和字段值类型/范围校验。</zh-CN>
            //   <en>Traverse the actual payload to reject sensitive names, enforce the allowlist, and validate value type and scope.</en>
            // </lang>
            foreach (KeyValuePair<string, object> entry in payload)
            {
                if (ContainsProhibitedFieldName(entry.Key))
                {
                    return Failure("HIA_PERIPHERAL_PROHIBITED_FIELD", "The capability payload contains a prohibited field.");
                }

                if (!rule.Allows(entry.Key))
                {
                    return Failure("HIA_PERIPHERAL_UNKNOWN_FIELD", "The capability payload contains an unknown field.");
                }

                if (!IsValidPayloadValue(kind, entry.Key, entry.Value))
                {
                    return Failure("HIA_PERIPHERAL_INVALID_FIELD", "The capability payload contains an invalid field value.");
                }
            }

            return Success();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证可忽略 metadata 的字段范围和版本一致性。</zh-CN>
        ///   <en>Validates field scope and version consistency for ignorable metadata.</en>
        /// </lang>
        /// </summary>
        private static PortalHiaBoundaryValidationResult ValidateMetadata(IDictionary<string, object> metadata)
        {
            if (metadata == null || metadata.Count == 0)
            {
                return Success();
            }

            // <lang>
            //   <zh-CN>只允许 metadataVersion 与 source 两个非敏感字段，并复用安全文本规则。</zh-CN>
            //   <en>Allow only the non-sensitive metadataVersion and source fields and reuse the safe-text rule.</en>
            // </lang>
            foreach (KeyValuePair<string, object> entry in metadata)
            {
                if (ContainsProhibitedFieldName(entry.Key))
                {
                    return Failure("HIA_PERIPHERAL_PROHIBITED_FIELD", "The metadata contains a prohibited field.");
                }

                if (!string.Equals(entry.Key, "metadataVersion", StringComparison.Ordinal) &&
                    !string.Equals(entry.Key, "source", StringComparison.Ordinal))
                {
                    return Failure("HIA_PERIPHERAL_UNKNOWN_FIELD", "The metadata contains an unknown field.");
                }

                // <lang>
                //   <zh-CN>将 metadata 值限制为文本，再校验长度、路径/URL 外观和版本一致性。</zh-CN>
                //   <en>Restrict metadata values to text, then validate length, path/URL appearance, and version consistency.</en>
                // </lang>
                string text = entry.Value as string;
                if (!IsSafeText(text, 100) ||
                    (string.Equals(entry.Key, "metadataVersion", StringComparison.Ordinal) &&
                     !string.Equals(text, CurrentContractVersion, StringComparison.Ordinal)))
                {
                    return Failure("HIA_PERIPHERAL_INVALID_FIELD", "The metadata contains an invalid field value.");
                }
            }

            return Success();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按字段语义验证 payload 值的类型、枚举范围和安全文本约束。</zh-CN>
        ///   <en>Validates payload value type, enumeration scope, and safe-text constraints by field semantics.</en>
        /// </lang>
        /// </summary>
        private static bool IsValidPayloadValue(string kind, string key, object value)
        {
            if (string.Equals(key, "capabilities", StringComparison.Ordinal))
            {
                return IsValidCapabilities(value);
            }

            if (string.Equals(key, "isAvailable", StringComparison.Ordinal) ||
                string.Equals(key, "isSensitive", StringComparison.Ordinal) ||
                string.Equals(key, "canEditOnline", StringComparison.Ordinal))
            {
                return value is bool;
            }

            // <lang>
            //   <zh-CN>非布尔/集合字段必须先转换为安全文本，后续枚举判断只作用于已净化值。</zh-CN>
            //   <en>Non-boolean and non-collection fields must first become safe text so later enum checks operate on sanitized values.</en>
            // </lang>
            string text = value as string;
            if (!IsSafeText(text, 200))
            {
                return false;
            }

            if (string.Equals(key, "descriptorVersion", StringComparison.Ordinal))
            {
                return string.Equals(text, CurrentContractVersion, StringComparison.Ordinal);
            }

            if (string.Equals(key, "status", StringComparison.Ordinal))
            {
                return string.Equals(text, "Healthy", StringComparison.Ordinal) ||
                       string.Equals(text, "Warning", StringComparison.Ordinal) ||
                       string.Equals(text, "Error", StringComparison.Ordinal) ||
                       string.Equals(text, "Unknown", StringComparison.Ordinal);
            }

            if (string.Equals(key, "state", StringComparison.Ordinal))
            {
                return string.Equals(text, "Available", StringComparison.Ordinal) ||
                       string.Equals(text, "Registered", StringComparison.Ordinal) ||
                       string.Equals(text, "Enabled", StringComparison.Ordinal) ||
                       string.Equals(text, "Disabled", StringComparison.Ordinal) ||
                       string.Equals(text, "UninstallReady", StringComparison.Ordinal);
            }

            if (string.Equals(key, "level", StringComparison.Ordinal))
            {
                return string.Equals(text, "Info", StringComparison.Ordinal) ||
                       string.Equals(text, "Warning", StringComparison.Ordinal) ||
                       string.Equals(text, "Error", StringComparison.Ordinal);
            }

            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证 capabilities 是非空的受限字符串集合，且数量不超过契约上限。</zh-CN>
        ///   <en>Validates that capabilities is a non-empty restricted string collection within the contract limit.</en>
        /// </lang>
        /// </summary>
        private static bool IsValidCapabilities(object value)
        {
            // <lang>
            //   <zh-CN>暂存集合视图并拒绝字符串伪装的集合输入。</zh-CN>
            //   <en>Hold the collection view and reject string values that masquerade as collections.</en>
            // </lang>
            IEnumerable values = value as IEnumerable;
            if (values == null || value is string)
            {
                return false;
            }

            // <lang>
            //   <zh-CN>累计能力项数量，用于执行非空和最多 32 项的契约上限。</zh-CN>
            //   <en>Count capability items to enforce the non-empty contract and its maximum of 32 entries.</en>
            // </lang>
            int count = 0;

            // <lang>
            //   <zh-CN>逐项要求安全文本和实例标识字符集，避免能力名携带路径或外部地址。</zh-CN>
            //   <en>Require safe text and instance-identifier characters for each item so capability names cannot carry paths or external addresses.</en>
            // </lang>
            foreach (object item in values)
            {
                // <lang>
                //   <zh-CN>将当前集合项限制为字符串，再复用统一文本安全检查。</zh-CN>
                //   <en>Restrict the current collection item to a string and reuse the shared text-safety check.</en>
                // </lang>
                string capability = item as string;
                if (!IsSafeText(capability, 80) || !InstanceIdPattern.IsMatch(capability.ToLowerInvariant()))
                {
                    return false;
                }

                count++;
                if (count > 32)
                {
                    return false;
                }
            }

            return count > 0;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证文本非空、长度受限，且不像物理路径或 URL。</zh-CN>
        ///   <en>Validates that text is non-empty, length-limited, and does not look like a physical path or URL.</en>
        /// </lang>
        /// </summary>
        private static bool IsSafeText(string value, int maximumLength)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Length <= maximumLength &&
                   !LooksLikeUnsafeLocation(value);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>识别会泄露身份、凭据、路径、审计或异常细节的字段名片段。</zh-CN>
        ///   <en>Detects field-name fragments that could expose identity, credentials, paths, audit data, or exception details.</en>
        /// </lang>
        /// </summary>
        private static bool ContainsProhibitedFieldName(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return true;
            }

            // <lang>
            //   <zh-CN>去除字段名分隔符并转为 invariant 小写，以便大小写/命名风格变化不能绕过敏感片段识别。</zh-CN>
            //   <en>Remove field-name separators and use invariant lowercase so casing or naming style cannot bypass sensitive-fragment detection.</en>
            // </lang>
            string normalized = key.Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();

            // <lang>
            //   <zh-CN>保存会触发拒绝的身份、凭据、路径、审计和异常片段；列表本身不包含运行时秘密。</zh-CN>
            //   <en>Store identity, credential, path, audit, and exception fragments that trigger rejection; the list contains no runtime secrets.</en>
            // </lang>
            string[] prohibitedFragments =
            {
                "password", "secret", "token", "cookie", "connectionstring", "requestbody",
                "physicalpath", "absolutepath", "filepath", "clientip", "ipaddress", "useragent",
                "username", "userid", "email", "role", "audit", "stacktrace", "exceptiondetail"
            };

            // <lang>
            //   <zh-CN>逐个匹配受限片段，命中任一项即 fail-closed。</zh-CN>
            //   <en>Match each restricted fragment and fail closed when any fragment is found.</en>
            // </lang>
            foreach (string fragment in prohibitedFragments)
            {
                if (normalized.Contains(fragment))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>识别不应进入外围描述的本机路径、UNC 路径、绝对路径和外部 URL。</zh-CN>
        ///   <en>Detects local paths, UNC paths, absolute paths, and external URLs that should not enter peripheral descriptors.</en>
        /// </lang>
        /// </summary>
        private static bool LooksLikeUnsafeLocation(string value)
        {
            // <lang>
            //   <zh-CN>去除外围空白后统一识别盘符、UNC、Unix 绝对路径和 URL 前缀。</zh-CN>
            //   <en>Trim surrounding whitespace before uniformly recognizing drive, UNC, Unix-absolute, and URL prefixes.</en>
            // </lang>
            string trimmed = value.Trim();
            return Regex.IsMatch(trimmed, @"^[A-Za-z]:[\\/]|^\\\\|^/|^[A-Za-z][A-Za-z0-9+.-]*://");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证时间文本是 round-trip 格式且偏移为 UTC。</zh-CN>
        ///   <en>Validates that timestamp text uses round-trip format and has a UTC offset.</en>
        /// </lang>
        /// </summary>
        private static bool IsUtcTimestamp(string value)
        {
            // <lang>
            //   <zh-CN>暂存 round-trip 解析结果，并要求偏移为零来保持 UTC-only 契约。</zh-CN>
            //   <en>Hold the round-trip parse result and require zero offset to preserve the UTC-only contract.</en>
            // </lang>
            DateTimeOffset timestamp;
            return DateTimeOffset.TryParseExact(
                value,
                "o",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out timestamp) && timestamp.Offset == TimeSpan.Zero;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证 producer 版本符合语义版本文本格式。</zh-CN>
        ///   <en>Validates that the producer version follows semantic-version text format.</en>
        /// </lang>
        /// </summary>
        private static bool IsSemanticVersion(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && SemanticVersionPattern.IsMatch(value);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建统一的契约验证成功结果。</zh-CN>
        ///   <en>Creates the unified successful contract-validation result.</en>
        /// </lang>
        /// </summary>
        private static PortalHiaBoundaryValidationResult Success()
        {
            return new PortalHiaBoundaryValidationResult(true, "HIA_PERIPHERAL_VALID", "The peripheral envelope is valid.");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建统一的契约验证失败结果。</zh-CN>
        ///   <en>Creates a unified failed contract-validation result.</en>
        /// </lang>
        /// </summary>
        private static PortalHiaBoundaryValidationResult Failure(string code, string message)
        {
            return new PortalHiaBoundaryValidationResult(false, code, message);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>某一 capability kind 的 payload 字段白名单规则。</zh-CN>
        ///   <en>Payload field allowlist rule for one capability kind.</en>
        /// </lang>
        /// </summary>
        private sealed class PayloadRule
        {
            /// <summary>
            /// <lang>
            ///   <zh-CN>当前 kind 的完整允许字段集合，供 required/optional 规则共享。</zh-CN>
            ///   <en>Complete allowed-field set for the current kind, shared by required and optional rules.</en>
            /// </lang>
            /// </summary>
            private readonly ISet<string> _allowedKeys;

            /// <summary>
            /// <lang>
            ///   <zh-CN>用必填键和可选键创建 payload 规则。</zh-CN>
            ///   <en>Creates a payload rule from required and optional keys.</en>
            /// </lang>
            /// </summary>
            public PayloadRule(IEnumerable<string> requiredKeys, IEnumerable<string> optionalKeys)
            {
                // <lang>
                //   <zh-CN>复制并冻结必填键集合，避免调用方后续修改规则输入。</zh-CN>
                //   <en>Copy and freeze required keys so callers cannot mutate rule inputs afterward.</en>
                // </lang>
                RequiredKeys = new List<string>(requiredKeys).AsReadOnly();

                // <lang>
                //   <zh-CN>以必填键初始化白名单，再追加受控可选键。</zh-CN>
                //   <en>Initialize the allowlist from required keys and then add controlled optional keys.</en>
                // </lang>
                _allowedKeys = new HashSet<string>(RequiredKeys, StringComparer.Ordinal);

                // <lang>
                //   <zh-CN>把每个可选键纳入同一 ordinal 白名单，保持字段匹配大小写敏感且稳定。</zh-CN>
                //   <en>Add each optional key to the same ordinal allowlist so field matching remains case-sensitive and stable.</en>
                // </lang>
                foreach (string optionalKey in optionalKeys)
                {
                    _allowedKeys.Add(optionalKey);
                }
            }

            /// <summary>
            /// <l>
            ///   <zh-CN>当前 kind 必须提供的字段名集合。</zh-CN>
            ///   <en>Field names that the current kind must provide.</en>
            /// </l>
            /// </summary>
            public IList<string> RequiredKeys { get; private set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>判断字段名是否在当前 kind 的白名单中。</zh-CN>
            ///   <en>Determines whether a field name is allowed for the current kind.</en>
            /// </lang>
            /// </summary>
            public bool Allows(string key)
            {
                return _allowedKeys.Contains(key ?? string.Empty);
            }
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>HIA 外围能力描述的可序列化 envelope。</zh-CN>
    ///   <en>Serializable envelope for an HIA peripheral capability descriptor.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>属性使用可写 DTO 形式以支持受控序列化；任何 consumer 在使用前都必须调用 <see cref="PortalHiaBoundaryContract.Validate"/>，不能信任未经验证的输入。</zh-CN>
    ///   <en>Properties remain writable DTO members for controlled serialization. Every consumer must call <see cref="PortalHiaBoundaryContract.Validate"/> before use and must not trust unvalidated input.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalHiaPeripheralEnvelope
    {
        /// <summary>
        /// <l>
        ///   <zh-CN>契约稳定名称。</zh-CN>
        ///   <en>Stable contract name.</en>
        /// </l>
        /// </summary>
        public string Contract { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>契约草案或稳定版本。</zh-CN>
        ///   <en>Draft or stable contract version.</en>
        /// </l>
        /// </summary>
        public string ContractVersion { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>部署级非敏感门户实例标识。</zh-CN>
        ///   <en>Deployment-level non-sensitive portal instance identifier.</en>
        /// </l>
        /// </summary>
        public string PortalInstanceId { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>产生当前描述的门户 producer。</zh-CN>
        ///   <en>Portal producer that created the current descriptor.</en>
        /// </l>
        /// </summary>
        public PortalHiaProducerDescriptor Producer { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>受支持的能力类型。</zh-CN>
        ///   <en>Supported capability kind.</en>
        /// </l>
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>ISO 8601 round-trip UTC 时间文本。</zh-CN>
        ///   <en>ISO 8601 round-trip UTC timestamp text.</en>
        /// </l>
        /// </summary>
        public string OccurredUtc { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>对应 kind 的受限能力描述。</zh-CN>
        ///   <en>Restricted capability descriptor for the selected kind.</en>
        /// </l>
        /// </summary>
        public IDictionary<string, object> Payload { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>可忽略的实现追踪 metadata。</zh-CN>
        ///   <en>Optional, ignorable implementation-tracing metadata.</en>
        /// </l>
        /// </summary>
        public IDictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>外围能力描述的 producer 身份。</zh-CN>
    ///   <en>Producer identity for a peripheral capability descriptor.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalHiaProducerDescriptor
    {
        /// <summary>
        /// <l>
        ///   <zh-CN>producer 稳定标识。</zh-CN>
        ///   <en>Stable producer identifier.</en>
        /// </l>
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>producer 的语义版本文本。</zh-CN>
        ///   <en>Semantic version text of the producer.</en>
        /// </l>
        /// </summary>
        public string Version { get; set; }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>外围能力契约的安全验证结果。</zh-CN>
    ///   <en>Safe validation result for a peripheral capability contract.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalHiaBoundaryValidationResult
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>创建不回显输入内容的契约验证结果。</zh-CN>
        ///   <en>Creates a contract-validation result that does not echo input content.</en>
        /// </lang>
        /// </summary>
        internal PortalHiaBoundaryValidationResult(bool isValid, string code, string message)
        {
            // <lang>
            //   <zh-CN>保存验证状态，供调用方只读取结构化结果而不接触原始 envelope。</zh-CN>
            //   <en>Store validation state so callers consume a structured result without accessing the raw envelope.</en>
            // </lang>
            IsValid = isValid;

            // <lang>
            //   <zh-CN>将机器代码空值归一为空字符串，保证失败/成功结果都可稳定序列化。</zh-CN>
            //   <en>Normalize a null machine code to empty so success and failure results serialize stably.</en>
            // </lang>
            Code = code ?? string.Empty;

            // <lang>
            //   <zh-CN>将安全消息空值归一为空字符串，不回显请求字段或 payload 原文。</zh-CN>
            //   <en>Normalize a null safe message to empty without echoing request fields or raw payload text.</en>
            // </lang>
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// <l>
        ///   <zh-CN>当前 envelope 是否通过验证。</zh-CN>
        ///   <en>Whether the current envelope passed validation.</en>
        /// </l>
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>稳定、可供机器处理的验证代码。</zh-CN>
        ///   <en>Stable machine-readable validation code.</en>
        /// </l>
        /// </summary>
        public string Code { get; private set; }

        /// <summary>
        /// <l>
        ///   <zh-CN>不回显输入 payload 的安全说明。</zh-CN>
        ///   <en>Safe message that does not echo the input payload.</en>
        /// </l>
        /// </summary>
        public string Message { get; private set; }
    }
}
