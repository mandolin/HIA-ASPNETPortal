using System;
using System.Collections.Generic;
using System.Text;
using ASPNET.StarterKit.Portal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// <lang>
///   <zh-CN>在线能力包回滚演练与故障隔离测试：快照形状校验、零残留逐字节一致判定、
///   三种演练情形的如实结论，以及单模块/单上传失败不扩散为整页失败的隔离判定。</zh-CN>
///   <en>Tests for online capability package rollback drills and failure isolation: snapshot shape validation,
///   byte-identical zero-residue decisions, honest verdicts for the three drill scenarios, and isolation ensuring
///   one failed module/upload never escalates into a whole-page failure.</en>
/// </lang>
/// </summary>
[TestClass]
public class PortalOnlinePackageRollbackTests
{
    /// <summary><lang><zh-CN>固定 UTC 时间，使快照与证据可复现。</zh-CN><en>Fixed UTC time so snapshots and evidence are reproducible.</en></lang></summary>
    private static readonly DateTime FixedUtc = new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);

    /// <summary><lang><zh-CN>构造一条合法清单条目，摘要取自真实 SHA-256 计算。</zh-CN><en>Builds a valid manifest entry whose digest comes from a real SHA-256 computation.</en></lang></summary>
    private static PortalOnlinePackageSnapshotEntry Entry(string path, string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        return new PortalOnlinePackageSnapshotEntry(
            path, bytes.Length, PortalOnlinePackageIntegrity.ComputeSha256Hex(bytes));
    }

    /// <summary><lang><zh-CN>构造一份含三个文件的基线快照。</zh-CN><en>Builds a three-file baseline snapshot.</en></lang></summary>
    private static PortalOnlinePackageSnapshot Baseline()
    {
        return new PortalOnlinePackageSnapshot(new List<PortalOnlinePackageSnapshotEntry>
        {
            Entry("module.json", "{\"schemaVersion\":2}"),
            Entry("Styles.css", "body { color: inherit; }"),
            Entry("Widget.ascx", "<%-- fixture --%>")
        }, FixedUtc);
    }

    /// <summary><lang><zh-CN>返回与基线逐字节一致的还原后清单副本。</zh-CN><en>Returns a restored manifest copy byte-identical to the baseline.</en></lang></summary>
    private static IList<PortalOnlinePackageSnapshotEntry> Restored()
    {
        return new List<PortalOnlinePackageSnapshotEntry>
        {
            Entry("module.json", "{\"schemaVersion\":2}"),
            Entry("Styles.css", "body { color: inherit; }"),
            Entry("Widget.ascx", "<%-- fixture --%>")
        };
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>合法快照通过校验；条目只读、创建时间原样保留。</zh-CN>
    ///   <en>A well-formed snapshot validates; entries are read-only and the creation time is preserved.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void Snapshot_ValidBaselinePasses()
    {
        PortalOnlinePackageSnapshot snapshot = Baseline();
        string reason;

        Assert.IsTrue(PortalOnlinePackageRollbackDrill.TryValidateSnapshot(snapshot, out reason), reason);
        Assert.AreEqual(string.Empty, reason);
        Assert.AreEqual(3, snapshot.Entries.Count);
        Assert.AreEqual(FixedUtc, snapshot.CreatedUtc);
        Assert.IsTrue(snapshot.Entries.IsReadOnly, "快照清单应只读，防调用方改动基线");
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>快照形状拒真：null / 空清单 / 非法路径（冒号、父级跳转、绝对路径）/ 负长度 /
    ///   非法摘要 / 重复路径一律拒绝，不把畸形快照当作可还原依据。</zh-CN>
    ///   <en>Snapshot shape rejection: null, empty manifest, illegal paths (colon, parent traversal, rooted),
    ///   negative length, invalid digest, and duplicate paths all reject so a malformed snapshot is never a restore basis.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void Snapshot_RejectsMalformedManifests()
    {
        string reason;
        Assert.IsFalse(PortalOnlinePackageRollbackDrill.TryValidateSnapshot(null, out reason));
        Assert.IsFalse(PortalOnlinePackageRollbackDrill.TryValidateSnapshot(
            new PortalOnlinePackageSnapshot(null, FixedUtc), out reason));
        Assert.IsFalse(PortalOnlinePackageRollbackDrill.TryValidateSnapshot(
            new PortalOnlinePackageSnapshot(new List<PortalOnlinePackageSnapshotEntry>(), FixedUtc), out reason));

        string[] badPaths = { "C:\\evil.txt", "dir/../evil.txt", "/rooted.txt", "\\rooted.txt", "  ", "" };
        foreach (string badPath in badPaths)
        {
            PortalOnlinePackageSnapshot bad = new PortalOnlinePackageSnapshot(
                new List<PortalOnlinePackageSnapshotEntry> { Entry(badPath, "x") }, FixedUtc);
            Assert.IsFalse(PortalOnlinePackageRollbackDrill.TryValidateSnapshot(bad, out reason), "应拒绝路径: " + badPath);
            Assert.IsFalse(string.IsNullOrEmpty(reason));
        }

        PortalOnlinePackageSnapshot negativeLength = new PortalOnlinePackageSnapshot(
            new List<PortalOnlinePackageSnapshotEntry>
            {
                new PortalOnlinePackageSnapshotEntry("a.txt", -1L, new string('a', 64))
            }, FixedUtc);
        Assert.IsFalse(PortalOnlinePackageRollbackDrill.TryValidateSnapshot(negativeLength, out reason));

        PortalOnlinePackageSnapshot badHash = new PortalOnlinePackageSnapshot(
            new List<PortalOnlinePackageSnapshotEntry>
            {
                new PortalOnlinePackageSnapshotEntry("a.txt", 1L, "short")
            }, FixedUtc);
        Assert.IsFalse(PortalOnlinePackageRollbackDrill.TryValidateSnapshot(badHash, out reason));

        PortalOnlinePackageSnapshot duplicate = new PortalOnlinePackageSnapshot(
            new List<PortalOnlinePackageSnapshotEntry> { Entry("a.txt", "x"), Entry("a.txt", "y") }, FixedUtc);
        Assert.IsFalse(PortalOnlinePackageRollbackDrill.TryValidateSnapshot(duplicate, out reason));
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>零残留判定：逐字节一致（相同路径/长度/摘要）通过；大小写不同的摘要仍视为一致。</zh-CN>
    ///   <en>Zero-residue decision: a byte-identical manifest (same path/length/digest) passes; digest casing differences still compare equal.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void ZeroResidue_ByteIdenticalManifestPasses()
    {
        string reason;
        Assert.IsTrue(PortalOnlinePackageRollbackDrill.TryVerifyZeroResidue(Baseline(), Restored(), out reason), reason);
        Assert.AreEqual(string.Empty, reason);

        IList<PortalOnlinePackageSnapshotEntry> upper = Restored();
        foreach (PortalOnlinePackageSnapshotEntry entry in upper)
        {
            // <lang>
            //   <zh-CN>摘要大小写不影响安全结论，构造一条大写摘要条目验证 OrdinalIgnoreCase。</zh-CN>
            //   <en>Digest casing must not change the verdict; build one uppercase-digest entry to verify OrdinalIgnoreCase.</en>
            // </lang>
            PortalOnlinePackageSnapshotEntry mixed = new PortalOnlinePackageSnapshotEntry(
                "module.json", entry.ByteLength, entry.Sha256Hex.ToUpperInvariant());
            upper[0] = mixed;
            break;
        }
        Assert.IsTrue(PortalOnlinePackageRollbackDrill.TryVerifyZeroResidue(Baseline(), upper, out reason), reason);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>零残留拒真：多余文件（残留）、丢失文件、内容被改、长度被改、实际清单缺失、
    ///   重复路径、非法基线一律拒绝，且拒绝原因稳定、不回显内容。</zh-CN>
    ///   <en>Zero-residue rejects: an extra file (residue), a lost file, changed content, changed length, a missing
    ///   actual manifest, duplicate paths, and an invalid baseline; rejection reasons are stable and echo no content.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void ZeroResidue_RejectsResidueLossAndDivergence()
    {
        string reason;

        IList<PortalOnlinePackageSnapshotEntry> withResidue = Restored();
        withResidue.Add(Entry("residue.txt", "residue"));
        Assert.IsFalse(PortalOnlinePackageRollbackDrill.TryVerifyZeroResidue(Baseline(), withResidue, out reason));
        Assert.IsFalse(string.IsNullOrEmpty(reason));

        IList<PortalOnlinePackageSnapshotEntry> lostFile = Restored();
        lostFile.RemoveAt(2);
        Assert.IsFalse(PortalOnlinePackageRollbackDrill.TryVerifyZeroResidue(Baseline(), lostFile, out reason));

        IList<PortalOnlinePackageSnapshotEntry> changedContent = Restored();
        changedContent[1] = Entry("Styles.css", "body { color: red; }");
        Assert.IsFalse(PortalOnlinePackageRollbackDrill.TryVerifyZeroResidue(Baseline(), changedContent, out reason));

        IList<PortalOnlinePackageSnapshotEntry> changedLength = Restored();
        changedLength[0] = new PortalOnlinePackageSnapshotEntry(
            "module.json", 999L, changedLength[0].Sha256Hex);
        Assert.IsFalse(PortalOnlinePackageRollbackDrill.TryVerifyZeroResidue(Baseline(), changedLength, out reason));

        IList<PortalOnlinePackageSnapshotEntry> invalidHash = Restored();
        invalidHash[0] = new PortalOnlinePackageSnapshotEntry("module.json", 18L, "short");
        Assert.IsFalse(PortalOnlinePackageRollbackDrill.TryVerifyZeroResidue(Baseline(), invalidHash, out reason));

        IList<PortalOnlinePackageSnapshotEntry> duplicate = Restored();
        duplicate[2] = duplicate[0];
        Assert.IsFalse(PortalOnlinePackageRollbackDrill.TryVerifyZeroResidue(Baseline(), duplicate, out reason));

        Assert.IsFalse(PortalOnlinePackageRollbackDrill.TryVerifyZeroResidue(Baseline(), null, out reason));
        Assert.IsFalse(PortalOnlinePackageRollbackDrill.TryVerifyZeroResidue(null, Restored(), out reason));
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>三种演练情形全部如实返回「拒绝继续安装」，但各自保留可区分的稳定原因；
    ///   枚举外伪造值也 fail-closed 拒绝。<c>InstallationContinued</c> 语义恒为 false。</zh-CN>
    ///   <en>All three drill scenarios honestly return "installation refused to continue", each with a distinct stable
    ///   reason; a forged out-of-enum value also fails closed. The `InstallationContinued` semantics stay false throughout.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void DrillScenarios_AllRefuseContinuationHonestly()
    {
        PortalOnlinePackageRollbackScenario[] scenarios =
        {
            PortalOnlinePackageRollbackScenario.ValidationFailed,
            PortalOnlinePackageRollbackScenario.BackupCorrupted,
            PortalOnlinePackageRollbackScenario.CheckpointRejected
        };

        ISet<string> reasons = new HashSet<string>(StringComparer.Ordinal);
        foreach (PortalOnlinePackageRollbackScenario scenario in scenarios)
        {
            string verdict;
            string reason;
            Assert.IsFalse(PortalOnlinePackageRollbackDrill.TryEvaluateScenario(scenario, out verdict, out reason));
            Assert.AreEqual(PortalOnlinePackageRollbackDrill.VerdictRejected, verdict);
            Assert.IsFalse(string.IsNullOrEmpty(reason));
            Assert.IsTrue(reasons.Add(reason), "三种情形的原因必须可区分");
        }

        string forgedVerdict;
        string forgedReason;
        Assert.IsFalse(PortalOnlinePackageRollbackDrill.TryEvaluateScenario(
            (PortalOnlinePackageRollbackScenario)999, out forgedVerdict, out forgedReason));
        Assert.AreEqual(PortalOnlinePackageRollbackDrill.VerdictRejected, forgedVerdict);
        Assert.IsTrue(forgedReason.Contains("Unknown"));

        Assert.AreEqual("restorable", PortalOnlinePackageRollbackDrill.VerdictRestorable);
        Assert.AreEqual("rejected", PortalOnlinePackageRollbackDrill.VerdictRejected);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>失败隔离：单项失败只进入失败清单，页面仍存活、其它项继续处理；
    ///   失败清单如实返回，绝不把失败伪装成成功。</zh-CN>
    ///   <en>Failure isolation: one failure only enters the failure list, the page survives, and other items keep processing;
    ///   the failure list is returned honestly and failures are never disguised as successes.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void FailureIsolation_SingleFailureDoesNotEscalate()
    {
        IList<string> ids = new List<string> { "mod-a", "mod-b", "mod-c" };
        IDictionary<string, bool> results = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            { "mod-a", true }, { "mod-b", false }, { "mod-c", true }
        };

        IList<string> failedIds;
        bool pageSurvived;
        Assert.IsTrue(PortalOnlinePackageFailureIsolation.TryAggregate(ids, results, out failedIds, out pageSurvived));
        Assert.IsTrue(pageSurvived, "单模块失败不得升级为整页失败");
        Assert.AreEqual(1, failedIds.Count);
        Assert.AreEqual("mod-b", failedIds[0]);

        // <lang>
        //   <zh-CN>全部成功时失败清单为空，页面存活。</zh-CN>
        //   <en>When all succeed the failure list is empty and the page survives.</en>
        // </lang>
        results["mod-b"] = true;
        Assert.IsTrue(PortalOnlinePackageFailureIsolation.TryAggregate(ids, results, out failedIds, out pageSurvived));
        Assert.IsTrue(pageSurvived);
        Assert.AreEqual(0, failedIds.Count);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>失败隔离的 fail-closed 与如实记录：映射缺失键按失败、空白标识计入失败、
    ///   全部失败仍判定页面存活（逐项隔离未升级为整页异常），但失败清单不得为空或伪造成功。</zh-CN>
    ///   <en>Fail-closed and honest reporting: a missing map key is a failure, blank ids are recorded as failures, and even
    ///   an all-failed batch keeps the page alive (failures stayed isolated), but the failure list is never empty or faked.</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void FailureIsolation_FailClosedAndHonestFailureList()
    {
        IList<string> ids = new List<string> { "mod-a", "mod-b", "mod-c" };
        IDictionary<string, bool> results = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            { "mod-a", true }, { "mod-b", true }
        };

        IList<string> failedIds;
        bool pageSurvived;
        Assert.IsTrue(PortalOnlinePackageFailureIsolation.TryAggregate(ids, results, out failedIds, out pageSurvived));
        Assert.IsTrue(pageSurvived);
        Assert.AreEqual(1, failedIds.Count, "缺失键必须按失败处理");
        Assert.AreEqual("mod-c", failedIds[0]);

        IList<string> allFailed = new List<string> { "mod-a", "mod-b" };
        IDictionary<string, bool> noneSucceeded = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            { "mod-a", false }, { "mod-b", false }
        };
        Assert.IsTrue(PortalOnlinePackageFailureIsolation.TryAggregate(
            allFailed, noneSucceeded, out failedIds, out pageSurvived));
        Assert.IsTrue(pageSurvived, "逐项隔离后页面未被整页异常终止");
        Assert.AreEqual(2, failedIds.Count, "失败清单必须如实完整");

        IList<string> withBlank = new List<string> { "mod-a", " " };
        IDictionary<string, bool> blankResults = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            { "mod-a", true }
        };
        Assert.IsTrue(PortalOnlinePackageFailureIsolation.TryAggregate(
            withBlank, blankResults, out failedIds, out pageSurvived));
        Assert.IsTrue(pageSurvived);
        Assert.AreEqual(1, failedIds.Count);
        Assert.AreEqual(string.Empty, failedIds[0]);
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>失败隔离拒绝非法输入：空批次、null 标识序列或 null 成功映射返回 false，
    ///   且页面不判定为存活（不把调用方错误解释为「全部成功」）。</zh-CN>
    ///   <en>Failure isolation rejects invalid input: an empty batch, a null id list, or a null success map returns false
    ///   with page survival false, so a caller error is never interpreted as "all succeeded".</en>
    /// </lang>
    /// </summary>
    [TestMethod]
    public void FailureIsolation_RejectsInvalidInput()
    {
        IList<string> failedIds;
        bool pageSurvived;
        IDictionary<string, bool> results = new Dictionary<string, bool>(StringComparer.Ordinal);

        Assert.IsFalse(PortalOnlinePackageFailureIsolation.TryAggregate(
            null, results, out failedIds, out pageSurvived));
        Assert.IsFalse(pageSurvived);
        Assert.IsNotNull(failedIds);
        Assert.AreEqual(0, failedIds.Count);

        Assert.IsFalse(PortalOnlinePackageFailureIsolation.TryAggregate(
            new List<string>(), results, out failedIds, out pageSurvived));
        Assert.IsFalse(pageSurvived);

        Assert.IsFalse(PortalOnlinePackageFailureIsolation.TryAggregate(
            new List<string> { "mod-a" }, null, out failedIds, out pageSurvived));
        Assert.IsFalse(pageSurvived);
    }
}
