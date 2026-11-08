using System;
using ASPNET.StarterKit.Portal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// <lang>
///   <zh-CN>在线能力包双步确认闸测试：签发前置条件、二次鉴权、防伪同步令牌、包哈希绑定、过期、防重放与幂等。</zh-CN>
///   <en>Tests for the two-step confirmation gate: issuance preconditions, re-authorization, anti-forgery token, package-hash binding, expiry, replay defence, and idempotency.</en>
/// </lang>
/// </summary>
[TestClass]
public class PortalOnlinePackageConfirmationTests
{
    /// <summary><lang><zh-CN>测试用合法 SHA-256 十六进制摘要（64 位 a）。</zh-CN><en>A valid test SHA-256 hex digest (64 a's).</en></lang></summary>
    private const string ValidHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    /// <summary><lang><zh-CN>与 ValidHash 不同的另一合法摘要（末位 b），用于模拟暂存包被替换。</zh-CN><en>Another valid digest differing from ValidHash (last char b), modelling a substituted staged package.</en></lang></summary>
    private const string OtherHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaab";

    /// <summary><lang><zh-CN>固定签发时间（UTC），使所有时间相关断言可复现。</zh-CN><en>Fixed UTC issue time so all time-related assertions are reproducible.</en></lang></summary>
    private static readonly DateTime Issued = new DateTime(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// <lang>
    ///   <zh-CN>签发一张合法票据并返回；失败时让测试以明确消息终止。</zh-CN>
    ///   <en>Issues one valid ticket and returns it; fails the test with a clear message when issuance fails.</en>
    /// </lang>
    /// </summary>
    private static PortalOnlinePackageConfirmationTicket IssueValid()
    {
        PortalOnlinePackageConfirmationTicket ticket;
        string reason;
        bool ok = PortalOnlinePackageConfirmationGate.TryIssue(
            "admin", "staged-1234.zip", ValidHash, Issued,
            PortalPermissionKeys.ModuleOnlinePackageUpload, true, out ticket, out reason);
        Assert.IsTrue(ok, reason);
        return ticket;
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>以合法参数确认一张票据，便于各测试只改动一个变量。</zh-CN>
    ///   <en>Confirms a ticket with valid parameters so each test varies exactly one input.</en>
    /// </lang>
    /// </summary>
    private static bool Confirm(
        PortalOnlinePackageConfirmationTicket ticket,
        out PortalOnlinePackageConfirmationTicket consumed,
        out bool replayed,
        out string reason)
    {
        return PortalOnlinePackageConfirmationGate.TryConfirm(
            ticket, ticket == null ? null : ticket.AntiForgeryToken, "admin", ValidHash,
            PortalPermissionKeys.ModuleOnlinePackageConfirm, true,
            Issued.AddMinutes(5), out consumed, out replayed, out reason);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>合法签发产生带独立 nonce 与防伪令牌的未消费票据，且绑定账号、存储名与哈希。</zh-CN>
    ///   <en>A valid issuance yields an unconsumed ticket with independent nonce and anti-forgery token, bound to actor, storage name, and hash.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryIssue_ProducesBoundUnconsumedTicket()
    {
        PortalOnlinePackageConfirmationTicket ticket = IssueValid();

        Assert.IsNotNull(ticket);
        Assert.AreEqual(32, ticket.Nonce.Length);
        Assert.AreEqual(32, ticket.AntiForgeryToken.Length);
        Assert.AreNotEqual(ticket.Nonce, ticket.AntiForgeryToken);
        Assert.AreEqual("admin", ticket.ActorName);
        Assert.AreEqual("staged-1234.zip", ticket.PackageStorageName);
        Assert.AreEqual(ValidHash, ticket.PackageHash);
        Assert.AreEqual(Issued, ticket.IssuedUtc);
        Assert.AreEqual(Issued.AddMinutes(PortalOnlinePackageConfirmationGate.TicketLifetimeMinutes), ticket.ExpiresUtc);
        Assert.IsFalse(ticket.ConsumedUtc.HasValue);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>签发要求精确的上传权限键且权限已授予：用确认键、其它键、未知键或未授权都拒绝，且不产生票据。</zh-CN>
    ///   <en>Issuance requires the exact upload key and a granted verdict: the confirm key, other keys, unknown keys, and a denied verdict are all rejected with no ticket.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryIssue_RejectsWrongOrMissingUploadPermission()
    {
        string[] badKeys =
        {
            PortalPermissionKeys.ModuleOnlinePackageConfirm,
            PortalPermissionKeys.ModuleCatalogEdit,
            "Module.OnlinePackage.Upload.Admin",
            "Admins",
            ""
        };

        foreach (string key in badKeys)
        {
            PortalOnlinePackageConfirmationTicket ticket;
            string reason;
            Assert.IsFalse(PortalOnlinePackageConfirmationGate.TryIssue(
                "admin", "staged-1234.zip", ValidHash, Issued, key, true, out ticket, out reason), "应拒绝键: " + key);
            Assert.IsNull(ticket);
            Assert.IsFalse(string.IsNullOrEmpty(reason));
        }

        PortalOnlinePackageConfirmationTicket denied;
        string deniedReason;
        Assert.IsFalse(PortalOnlinePackageConfirmationGate.TryIssue(
            "admin", "staged-1234.zip", ValidHash, Issued,
            PortalPermissionKeys.ModuleOnlinePackageUpload, false, out denied, out deniedReason));
        Assert.IsNull(denied);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>签发要求账号名/存储名/哈希齐备且哈希为定长十六进制，拒绝把伪造声明当作摘要。</zh-CN>
    ///   <en>Issuance requires actor, storage name, and a fixed-length hex hash; forged claims cannot be passed off as a digest.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryIssue_RejectsMissingBindingOrMalformedHash()
    {
        string[] badHashes = { "", "short", new string('a', 63), new string('a', 65), new string('g', 64), "declared-by-package:trusted" };
        foreach (string hash in badHashes)
        {
            PortalOnlinePackageConfirmationTicket ticket;
            string reason;
            Assert.IsFalse(PortalOnlinePackageConfirmationGate.TryIssue(
                "admin", "staged-1234.zip", hash, Issued,
                PortalPermissionKeys.ModuleOnlinePackageUpload, true, out ticket, out reason), "应拒绝哈希: " + hash);
            Assert.IsNull(ticket);
        }

        PortalOnlinePackageConfirmationTicket blankActor;
        string blankReason;
        Assert.IsFalse(PortalOnlinePackageConfirmationGate.TryIssue(
            "  ", "staged-1234.zip", ValidHash, Issued,
            PortalPermissionKeys.ModuleOnlinePackageUpload, true, out blankActor, out blankReason));
        Assert.IsNull(blankActor);

        PortalOnlinePackageConfirmationTicket localTime;
        string localReason;
        Assert.IsFalse(PortalOnlinePackageConfirmationGate.TryIssue(
            "admin", "staged-1234.zip", ValidHash, DateTime.Now,
            PortalPermissionKeys.ModuleOnlinePackageUpload, true, out localTime, out localReason));
        Assert.IsNull(localTime);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>合法确认返回已消费票据且 replayed=false；原票据保持不变（不可变值对象）。</zh-CN>
    ///   <en>A valid confirmation returns a consumed ticket with replayed=false while the original stays unchanged (immutable value object).</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryConfirm_FirstUseConsumesTicketWithoutMutatingOriginal()
    {
        PortalOnlinePackageConfirmationTicket ticket = IssueValid();

        PortalOnlinePackageConfirmationTicket consumed;
        bool replayed;
        string reason;
        Assert.IsTrue(Confirm(ticket, out consumed, out replayed, out reason), reason);
        Assert.IsFalse(replayed);
        Assert.IsNotNull(consumed);
        Assert.IsTrue(consumed.ConsumedUtc.HasValue);
        Assert.AreEqual(Issued.AddMinutes(5), consumed.ConsumedUtc.Value);
        Assert.AreEqual(ticket.Nonce, consumed.Nonce);
        Assert.AreEqual(ticket.PackageHash, consumed.PackageHash);
        Assert.IsFalse(ticket.ConsumedUtc.HasValue, "原票据不得被就地修改");
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>重复提交同一已消费票据以幂等方式返回 replayed=true，调用方据此跳过安装，不重复执行。</zh-CN>
    ///   <en>Re-submitting a consumed ticket returns replayed=true idempotently, so callers skip installation instead of executing twice.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryConfirm_ReplayIsIdempotent()
    {
        PortalOnlinePackageConfirmationTicket ticket = IssueValid();
        PortalOnlinePackageConfirmationTicket first;
        bool firstReplayed;
        string firstReason;
        Assert.IsTrue(Confirm(ticket, out first, out firstReplayed, out firstReason));

        PortalOnlinePackageConfirmationTicket second;
        bool secondReplayed;
        string secondReason;
        Assert.IsTrue(Confirm(first, out second, out secondReplayed, out secondReason), secondReason);
        Assert.IsTrue(secondReplayed);
        Assert.AreSame(first, second, "幂等重放应返回原已消费票据");
        Assert.AreEqual(first.ConsumedUtc, second.ConsumedUtc, "幂等重放不得改写消费时间");
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>无票据不能确认（防「上传即安装」被单步跳过）；闸不持有存储、无法替调用方找回票据。</zh-CN>
    ///   <en>Confirmation without a ticket is rejected (preventing a one-step "upload means installed" skip); the gate holds no store and recovers nothing.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryConfirm_RejectsMissingTicket()
    {
        PortalOnlinePackageConfirmationTicket consumed;
        bool replayed;
        string reason;
        Assert.IsFalse(Confirm(null, out consumed, out replayed, out reason));
        Assert.IsNull(consumed);
        Assert.IsFalse(replayed);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>防伪同步令牌不匹配、缺失或大小写不同都拒绝（CSRF 同步令牌模式）。</zh-CN>
    ///   <en>A missing, mismatched, or differently-cased anti-forgery token is rejected (CSRF synchroniser pattern).</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryConfirm_RejectsAntiForgeryMismatch()
    {
        PortalOnlinePackageConfirmationTicket ticket = IssueValid();
        string[] badTokens = { null, "", "forged", ticket.Nonce, ticket.AntiForgeryToken.ToUpperInvariant() };

        foreach (string token in badTokens)
        {
            PortalOnlinePackageConfirmationTicket consumed;
            bool replayed;
            string reason;
            Assert.IsFalse(PortalOnlinePackageConfirmationGate.TryConfirm(
                ticket, token, "admin", ValidHash,
                PortalPermissionKeys.ModuleOnlinePackageConfirm, true, Issued.AddMinutes(1),
                out consumed, out replayed, out reason), "应拒绝令牌: " + token);
            Assert.IsNull(consumed);
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>二次鉴权：确认键必须精确且已授予；用上传键、未知键、角色名或未授权都拒绝，且票据不被消费。</zh-CN>
    ///   <en>Re-authorization: the confirm key must be exact and granted; the upload key, unknown keys, role names, and a denied verdict are rejected without consuming the ticket.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryConfirm_RejectsWrongOrMissingConfirmPermission()
    {
        PortalOnlinePackageConfirmationTicket ticket = IssueValid();
        string[] badKeys =
        {
            PortalPermissionKeys.ModuleOnlinePackageUpload,
            PortalPermissionKeys.ModuleCatalogEdit,
            "Admins",
            "Module.OnlinePackage.GrantByOrigin",
            ""
        };

        foreach (string key in badKeys)
        {
            PortalOnlinePackageConfirmationTicket consumed;
            bool replayed;
            string reason;
            Assert.IsFalse(PortalOnlinePackageConfirmationGate.TryConfirm(
                ticket, ticket.AntiForgeryToken, "admin", ValidHash, key, true, Issued.AddMinutes(1),
                out consumed, out replayed, out reason), "应拒绝键: " + key);
            Assert.IsNull(consumed);
        }

        PortalOnlinePackageConfirmationTicket denied;
        bool deniedReplayed;
        string deniedReason;
        Assert.IsFalse(PortalOnlinePackageConfirmationGate.TryConfirm(
            ticket, ticket.AntiForgeryToken, "admin", ValidHash,
            PortalPermissionKeys.ModuleOnlinePackageConfirm, false, Issued.AddMinutes(1),
            out denied, out deniedReplayed, out deniedReason));
        Assert.IsNull(denied);
        Assert.IsFalse(ticket.ConsumedUtc.HasValue, "二次鉴权失败不得消费票据");
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>账号绑定：他人确认、大小写不同都拒绝，票据不被消费。</zh-CN>
    ///   <en>Actor binding: a different account or a case variant is rejected and the ticket is not consumed.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryConfirm_RejectsDifferentActor()
    {
        PortalOnlinePackageConfirmationTicket ticket = IssueValid();
        string[] badActors = { "other", "Admin", "", null };

        foreach (string actor in badActors)
        {
            PortalOnlinePackageConfirmationTicket consumed;
            bool replayed;
            string reason;
            Assert.IsFalse(PortalOnlinePackageConfirmationGate.TryConfirm(
                ticket, ticket.AntiForgeryToken, actor, ValidHash,
                PortalPermissionKeys.ModuleOnlinePackageConfirm, true, Issued.AddMinutes(1),
                out consumed, out replayed, out reason), "应拒绝账号: " + actor);
            Assert.IsNull(consumed);
        }
        Assert.IsFalse(ticket.ConsumedUtc.HasValue);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>包哈希绑定：暂存包被替换（TOCTOU）或哈希格式不合法都拒绝，票据不被消费。</zh-CN>
    ///   <en>Package-hash binding: a substituted staged package (TOCTOU) or a malformed hash is rejected without consuming the ticket.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryConfirm_RejectsSwappedOrMalformedPackageHash()
    {
        PortalOnlinePackageConfirmationTicket ticket = IssueValid();
        string[] badHashes = { OtherHash, ValidHash.ToUpperInvariant(), "", "short", new string('g', 64) };

        foreach (string hash in badHashes)
        {
            PortalOnlinePackageConfirmationTicket consumed;
            bool replayed;
            string reason;
            Assert.IsFalse(PortalOnlinePackageConfirmationGate.TryConfirm(
                ticket, ticket.AntiForgeryToken, "admin", hash,
                PortalPermissionKeys.ModuleOnlinePackageConfirm, true, Issued.AddMinutes(1),
                out consumed, out replayed, out reason), "应拒绝哈希: " + hash);
            Assert.IsNull(consumed);
        }
        Assert.IsFalse(ticket.ConsumedUtc.HasValue);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>时间判定：非 UTC、默认值、早于签发（时钟回拨）与超过 30 分钟有效期都拒绝。</zh-CN>
    ///   <en>Time checks: non-UTC, default, before issuance (clock rollback), and beyond the 30-minute lifetime are rejected.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void TryConfirm_RejectsInvalidOrExpiredTime()
    {
        PortalOnlinePackageConfirmationTicket ticket = IssueValid();
        DateTime[] badTimes =
        {
            Issued.AddMinutes(-1),
            Issued.AddMinutes(PortalOnlinePackageConfirmationGate.TicketLifetimeMinutes + 1),
            new DateTime(2026, 11, 8, 11, 0, 0, DateTimeKind.Local),
            default(DateTime)
        };

        foreach (DateTime time in badTimes)
        {
            PortalOnlinePackageConfirmationTicket consumed;
            bool replayed;
            string reason;
            Assert.IsFalse(PortalOnlinePackageConfirmationGate.TryConfirm(
                ticket, ticket.AntiForgeryToken, "admin", ValidHash,
                PortalPermissionKeys.ModuleOnlinePackageConfirm, true, time,
                out consumed, out replayed, out reason), "应拒绝时间: " + time.ToString("o"));
            Assert.IsNull(consumed);
        }

        // <lang>
        //   <zh-CN>恰好在有效期边界仍可确认（闭区间），确保边界不被误判为过期。</zh-CN>
        //   <en>The exact lifetime boundary is still valid (closed interval), so it is not mistaken for expiry.</en>
        // </lang>
        PortalOnlinePackageConfirmationTicket boundary;
        bool boundaryReplayed;
        string boundaryReason;
        Assert.IsTrue(PortalOnlinePackageConfirmationGate.TryConfirm(
            ticket, ticket.AntiForgeryToken, "admin", ValidHash,
            PortalPermissionKeys.ModuleOnlinePackageConfirm, true, ticket.ExpiresUtc,
            out boundary, out boundaryReplayed, out boundaryReason), boundaryReason);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>票据不承载来源/capability/角色结论：公开属性只有绑定字段与时间，且签发的两张票据 nonce 与令牌互不相同。</zh-CN>
    ///   <en>Tickets carry no origin/capability/role verdict: public properties are binding fields and time only, and two issued tickets have distinct nonce and token values.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void Ticket_HasNoClaimFieldsAndUsesFreshRandomness()
    {
        PortalOnlinePackageConfirmationTicket first = IssueValid();
        PortalOnlinePackageConfirmationTicket second = IssueValid();

        Assert.AreNotEqual(first.Nonce, second.Nonce);
        Assert.AreNotEqual(first.AntiForgeryToken, second.AntiForgeryToken);

        // <lang>
        //   <zh-CN>反射核对公开属性集合，防止后续接线时悄悄加入「来源声明即已授权」之类的危险字段。</zh-CN>
        //   <en>Reflect over public properties so a later change cannot quietly add a dangerous field such as "origin claim equals authorized".</en>
        // </lang>
        string[] names = Array.ConvertAll(
            typeof(PortalOnlinePackageConfirmationTicket).GetProperties(),
            property => property.Name);
        Array.Sort(names);
        string[] expected =
        {
            "ActorName", "AntiForgeryToken", "ConsumedUtc", "ExpiresUtc",
            "IssuedUtc", "Nonce", "PackageHash", "PackageStorageName"
        };
        CollectionAssert.AreEqual(expected, names);
    }
}
