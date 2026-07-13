using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// HIA 外围能力描述契约的当前草案基线。
    /// Current draft baseline for the HIA peripheral capability contract.
    /// </summary>
    /// <remarks>
    /// 该类型只定义门户拥有的、可离线验证的描述 envelope；它不加载外部程序集、不开启网络 transport，
    /// 也不表示用户、权限、审计或业务数据已经成为跨系统协议。
    /// This type defines a portal-owned, offline-verifiable descriptor envelope only. It does not load external
    /// assemblies, enable network transport, or make users, authorization, auditing, or business data cross-system APIs.
    /// </remarks>
    public static class PortalHiaBoundaryContract
    {
        /// <summary>
        /// 当前外围契约的稳定名称。
        /// Stable name of the current peripheral contract.
        /// </summary>
        public const string ContractName = "hia.portal.peripheral";

        /// <summary>
        /// 当前外围契约的草案版本。
        /// Current draft version of the peripheral contract.
        /// </summary>
        public const string CurrentContractVersion = "0.1.0-draft";

        /// <summary>
        /// 当前门户 producer 的稳定标识。
        /// Stable producer identifier for the current portal.
        /// </summary>
        public const string ProducerId = "hia-aspnetportal";

        private static readonly Regex InstanceIdPattern = new Regex(
            @"^[a-z0-9][a-z0-9._-]{0,127}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex SemanticVersionPattern = new Regex(
            @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly ISet<string> AllowedKinds = new HashSet<string>(StringComparer.Ordinal)
        {
            "portal.module-capability",
            "portal.theme-capability",
            "portal.setting-capability",
            "portal.health-capability",
            "portal.diagnostic-reference"
        };

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
        /// 验证一个外围能力 envelope 是否符合当前草案、字段范围和隐私边界。
        /// Validates whether a peripheral capability envelope meets the current draft, field scope, and privacy boundary.
        /// </summary>
        /// <param name="envelope">待验证的门户拥有 envelope。Portal-owned envelope to validate.</param>
        /// <returns>不包含 payload 原文的结构化验证结果。Structured validation result without raw payload content.</returns>
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

            PortalHiaBoundaryValidationResult payloadResult = ValidatePayload(envelope.Kind, envelope.Payload);
            if (!payloadResult.IsValid)
            {
                return payloadResult;
            }

            PortalHiaBoundaryValidationResult metadataResult = ValidateMetadata(envelope.Metadata);
            return metadataResult.IsValid ? Success() : metadataResult;
        }

        /// <summary>
        /// 验证并规范化部署级门户实例标识。
        /// Validates and normalizes a deployment-level portal instance identifier.
        /// </summary>
        /// <param name="candidate">部署配置提供的候选标识。Candidate identifier supplied by deployment configuration.</param>
        /// <param name="normalizedInstanceId">成功时返回小写受限标识或规范 GUID。Normalized restricted identifier or canonical GUID when successful.</param>
        /// <returns>候选值可作为非敏感稳定实例标识时为 true。True when the candidate can serve as a non-sensitive stable instance identifier.</returns>
        public static bool TryNormalizePortalInstanceId(string candidate, out string normalizedInstanceId)
        {
            normalizedInstanceId = string.Empty;
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return false;
            }

            string trimmed = candidate.Trim();
            Guid guid;
            if (Guid.TryParse(trimmed, out guid))
            {
                normalizedInstanceId = guid.ToString("D");
                return true;
            }

            string normalized = trimmed.ToLowerInvariant();
            if (!InstanceIdPattern.IsMatch(normalized))
            {
                return false;
            }

            normalizedInstanceId = normalized;
            return true;
        }

        private static PortalHiaBoundaryValidationResult ValidatePayload(
            string kind,
            IDictionary<string, object> payload)
        {
            if (payload == null)
            {
                return Failure("HIA_PERIPHERAL_INVALID_PAYLOAD", "The capability payload is required.");
            }

            PayloadRule rule;
            if (!PayloadRules.TryGetValue(kind, out rule))
            {
                return Failure("HIA_PERIPHERAL_UNSUPPORTED_KIND", "The capability kind has no payload rule.");
            }

            foreach (string requiredKey in rule.RequiredKeys)
            {
                if (!payload.ContainsKey(requiredKey))
                {
                    return Failure("HIA_PERIPHERAL_MISSING_FIELD", "A required capability field is missing.");
                }
            }

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

        private static PortalHiaBoundaryValidationResult ValidateMetadata(IDictionary<string, object> metadata)
        {
            if (metadata == null || metadata.Count == 0)
            {
                return Success();
            }

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

        private static bool IsValidCapabilities(object value)
        {
            IEnumerable values = value as IEnumerable;
            if (values == null || value is string)
            {
                return false;
            }

            int count = 0;
            foreach (object item in values)
            {
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

        private static bool IsSafeText(string value, int maximumLength)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Length <= maximumLength &&
                   !LooksLikeUnsafeLocation(value);
        }

        private static bool ContainsProhibitedFieldName(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return true;
            }

            string normalized = key.Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
            string[] prohibitedFragments =
            {
                "password", "secret", "token", "cookie", "connectionstring", "requestbody",
                "physicalpath", "absolutepath", "filepath", "clientip", "ipaddress", "useragent",
                "username", "userid", "email", "role", "audit", "stacktrace", "exceptiondetail"
            };

            foreach (string fragment in prohibitedFragments)
            {
                if (normalized.Contains(fragment))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool LooksLikeUnsafeLocation(string value)
        {
            string trimmed = value.Trim();
            return Regex.IsMatch(trimmed, @"^[A-Za-z]:[\\/]|^\\\\|^/|^[A-Za-z][A-Za-z0-9+.-]*://");
        }

        private static bool IsUtcTimestamp(string value)
        {
            DateTimeOffset timestamp;
            return DateTimeOffset.TryParseExact(
                value,
                "o",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out timestamp) && timestamp.Offset == TimeSpan.Zero;
        }

        private static bool IsSemanticVersion(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && SemanticVersionPattern.IsMatch(value);
        }

        private static PortalHiaBoundaryValidationResult Success()
        {
            return new PortalHiaBoundaryValidationResult(true, "HIA_PERIPHERAL_VALID", "The peripheral envelope is valid.");
        }

        private static PortalHiaBoundaryValidationResult Failure(string code, string message)
        {
            return new PortalHiaBoundaryValidationResult(false, code, message);
        }

        private sealed class PayloadRule
        {
            private readonly ISet<string> _allowedKeys;

            public PayloadRule(IEnumerable<string> requiredKeys, IEnumerable<string> optionalKeys)
            {
                RequiredKeys = new List<string>(requiredKeys).AsReadOnly();
                _allowedKeys = new HashSet<string>(RequiredKeys, StringComparer.Ordinal);
                foreach (string optionalKey in optionalKeys)
                {
                    _allowedKeys.Add(optionalKey);
                }
            }

            public IList<string> RequiredKeys { get; private set; }

            public bool Allows(string key)
            {
                return _allowedKeys.Contains(key ?? string.Empty);
            }
        }
    }

    /// <summary>
    /// HIA 外围能力描述的可序列化 envelope。
    /// Serializable envelope for an HIA peripheral capability descriptor.
    /// </summary>
    /// <remarks>
    /// 属性使用可写 DTO 形式以支持未来受控序列化；任何 consumer 在使用前都必须调用
    /// <see cref="PortalHiaBoundaryContract.Validate"/>，不能信任未经验证的输入。
    /// Properties remain writable DTO members for future controlled serialization. Every consumer must call
    /// <see cref="PortalHiaBoundaryContract.Validate"/> before use and must not trust unvalidated input.
    /// </remarks>
    public sealed class PortalHiaPeripheralEnvelope
    {
        /// <summary>
        /// 契约稳定名称。
        /// Stable contract name.
        /// </summary>
        public string Contract { get; set; }

        /// <summary>
        /// 契约草案或稳定版本。
        /// Draft or stable contract version.
        /// </summary>
        public string ContractVersion { get; set; }

        /// <summary>
        /// 部署级非敏感门户实例标识。
        /// Deployment-level non-sensitive portal instance identifier.
        /// </summary>
        public string PortalInstanceId { get; set; }

        /// <summary>
        /// 产生当前描述的门户 producer。
        /// Portal producer that created the current descriptor.
        /// </summary>
        public PortalHiaProducerDescriptor Producer { get; set; }

        /// <summary>
        /// 受支持的能力类型。
        /// Supported capability kind.
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// ISO 8601 round-trip UTC 时间文本。
        /// ISO 8601 round-trip UTC timestamp text.
        /// </summary>
        public string OccurredUtc { get; set; }

        /// <summary>
        /// 对应 kind 的受限能力描述。
        /// Restricted capability descriptor for the selected kind.
        /// </summary>
        public IDictionary<string, object> Payload { get; set; }

        /// <summary>
        /// 可忽略的实现追踪 metadata。
        /// Optional, ignorable implementation-tracing metadata.
        /// </summary>
        public IDictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// 外围能力描述的 producer 身份。
    /// Producer identity for a peripheral capability descriptor.
    /// </summary>
    public sealed class PortalHiaProducerDescriptor
    {
        /// <summary>
        /// producer 稳定标识。
        /// Stable producer identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// producer 的语义版本文本。
        /// Semantic version text of the producer.
        /// </summary>
        public string Version { get; set; }
    }

    /// <summary>
    /// 外围能力契约的安全验证结果。
    /// Safe validation result for a peripheral capability contract.
    /// </summary>
    public sealed class PortalHiaBoundaryValidationResult
    {
        internal PortalHiaBoundaryValidationResult(bool isValid, string code, string message)
        {
            IsValid = isValid;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// 当前 envelope 是否通过验证。
        /// Whether the current envelope passed validation.
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// 稳定、可供机器处理的验证代码。
        /// Stable machine-readable validation code.
        /// </summary>
        public string Code { get; private set; }

        /// <summary>
        /// 不回显输入 payload 的安全说明。
        /// Safe message that does not echo the input payload.
        /// </summary>
        public string Message { get; private set; }
    }
}
