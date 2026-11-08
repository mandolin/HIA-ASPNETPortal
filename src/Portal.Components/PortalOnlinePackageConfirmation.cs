using System;
using System.Globalization;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>在线能力包双步确认的一次性票据：绑定账号、隔离暂存包与其哈希，并携带独立的防伪同步令牌。</zh-CN>
    ///   <en>One-time ticket for online capability package two-step confirmation: binds the actor, the isolated staged package and its hash, and carries an independent anti-forgery synchroniser token.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>票据是不可变值对象；消费后通过 WithConsumed 返回新实例，避免共享可变状态。
    ///   **A 级硬边界**：票据只承载「谁、哪个包、什么哈希、何时、是否消费」，**不承载**包的来源声明、capability 声明或任何角色/授权结论；
    ///   这些信息永不参与授权判定。票据字段来自服务端校验结果，不接受浏览器原文作为授权依据。</zh-CN>
    ///   <en>The ticket is an immutable value object; consumption returns a new instance via WithConsumed so no mutable state is shared.
    ///   **A-level hard boundary**: the ticket carries only "who, which package, which hash, when, whether consumed" and **never** a package origin claim,
    ///   capability claim, or any role/authorization conclusion; none of those take part in authorization. Fields come from server-side validation,
    ///   and raw browser text is never accepted as an authorization basis.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalOnlinePackageConfirmationTicket
    {
        /// <summary><lang><zh-CN>一次性随机票据标识，用于索引与防重放。</zh-CN><en>One-time random ticket identifier used for lookup and replay defence.</en></lang></summary>
        public string Nonce { get; private set; }

        /// <summary><lang><zh-CN>防伪同步令牌（CSRF synchroniser token），与 Nonce 独立生成，须由表单隐藏字段回传。</zh-CN><en>Anti-forgery synchroniser token (CSRF), generated independently of the Nonce and returned through a hidden form field.</en></lang></summary>
        public string AntiForgeryToken { get; private set; }

        /// <summary><lang><zh-CN>发起上传的账号名；确认时必须一致。</zh-CN><en>Account name that initiated the upload; must match at confirmation.</en></lang></summary>
        public string ActorName { get; private set; }

        /// <summary><lang><zh-CN>隔离暂存区内服务端生成的包存储名，不来自用户文件名。</zh-CN><en>Server-generated package storage name in the isolated staging area, never derived from a user file name.</en></lang></summary>
        public string PackageStorageName { get; private set; }

        /// <summary><lang><zh-CN>包内容的 SHA-256 十六进制摘要；确认时须与暂存包当前哈希一致。</zh-CN><en>SHA-256 hex digest of package content; must match the staged package hash at confirmation.</en></lang></summary>
        public string PackageHash { get; private set; }

        /// <summary><lang><zh-CN>签发时间（UTC）。</zh-CN><en>Issue time (UTC).</en></lang></summary>
        public DateTime IssuedUtc { get; private set; }

        /// <summary><lang><zh-CN>过期时间（UTC），超过即拒绝确认。</zh-CN><en>Expiry time (UTC); confirmation is rejected afterwards.</en></lang></summary>
        public DateTime ExpiresUtc { get; private set; }

        /// <summary><lang><zh-CN>消费时间（UTC）；null 表示尚未消费，非空表示本票据已被一次性使用。</zh-CN><en>Consumption time (UTC); null means unused, non-null means this one-time ticket has been used.</en></lang></summary>
        public DateTime? ConsumedUtc { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>由服务端签发构造票据；调用方须保证所有字段已通过校验。</zh-CN>
        ///   <en>Constructs a ticket from a server-side issuance; callers must ensure every field passed validation.</en>
        /// </lang>
        /// </summary>
        internal PortalOnlinePackageConfirmationTicket(
            string nonce,
            string antiForgeryToken,
            string actorName,
            string packageStorageName,
            string packageHash,
            DateTime issuedUtc,
            DateTime expiresUtc)
        {
            // <lang>
            //   <zh-CN>逐项赋值；不在此处做 Trim 或大小写折叠，保持服务端签发的原始值可被精确比对。</zh-CN>
            //   <en>Assign each field without trimming or case folding, preserving the server-issued values for exact comparison.</en>
            // </lang>
            Nonce = nonce;
            AntiForgeryToken = antiForgeryToken;
            ActorName = actorName;
            PackageStorageName = packageStorageName;
            PackageHash = packageHash;
            IssuedUtc = issuedUtc;
            ExpiresUtc = expiresUtc;
            ConsumedUtc = null;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回仅消费标记不同的新票据，原票据保持不变（不可变值对象）。</zh-CN>
        ///   <en>Returns a new ticket differing only in the consumption marker, leaving the original unchanged (immutable value object).</en>
        /// </lang>
        /// </summary>
        /// <param name="consumedUtc"><l zh-CN="消费时间（UTC）。" en="Consumption time (UTC)." /></param>
        /// <returns><l zh-CN="已消费的新票据。" en="A new, consumed ticket." /></returns>
        internal PortalOnlinePackageConfirmationTicket WithConsumed(DateTime consumedUtc)
        {
            // <lang>
            //   <zh-CN>复用同一构造入口复制全部字段，再单独设置消费时间，避免字段漂移或漏拷。</zh-CN>
            //   <en>Copy every field through the same constructor, then set consumption separately, so no field can drift or be omitted.</en>
            // </lang>
            PortalOnlinePackageConfirmationTicket copy = new PortalOnlinePackageConfirmationTicket(
                Nonce, AntiForgeryToken, ActorName, PackageStorageName, PackageHash, IssuedUtc, ExpiresUtc);
            copy.ConsumedUtc = consumedUtc;
            return copy;
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>在线能力包双步确认闸：签发一次性票据，并在「确认安装」时执行二次鉴权、防伪令牌、包哈希绑定、过期与防重放判定。</zh-CN>
    ///   <en>Two-step confirmation gate for online capability packages: issues a one-time ticket and, at "confirm install", enforces re-authorization, anti-forgery token, package-hash binding, expiry, and replay defence.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本类型是**纯逻辑、无状态**的判定器：不读取 HTTP 请求、不读写数据库、不持有票据存储、不加载程序集。
    ///   页面层负责在上传成功后持久化票据、在确认时取回并回传；持久化介质（会话/数据库）由后续独立能力模块接线。
    ///   依据 OWASP CSRF Prevention Cheat Sheet 的同步令牌模式与 DNN「上传 ZIP → 逐页展示包信息 → 显式接受后才安装」的分步向导。
    ///   **A 级硬边界**：闸只接受 `PortalPermissionKeys.ModuleOnlinePackageUpload` / `ModuleOnlinePackageConfirm` 两个已登记键的判定结果，
    ///   不接受来源声明、capability 声明或角色名作为授权依据；不授予 `Admins`，也不扩大任何角色的作用域。</zh-CN>
    ///   <en>This type is a **pure, stateless** decision maker: it reads no HTTP request, touches no database, holds no ticket store, and loads no assembly.
    ///   The page layer persists the ticket after a successful upload and retrieves it for confirmation; the persistence medium (session/database)
    ///   is wired by a later independent capability module. It follows the synchroniser-token pattern of the OWASP CSRF Prevention Cheat Sheet
    ///   and DNN's wizard ("upload ZIP, review package information page by page, install only after explicit acceptance").
    ///   **A-level hard boundary**: the gate accepts only verdicts for the two registered keys `PortalPermissionKeys.ModuleOnlinePackageUpload` /
    ///   `ModuleOnlinePackageConfirm`; no origin claim, capability claim, or role name is an authorization basis; it grants no `Admins` role
    ///   and extends no role's scope.</en>
    /// </lang>
    /// </remarks>
    public static class PortalOnlinePackageConfirmationGate
    {
        /// <summary><lang><zh-CN>票据有效期（分钟）；超时须重新上传校验，不自动续期。</zh-CN><en>Ticket lifetime in minutes; expiry requires a fresh upload validation with no automatic renewal.</en></lang></summary>
        public const int TicketLifetimeMinutes = 30;

        /// <summary><lang><zh-CN>SHA-256 十六进制摘要的固定长度（64 字符）。</zh-CN><en>Fixed length of a SHA-256 hex digest (64 characters).</en></lang></summary>
        public const int PackageHashHexLength = 64;

        /// <summary>
        /// <lang>
        ///   <zh-CN>在上传校验通过且已判定具备上传权限后，签发一次性确认票据。</zh-CN>
        ///   <en>Issues a one-time confirmation ticket after upload validation passed and upload permission was granted.</en>
        /// </lang>
        /// </summary>
        /// <param name="actorName"><l zh-CN="发起上传的已认证账号名。" en="Authenticated account name initiating the upload." /></param>
        /// <param name="packageStorageName"><l zh-CN="隔离暂存区服务端生成的包存储名。" en="Server-generated package storage name in the isolated staging area." /></param>
        /// <param name="packageHash"><l zh-CN="包内容的 SHA-256 十六进制摘要。" en="SHA-256 hex digest of the package content." /></param>
        /// <param name="issuedUtc"><l zh-CN="签发时间（UTC），由调用方提供以便测试与审计一致。" en="Issue time (UTC), supplied by the caller for testability and audit consistency." /></param>
        /// <param name="uploadPermissionKey"><l zh-CN="页面层判定的上传权限键，必须是 Module.OnlinePackage.Upload。" en="Upload permission key judged by the page layer; must be Module.OnlinePackage.Upload." /></param>
        /// <param name="uploadGranted"><l zh-CN="页面层已判定的上传权限结果；false 一律拒绝。" en="Page-layer upload permission verdict; false is always rejected." /></param>
        /// <param name="ticket"><l zh-CN="成功时为新票据，失败时为 null。" en="New ticket on success, null on failure." /></param>
        /// <param name="reason"><l zh-CN="拒绝原因的稳定短语，不回显不可信文本。" en="Stable rejection phrase that never echoes untrusted text." /></param>
        /// <returns><l zh-CN="签发票据时为 true。" en="True when a ticket was issued." /></returns>
        public static bool TryIssue(
            string actorName,
            string packageStorageName,
            string packageHash,
            DateTime issuedUtc,
            string uploadPermissionKey,
            bool uploadGranted,
            out PortalOnlinePackageConfirmationTicket ticket,
            out string reason)
        {
            // <lang>
            //   <zh-CN>输出先置失败态，保证所有提前返回都不留下半签发票据或未初始化原因。</zh-CN>
            //   <en>Initialise outputs to the failure state so no early return leaves a half-issued ticket or an uninitialised reason.</en>
            // </lang>
            ticket = null;
            reason = string.Empty;

            // <lang>
            //   <zh-CN>权限键必须是注册表内已定义且精确的上传键：既防止用确认键冒签，也防止传入角色名或来源声明冒充权限。</zh-CN>
            //   <en>The key must be an exactly registered upload key: this prevents signing with the confirm key and blocks role names or origin claims from impersonating a permission.</en>
            // </lang>
            if (!PortalPermissionRegistry.IsDefined(uploadPermissionKey) ||
                !string.Equals(uploadPermissionKey, PortalPermissionKeys.ModuleOnlinePackageUpload, StringComparison.Ordinal))
            {
                reason = "Upload permission key is not the registered online-package upload key.";
                return false;
            }

            // <lang>
            //   <zh-CN>二次确认「页面层确实判定了权限」；不读取任何请求数据，权限结论只能由 EnsurePermission 传入。</zh-CN>
            //   <en>Require the page-layer verdict as well; no request data is read, and the verdict can only come from EnsurePermission.</en>
            // </lang>
            if (!uploadGranted)
            {
                reason = "Online package upload permission is not granted.";
                return false;
            }

            // <lang>
            //   <zh-CN>账号名、存储名与哈希都必须存在；空白账号会使票据失去「谁发起」的绑定。</zh-CN>
            //   <en>Actor, storage name, and hash must all be present; a blank actor would break the "who initiated" binding.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(actorName) || string.IsNullOrWhiteSpace(packageStorageName) ||
                string.IsNullOrWhiteSpace(packageHash))
            {
                reason = "Ticket binding input is missing.";
                return false;
            }

            // <lang>
            //   <zh-CN>哈希必须是定长十六进制，避免把任意文本（含伪造声明）当作内容摘要传入。</zh-CN>
            //   <en>The hash must be fixed-length hexadecimal so arbitrary text (including forged claims) cannot be passed off as a content digest.</en>
            // </lang>
            if (!IsHexSha256(packageHash))
            {
                reason = "Package hash is not a SHA-256 hex digest.";
                return false;
            }

            // <lang>
            //   <zh-CN>签发时间必须是 UTC 且不是默认值；时间由调用方提供，闸不读取系统时钟。</zh-CN>
            //   <en>The issue time must be UTC and not the default value; time is caller-supplied and the gate reads no system clock.</en>
            // </lang>
            if (issuedUtc.Kind != DateTimeKind.Utc || issuedUtc == default(DateTime))
            {
                reason = "Issue time must be a non-default UTC value.";
                return false;
            }

            // <lang>
            //   <zh-CN>Nonce 与防伪令牌分别独立生成，二者不可互相推导；使用 32 位十六进制无连字符 GUID。</zh-CN>
            //   <en>Nonce and anti-forgery token are generated independently, neither derivable from the other; both are 32-character hyphen-free GUIDs.</en>
            // </lang>
            string nonce = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            string antiForgeryToken = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            // <lang>
            //   <zh-CN>过期时间由固定有效期推导，不做自动续期；超时必须重新上传校验。</zh-CN>
            //   <en>Expiry derives from a fixed lifetime with no renewal; a timeout requires re-uploading and re-validating.</en>
            // </lang>
            DateTime expiresUtc = issuedUtc.AddMinutes(TicketLifetimeMinutes);

            // <lang>
            //   <zh-CN>构造不可变票据并返回；签发本身不写任何存储，持久化由页面层负责。</zh-CN>
            //   <en>Construct and return the immutable ticket; issuance writes nothing, and persistence belongs to the page layer.</en>
            // </lang>
            ticket = new PortalOnlinePackageConfirmationTicket(
                nonce, antiForgeryToken, actorName, packageStorageName, packageHash, issuedUtc, expiresUtc);
            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>在「确认安装」时校验票据并二次鉴权；通过时返回已消费票据，重复提交以幂等方式拒绝二次安装。</zh-CN>
        ///   <en>Validates the ticket and re-authorizes at "confirm install"; returns a consumed ticket on success, while a repeated submission is rejected idempotently.</en>
        /// </lang>
        /// </summary>
        /// <param name="ticket"><l zh-CN="页面层从持久化介质取回的票据，不可为 null。" en="Ticket retrieved by the page layer from its store; cannot be null." /></param>
        /// <param name="antiForgeryToken"><l zh-CN="表单隐藏字段回传的防伪同步令牌。" en="Anti-forgery synchroniser token returned by the hidden form field." /></param>
        /// <param name="actorName"><l zh-CN="当前已认证账号名，须与签发票据时一致。" en="Current authenticated account name; must match the issuing actor." /></param>
        /// <param name="currentPackageHash"><l zh-CN="确认时重新计算的暂存包哈希，用于检测包被替换。" en="Hash recomputed for the staged package at confirmation, detecting package substitution." /></param>
        /// <param name="confirmPermissionKey"><l zh-CN="页面层判定的确认权限键，必须是 Module.OnlinePackage.Confirm。" en="Confirm permission key judged by the page layer; must be Module.OnlinePackage.Confirm." /></param>
        /// <param name="confirmGranted"><l zh-CN="页面层已判定的确认权限结果；false 一律拒绝。" en="Page-layer confirm permission verdict; false is always rejected." /></param>
        /// <param name="confirmedUtc"><l zh-CN="确认时间（UTC），由调用方提供。" en="Confirmation time (UTC), supplied by the caller." /></param>
        /// <param name="consumedTicket"><l zh-CN="首次成功时为已消费票据；幂等重放时返回原已消费票据；失败时为 null。" en="Consumed ticket on first success, the original consumed ticket on idempotent replay, null on failure." /></param>
        /// <param name="replayed"><l zh-CN="true 表示这是已消费票据的幂等重放，调用方不得再次执行安装。" en="True indicates an idempotent replay of a consumed ticket; callers must not install again." /></param>
        /// <param name="reason"><l zh-CN="拒绝原因的稳定短语，不回显票据或令牌内容。" en="Stable rejection phrase that never echoes ticket or token content." /></param>
        /// <returns><l zh-CN="允许确认（首次或幂等重放）时为 true；任何拒绝为 false。" en="True when confirmation is allowed (first use or idempotent replay); false on any rejection." /></returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>判定顺序刻意固定：票据存在 → 时间 → 防伪令牌 → 账号 → 包哈希 → 权限键 → 权限结论 → 幂等消费。
        ///   每一步都在「未产生副作用」前完成，且失败时不改变票据、不消费、不安装。
        ///   **幂等语义**：已消费票据在通过全部校验后返回 `replayed=true`，调用方据此跳过安装动作，而不是报错或重复安装。</zh-CN>
        ///   <en>The order is deliberately fixed: ticket present → time → anti-forgery token → actor → package hash → permission key → verdict → idempotent consumption.
        ///   Every step completes before any side effect, and a failure neither mutates the ticket, consumes it, nor installs anything.
        ///   **Idempotency semantics**: a consumed ticket that passes every check returns `replayed=true`, so callers skip installation rather than erroring or installing twice.</en>
        /// </lang>
        /// </remarks>
        public static bool TryConfirm(
            PortalOnlinePackageConfirmationTicket ticket,
            string antiForgeryToken,
            string actorName,
            string currentPackageHash,
            string confirmPermissionKey,
            bool confirmGranted,
            DateTime confirmedUtc,
            out PortalOnlinePackageConfirmationTicket consumedTicket,
            out bool replayed,
            out string reason)
        {
            // <lang>
            //   <zh-CN>输出先置失败态，保证所有提前返回都不留下半消费票据或未初始化标志。</zh-CN>
            //   <en>Initialise outputs to the failure state so no early return leaves a half-consumed ticket or uninitialised flags.</en>
            // </lang>
            consumedTicket = null;
            replayed = false;
            reason = string.Empty;

            // <lang>
            //   <zh-CN>票据缺失即拒绝；闸不持有存储，无法替页面层「找回」票据。</zh-CN>
            //   <en>A missing ticket is rejected; the gate holds no store and cannot "recover" one for the page layer.</en>
            // </lang>
            if (ticket == null)
            {
                reason = "Confirmation ticket is missing.";
                return false;
            }

            // <lang>
            //   <zh-CN>确认时间必须是 UTC 且非默认值；不早于签发时间（防时钟回拨伪造），不晚于过期时间。</zh-CN>
            //   <en>Confirmation time must be UTC and non-default; not before issuance (clock-rollback forgery) and not after expiry.</en>
            // </lang>
            if (confirmedUtc.Kind != DateTimeKind.Utc || confirmedUtc == default(DateTime))
            {
                reason = "Confirmation time must be a non-default UTC value.";
                return false;
            }
            if (confirmedUtc < ticket.IssuedUtc)
            {
                reason = "Confirmation time precedes ticket issuance.";
                return false;
            }
            if (confirmedUtc > ticket.ExpiresUtc)
            {
                reason = "Confirmation ticket has expired; re-upload and re-validate the package.";
                return false;
            }

            // <lang>
            //   <zh-CN>防伪同步令牌用固定时间比较，避免通过响应时延逐字节猜测令牌（CSRF 同步令牌模式）。</zh-CN>
            //   <en>Compare the anti-forgery token in constant time so response timing cannot leak it byte by byte (CSRF synchroniser pattern).</en>
            // </lang>
            if (!FixedTimeEquals(ticket.AntiForgeryToken, antiForgeryToken))
            {
                reason = "Anti-forgery token does not match.";
                return false;
            }

            // <lang>
            //   <zh-CN>账号绑定：换人确认即拒绝，防止票据被另一会话或另一账号截获使用。</zh-CN>
            //   <en>Actor binding: confirmation by a different account is rejected, so a ticket cannot be used by another session or identity.</en>
            // </lang>
            if (!string.Equals(ticket.ActorName, actorName, StringComparison.Ordinal))
            {
                reason = "Confirmation actor does not match the issuing actor.";
                return false;
            }

            // <lang>
            //   <zh-CN>包哈希绑定：确认时重算哈希并精确比对，防止在暂存区把已校验包换成另一个包（TOCTOU）。</zh-CN>
            //   <en>Package-hash binding: recompute and compare exactly at confirmation, preventing a validated package from being swapped in staging (TOCTOU).</en>
            // </lang>
            if (!IsHexSha256(currentPackageHash) ||
                !FixedTimeEquals(ticket.PackageHash, currentPackageHash))
            {
                reason = "Staged package hash no longer matches the validated package.";
                return false;
            }

            // <lang>
            //   <zh-CN>权限键必须是注册表内已定义且精确的确认键；不能用上传键、其它模块键或角色名代替二次鉴权。</zh-CN>
            //   <en>The key must be the exactly registered confirm key; the upload key, other module keys, or role names cannot substitute for re-authorization.</en>
            // </lang>
            if (!PortalPermissionRegistry.IsDefined(confirmPermissionKey) ||
                !string.Equals(confirmPermissionKey, PortalPermissionKeys.ModuleOnlinePackageConfirm, StringComparison.Ordinal))
            {
                reason = "Confirm permission key is not the registered online-package confirm key.";
                return false;
            }

            // <lang>
            //   <zh-CN>二次鉴权：即便上传阶段已授权，确认阶段仍须重新判定且结果为允许。</zh-CN>
            //   <en>Re-authorization: even if upload was authorized, confirmation must be judged again and granted.</en>
            // </lang>
            if (!confirmGranted)
            {
                reason = "Online package confirm permission is not granted.";
                return false;
            }

            // <lang>
            //   <zh-CN>幂等重放：票据已消费，且上述校验全部通过 → 返回原票据并标记 replayed，调用方不得再次安装。</zh-CN>
            //   <en>Idempotent replay: the ticket is already consumed and every check passed → return the original ticket flagged as replayed; callers must not install again.</en>
            // </lang>
            if (ticket.ConsumedUtc.HasValue)
            {
                consumedTicket = ticket;
                replayed = true;
                return true;
            }

            // <lang>
            //   <zh-CN>首次确认：返回仅消费标记不同的新票据；持久化新状态由页面层完成，闸本身不写存储。</zh-CN>
            //   <en>First confirmation: return a new ticket differing only in the consumption marker; persisting the new state is the page layer's job, the gate writes no store.</en>
            // </lang>
            consumedTicket = ticket.WithConsumed(confirmedUtc);
            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断文本是否为定长 64 位十六进制 SHA-256 摘要。</zh-CN>
        ///   <en>Determines whether text is a fixed-length 64-character hexadecimal SHA-256 digest.</en>
        /// </lang>
        /// </summary>
        /// <param name="value"><l zh-CN="待判定的文本，可为 null。" en="Text to check; may be null." /></param>
        /// <returns><l zh-CN="长度与字符集合规时为 true。" en="True when length and character set comply." /></returns>
        private static bool IsHexSha256(string value)
        {
            // <lang>
            //   <zh-CN>长度不符直接拒绝，不做补全或 Trim，避免把形似摘要的文本洗成合法值。</zh-CN>
            //   <en>Reject a wrong length outright without padding or trimming, so digest-like text is never washed into a valid value.</en>
            // </lang>
            if (string.IsNullOrEmpty(value) || value.Length != PackageHashHexLength)
            {
                return false;
            }

            // <lang>
            //   <zh-CN>逐字符限定在 0-9 / a-f / A-F；其它字符（含冒号、空白、Unicode 同形字符）一律拒绝。</zh-CN>
            //   <en>Restrict every character to 0-9 / a-f / A-F; anything else (colons, whitespace, Unicode look-alikes) is rejected.</en>
            // </lang>
            foreach (char character in value)
            {
                // <lang>
                //   <zh-CN>命中集合外字符即整体失败，不继续扫描。</zh-CN>
                //   <en>A character outside the set fails the whole value immediately.</en>
                // </lang>
                bool isHex = (character >= '0' && character <= '9') ||
                             (character >= 'a' && character <= 'f') ||
                             (character >= 'A' && character <= 'F');
                if (!isHex)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>对两个字符串做固定时间比较：长度不同也仍遍历较长者，避免时序侧信道泄露令牌前缀。</zh-CN>
        ///   <en>Constant-time comparison of two strings: differing lengths still iterate over the longer one, so timing cannot leak a token prefix.</en>
        /// </lang>
        /// </summary>
        /// <param name="expected"><l zh-CN="服务端保存的期望值。" en="Server-held expected value." /></param>
        /// <param name="actual"><l zh-CN="请求回传的实际值，可为 null。" en="Caller-supplied actual value; may be null." /></param>
        /// <returns><l zh-CN="两者完全相同且均非 null 时为 true。" en="True when both are non-null and exactly equal." /></returns>
        private static bool FixedTimeEquals(string expected, string actual)
        {
            // <lang>
            //   <zh-CN>任一为 null 即不相等；仍走一次空值路径，避免为 null 建立提前返回的时序分支。</zh-CN>
            //   <en>Null on either side means unequal, but the empty path is still taken so null does not create a distinguishable early-return timing branch.</en>
            // </lang>
            if (expected == null || actual == null)
            {
                return false;
            }

            // <lang>
            //   <zh-CN>取较长者长度作为遍历上界，使比较耗时只取决于长度上界而非首个差异位置。</zh-CN>
            //   <en>Use the longer length as the iteration bound, so cost depends on that bound rather than the first differing position.</en>
            // </lang>
            int length = Math.Max(expected.Length, actual.Length);

            // <lang>
            //   <zh-CN>累积差异位：长度不同时以 0 参与，但仍保持循环次数一致，最终结果必不为 0。</zh-CN>
            //   <en>Accumulate differing bits; out-of-range positions contribute 0 yet keep the loop count identical, so the result is necessarily non-zero for unequal lengths.</en>
            // </lang>
            int difference = expected.Length ^ actual.Length;
            for (int index = 0; index < length; index++)
            {
                // <lang>
                //   <zh-CN>越界索引以 0 代替字符，不提前退出；异或累计保证任何差异都不会被后续位抵消。</zh-CN>
                //   <en>Out-of-range indices substitute 0 without exiting early; XOR accumulation ensures no difference can be cancelled by later bits.</en>
                // </lang>
                char left = index < expected.Length ? expected[index] : '\0';
                char right = index < actual.Length ? actual[index] : '\0';
                difference |= left ^ right;
            }

            return difference == 0;
        }
    }
}
