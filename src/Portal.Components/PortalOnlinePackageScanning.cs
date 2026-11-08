namespace ASPNET.StarterKit.Portal
{
    using System;

    /// <summary>
    /// <lang>
    ///   <zh-CN>恶意文件扫描的判定词表。三值封闭：`Unavailable` 与 `Infected` 都不得被当作 `Clean`。</zh-CN>
    ///   <en>Closed verdict vocabulary for malware scanning. `Unavailable` and `Infected` must never be treated as `Clean`.</en>
    /// </lang>
    /// </summary>
    public enum PortalOnlinePackageScanVerdict
    {
        /// <summary><lang><zh-CN>扫描器确认未命中恶意内容。</zh-CN><en>The scanner confirmed no malicious content.</en></lang></summary>
        Clean,

        /// <summary><lang><zh-CN>扫描器命中恶意内容。</zh-CN><en>The scanner detected malicious content.</en></lang></summary>
        Infected,

        /// <summary><lang><zh-CN>扫描器不可用、超时或返回未知结果；判定门按 fail-closed 拒绝。</zh-CN><en>The scanner is unavailable, timed out, or returned an unknown result; the gate rejects fail-closed.</en></lang></summary>
        Unavailable
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>在线能力包恶意扫描适配点。适配层只回答三值判定，不执行安装、不读取配置、不引入任何扫描引擎依赖。</zh-CN>
    ///   <en>Adapter point for online capability package malware scanning. The adapter only answers a three-value verdict;
    ///   it performs no installation, reads no config, and introduces no scanning-engine dependency.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>Q3 裁定「封装可降级、fail-closed、扫描器留适配点、不强制引入重依赖」。因此本接口是唯一切入点：
    ///   企业内网可接入既有扫描器或外部服务；未接线时调用方传入返回 `Unavailable` 的实现，判定门据此拒绝，
    ///   **不会**因为「还没接扫描器」而默认放行。适配层抛出的异常由判定门统一收敛为 `Unavailable`。</zh-CN>
    ///   <en>Q3 rules "degradable, fail-closed, leave a scanner adapter point, do not force heavy dependencies". This interface is the
    ///   only hook: an enterprise network may wire an existing scanner or external service; when unwired, callers pass an implementation
    ///   returning `Unavailable` and the gate rejects, so a missing scanner never defaults to allow. Exceptions from the adapter are
    ///   converged to `Unavailable` by the gate.</en>
    /// </lang>
    /// </remarks>
    public interface IPortalOnlinePackageScanner
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>扫描已隔离暂存的包内容并返回三值判定；实现必须自行保证不把不可信内容当作可执行代码加载。</zh-CN>
        ///   <en>Scans isolated staged package content and returns a three-value verdict; implementations must never load untrusted content as executable code.</en>
        /// </lang>
        /// </summary>
        /// <param name="stagingName"><l zh-CN="服务端生成的隔离暂存名，仅作关联标识。" en="Server-generated isolated staging name, used only as a correlation id." /></param>
        /// <param name="content"><l zh-CN="包内容字节，可为 null（适配层应返回 Unavailable）。" en="Package content bytes; may be null (the adapter should return Unavailable)." /></param>
        /// <returns><l zh-CN="三值判定；任何不确定情形返回 Unavailable。" en="Three-value verdict; any uncertain case returns Unavailable." /></returns>
        PortalOnlinePackageScanVerdict Scan(string stagingName, byte[] content);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>「未接线」扫描器的显式实现：恒定返回 `Unavailable`，用于让「没有扫描器」成为可见的拒绝原因，
    ///   而不是 null 或默认 `Clean`。这是 Q3「可降级、fail-closed」的默认落地。</zh-CN>
    ///   <en>Explicit "unwired" scanner implementation: always returns `Unavailable`, so "no scanner" becomes a visible rejection
    ///   reason rather than null or a default `Clean`. This is the default realization of Q3's "degradable, fail-closed" rule.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalOnlinePackageUnavailableScanner : IPortalOnlinePackageScanner
    {
        /// <summary><lang><zh-CN>恒返回 Unavailable；不读取内容、不记录不可信文本。</zh-CN><en>Always returns Unavailable; reads no content and records no untrusted text.</en></lang></summary>
        public PortalOnlinePackageScanVerdict Scan(string stagingName, byte[] content)
        {
            // <lang>
            //   <zh-CN>刻意忽略入参：默认实现不承担扫描职责，只表达「不可用」这一事实。</zh-CN>
            //   <en>Inputs are deliberately ignored: the default implementation performs no scanning and only expresses "unavailable".</en>
            // </lang>
            return PortalOnlinePackageScanVerdict.Unavailable;
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>在线能力包安全闸门：把「包级白名单 + SHA-256 完整性 + RSA-SHA256 签名（可选）+ 恶意扫描」四道判据
    ///   联合成**唯一放行入口**，任一不满足即拒绝（Q3 三系统联合判定 + 完整性前置）。纯逻辑、无状态、不触 IO。</zh-CN>
    ///   <en>Security gate for online capability packages: combines package allow-list, SHA-256 integrity, optional RSA-SHA256
    ///   signature, and malware scanning into the **single allow decision**; any failing criterion rejects. Pure, stateless, no IO.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>判定顺序固定：请求/内容 → 白名单 → 完整性 → 签名 → 扫描；每一步都在不产生副作用前完成。
    ///   签名可选语义：`requireSignature=true` 但公钥/签名缺失即拒绝；`requireSignature=false` 时若仍提供了签名材料，
    ///   则**必须验签通过**，否则拒绝——避免伪造签名被静默忽略。扫描器为 null、抛异常或返回 `Unavailable` 一律拒绝（fail-closed）。
    ///   **A 级硬边界**：期望哈希、公钥均来自服务端受信任来源，接口不提供任何「从包内声明推导信任」的入口。</zh-CN>
    ///   <en>The order is fixed: request/content → allow-list → integrity → signature → scan; every step completes before any side effect.
    ///   Optional-signature semantics: `requireSignature=true` with missing key/signature rejects; with `requireSignature=false`, supplied
    ///   signature material must still verify, so a forged signature is never silently ignored. A null scanner, an exception, or an
    ///   `Unavailable` verdict all reject (fail-closed).
    ///   **A-level hard boundary**: expected hash and public key come from trusted server sources; the API offers no way to derive trust from package-declared claims.</en>
    /// </lang>
    /// </remarks>
    public static class PortalOnlinePackageSecurityGate
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>联合判定是否允许把一个隔离暂存包送入安装流程；通过时 reason 为空字符串。</zh-CN>
        ///   <en>Jointly decides whether an isolated staged package may enter installation; reason is empty on success.</en>
        /// </lang>
        /// </summary>
        /// <param name="clientFileName"><l zh-CN="浏览器提交的原始文件名，仅用于扩展名白名单，不参与路径构造。" en="Raw browser file name, used only for extension allow-listing, never for path construction." /></param>
        /// <param name="content"><l zh-CN="包内容字节。" en="Package content bytes." /></param>
        /// <param name="expectedHash"><l zh-CN="服务端隔离暂存时记录的期望 SHA-256 摘要。" en="Server-recorded expected SHA-256 digest from isolated staging." /></param>
        /// <param name="publicKeyXml"><l zh-CN="服务端受信任公钥 XML；无签名体系时传空。" en="Server-trusted public-key XML; pass empty when no signature scheme is configured." /></param>
        /// <param name="signatureBase64"><l zh-CN="Base64 签名值；无签名时传空。" en="Base64 signature value; pass empty when unsigned." /></param>
        /// <param name="requireSignature"><l zh-CN="true=本部署要求签名；false=签名可选但提供则必须有效。" en="true = this deployment requires signatures; false = optional, but supplied signatures must still be valid." /></param>
        /// <param name="scanner"><l zh-CN="扫描适配实现；null 视为不可用并拒绝。" en="Scanner adapter; null is treated as unavailable and rejected." /></param>
        /// <param name="stagingName"><l zh-CN="服务端生成的隔离暂存名，供扫描关联。" en="Server-generated isolated staging name for scan correlation." /></param>
        /// <param name="reason"><l zh-CN="拒绝原因的稳定短语，不回显不可信文本。" en="Stable rejection phrase that echoes no untrusted text." /></param>
        /// <returns><l zh-CN="四道判据全部通过时为 true。" en="True when all four criteria pass." /></returns>
        public static bool TryAllowInstall(
            string clientFileName,
            byte[] content,
            string expectedHash,
            string publicKeyXml,
            string signatureBase64,
            bool requireSignature,
            IPortalOnlinePackageScanner scanner,
            string stagingName,
            out string reason)
        {
            // <lang>
            //   <zh-CN>输出先置失败态，保证所有提前返回都不留下未初始化原因。</zh-CN>
            //   <en>Initialise the output to a failure state so no early return leaves an uninitialised reason.</en>
            // </lang>
            reason = string.Empty;

            // <lang>
            //   <zh-CN>内容缺失或暂存名缺失一律拒绝，不把 null 解释为「空包可放行」。</zh-CN>
            //   <en>Missing content or staging name rejects; null is never interpreted as "an empty package is allowed".</en>
            // </lang>
            if (content == null || string.IsNullOrWhiteSpace(stagingName))
            {
                reason = "Package content or staging name is missing.";
                return false;
            }

            // <lang>
            //   <zh-CN>第一道：复用 W-anp-P276 的包级白名单（.zip 唯一、10 MiB 上限、文件名净化）。</zh-CN>
            //   <en>First criterion: reuse the W-anp-P276 package allow-list (only .zip, 10 MiB cap, filename sanitisation).</en>
            // </lang>
            string allowListReason;
            if (!PortalOnlinePackagePolicy.TryValidatePackageFile(clientFileName, content.Length, out allowListReason))
            {
                reason = allowListReason;
                return false;
            }

            // <lang>
            //   <zh-CN>第二道：内容摘要必须与服务端记录一致，承担 4.8 无 CRC-32 自动校验所缺的完整性职责。</zh-CN>
            //   <en>Second criterion: the content digest must match the server record, carrying the integrity duty missing from 4.8 CRC validation.</en>
            // </lang>
            string integrityReason;
            if (!PortalOnlinePackageIntegrity.TryVerifyIntegrity(content, expectedHash, out integrityReason))
            {
                reason = integrityReason;
                return false;
            }

            // <lang>
            //   <zh-CN>第三道：签名。区分三种情形——材料齐备则必须验签通过；两项皆无则按「是否要求签名」决定；
            //   **只有其中一项**（例如只给签名不给公钥）一律拒绝，不得当成「未提供」而静默放行。
            //   否则攻击者可用孤立/伪造的签名材料绕过「可选签名」路径。</zh-CN>
            //   <en>Third criterion: signature. Three cases — complete material must verify; neither supplied follows
            //   "is signature required"; **exactly one item** (e.g. a signature with no public key) always rejects and is
            //   never treated as "not supplied" and silently allowed. Otherwise an attacker could bypass the optional-signature
            //   path with orphaned/forged signature material.</en>
            // </lang>
            bool hasCompleteMaterial = PortalOnlinePackageIntegrity.HasSignatureMaterial(publicKeyXml, signatureBase64);
            bool hasPublicKey = !string.IsNullOrWhiteSpace(publicKeyXml);
            bool hasSignature = !string.IsNullOrWhiteSpace(signatureBase64);
            if (hasPublicKey != hasSignature)
            {
                reason = "Package signature material is incomplete.";
                return false;
            }
            if (requireSignature && !hasCompleteMaterial)
            {
                reason = "Package signature is required but missing.";
                return false;
            }
            if (hasCompleteMaterial)
            {
                string signatureReason;
                if (!PortalOnlinePackageIntegrity.TryVerifySignature(content, publicKeyXml, signatureBase64, out signatureReason))
                {
                    reason = signatureReason;
                    return false;
                }
            }

            // <lang>
            //   <zh-CN>第四道：扫描。null 适配视为不可用；异常与未知结果统一收敛为 Unavailable，绝不降级为 Clean。</zh-CN>
            //   <en>Fourth criterion: scan. A null adapter is unavailable; exceptions and unknown results converge to Unavailable and never degrade to Clean.</en>
            // </lang>
            if (scanner == null)
            {
                reason = "Malware scanner is unavailable; the package is rejected (fail-closed).";
                return false;
            }

            // <lang>
            //   <zh-CN>承接扫描判定；任何适配层异常都在 catch 中收敛为稳定拒绝原因。</zh-CN>
            //   <en>Holds the scan verdict; any adapter exception converges to a stable rejection reason in catch.</en>
            // </lang>
            PortalOnlinePackageScanVerdict verdict;
            try
            {
                verdict = scanner.Scan(stagingName, content);
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>不回显异常细节，也不尝试第二次扫描，避免失败路径递归或泄露扫描器内部信息。</zh-CN>
                //   <en>Do not echo exception details or retry scanning, avoiding recursive failure paths or scanner internals leakage.</en>
                // </lang>
                reason = "Malware scanner failed; the package is rejected (fail-closed).";
                return false;
            }

            // <lang>
            //   <zh-CN>只有显式 Clean 才放行；Infected、Unavailable 以及枚举外的伪造值一律拒绝。</zh-CN>
            //   <en>Only an explicit Clean allows installation; Infected, Unavailable, and forged out-of-enum values all reject.</en>
            // </lang>
            if (verdict != PortalOnlinePackageScanVerdict.Clean)
            {
                reason = verdict == PortalOnlinePackageScanVerdict.Infected
                    ? "Malware scanner detected malicious content."
                    : "Malware scanner is unavailable; the package is rejected (fail-closed).";
                return false;
            }

            return true;
        }
    }
}
