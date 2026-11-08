namespace ASPNET.StarterKit.Portal
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// <lang>
    ///   <zh-CN>在线能力包回滚演练的三种情形。均为**演练**（证明快照可还原为零残留），不表示真实回滚或安装发生。</zh-CN>
    ///   <en>Three drill scenarios for online capability package rollback. All are **drills** (proving a snapshot restores with zero residue); none means a real rollback or installation occurred.</en>
    /// </lang>
    /// </summary>
    public enum PortalOnlinePackageRollbackScenario
    {
        /// <summary><lang><zh-CN>校验失败：安装前的完整性/签名/扫描判据未通过。</zh-CN><en>Validation failure: an integrity, signature, or scan criterion failed before installation.</en></lang></summary>
        ValidationFailed,

        /// <summary><lang><zh-CN>备份损坏：备份自身哈希与记录不符，禁止用它还原。</zh-CN><en>Backup corrupted: the backup hash differs from the record, so restoring from it is forbidden.</en></lang></summary>
        BackupCorrupted,

        /// <summary><lang><zh-CN>复核点未通过：封版前/部署前事件驱动复核点拒绝继续。</zh-CN><en>Checkpoint not passed: a pre-seal / pre-deployment event-driven checkpoint refused to continue.</en></lang></summary>
        CheckpointRejected
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>回滚演练中单个文件的哈希清单条目（不可变）：相对路径、字节长度、SHA-256 小写 hex 摘要。</zh-CN>
    ///   <en>One immutable hash-manifest entry for rollback drills: relative path, byte length, and lowercase SHA-256 hex digest.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalOnlinePackageSnapshotEntry
    {
        /// <summary><lang><zh-CN>相对隔离根目录的路径，用 '/' 统一分隔。</zh-CN><en>Path relative to the isolation root, using '/' as the uniform separator.</en></lang></summary>
        public string RelativePath { get; private set; }

        /// <summary><lang><zh-CN>文件字节长度。</zh-CN><en>File byte length.</en></lang></summary>
        public long ByteLength { get; private set; }

        /// <summary><lang><zh-CN>SHA-256 小写十六进制摘要。</zh-CN><en>Lowercase SHA-256 hex digest.</en></lang></summary>
        public string Sha256Hex { get; private set; }

        /// <summary>
        /// <lang><zh-CN>构造一条清单条目；调用方须保证三个字段已通过校验。</zh-CN><en>Constructs a manifest entry; callers must ensure all three fields passed validation.</en></lang>
        /// </summary>
        public PortalOnlinePackageSnapshotEntry(string relativePath, long byteLength, string sha256Hex)
        {
            // <lang>
            //   <zh-CN>逐项赋值，不做 Trim 或大小写折叠，保持与实际文件口径逐字一致。</zh-CN>
            //   <en>Assign each field without trimming or case folding, preserving exact agreement with the actual file.</en>
            // </lang>
            RelativePath = relativePath;
            ByteLength = byteLength;
            Sha256Hex = sha256Hex;
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>隔离暂存目录的受控快照：只记录相对路径/长度/哈希清单，不复制文件内容
    ///   （沿用 `Invoke-PortalModuleAssemblyCheck.ps1` 的哈希清单快照口径）。</zh-CN>
    ///   <en>Controlled snapshot of an isolated staging directory: records only relative paths, lengths, and hashes,
    ///   without copying file content (following the hash-manifest snapshot of `Invoke-PortalModuleAssemblyCheck.ps1`).</en>
    /// </lang>
    /// </summary>
    public sealed class PortalOnlinePackageSnapshot
    {
        /// <summary><lang><zh-CN>快照条目的只读集合。</zh-CN><en>Read-only collection of snapshot entries.</en></lang></summary>
        public IList<PortalOnlinePackageSnapshotEntry> Entries { get; private set; }

        /// <summary><lang><zh-CN>快照生成时间（UTC）。</zh-CN><en>Snapshot creation time (UTC).</en></lang></summary>
        public DateTime CreatedUtc { get; private set; }

        /// <summary>
        /// <lang><zh-CN>以已校验清单构造快照；内部复制为只读数组，避免共享可变来源。</zh-CN>
        /// <en>Constructs a snapshot from a validated manifest, copying into a read-only array so no mutable source is shared.</en>
        /// </lang>
        /// </summary>
        public PortalOnlinePackageSnapshot(IList<PortalOnlinePackageSnapshotEntry> entries, DateTime createdUtc)
        {
            // <lang>
            //   <zh-CN>null 清单按空快照处理而非抛异常，便于调用方统一走 TryXxx 路径；调用方负责保证条目内容合法。</zh-CN>
            //   <en>A null manifest becomes an empty snapshot rather than throwing, so callers uniformly follow the TryXxx path; callers ensure entry validity.</en>
            // </lang>
            Entries = new List<PortalOnlinePackageSnapshotEntry>(entries ?? new List<PortalOnlinePackageSnapshotEntry>()).AsReadOnly();
            CreatedUtc = createdUtc;
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>在线能力包回滚演练判定器（纯逻辑、无状态、不触文件 IO）：把「校验 → 备份 → 还原 → 零残留」的
    ///   **结论判定**与「实际文件操作」分离，使演练证据可被单测逐项验证，而不依赖真实站点或数据库。</zh-CN>
    ///   <en>Decision maker for online capability package rollback drills (pure, stateless, no file IO): separates the
    ///   **verdict** of "validate → backup → restore → zero residue" from actual file operations, so drill evidence is
    ///   unit-testable without a live site or database.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>证明边界（沿用 `C-anp-P48` / `W-anp-P272` 定稿口径）：演练只证明「受控快照可还原为与偏离前
    ///   逐字节一致且零残留」，**不表示真实回滚发生、不表示安装发生、不表示运行期装配态被切换**。
    ///   三种情形（校验失败 / 备份损坏 / 复核点未通过）各自的判定结果必须如实记录，不得把拒绝包装成成功。</zh-CN>
    ///   <en>Proof boundary (following the `C-anp-P48` / `W-anp-P272` ruling): a drill proves only that "the controlled
    ///   snapshot restores byte-identically to the pre-divergence state with zero residue"; it **does not** mean a real
    ///   rollback occurred, an installation occurred, or a runtime assembly mode was switched.
    ///   Each of the three scenarios (validation failed / backup corrupted / checkpoint rejected) must record its verdict
    ///   honestly; a rejection is never wrapped as success.</en>
    /// </lang>
    /// </remarks>
    public static class PortalOnlinePackageRollbackDrill
    {
        /// <summary><lang><zh-CN>校验通过标记的稳定短语，供证据 JSON 使用。</zh-CN><en>Stable phrase for a passed check, used in evidence JSON.</en></lang></summary>
        public const string VerdictRestorable = "restorable";

        /// <summary><lang><zh-CN>拒绝标记的稳定短语。</zh-CN><en>Stable phrase for a rejection.</en></lang></summary>
        public const string VerdictRejected = "rejected";

        /// <summary>
        /// <lang>
        ///   <zh-CN>校验一份快照是否可作为还原依据：条目非空、路径合法、长度非负、摘要为 64 位 hex，
        ///   且相对路径不重复（防同一文件被两条记录覆盖）。</zh-CN>
        ///   <en>Validates a snapshot as a restore basis: entries present, paths legal, lengths non-negative, digests 64-char hex,
        ///   and relative paths unique (so one file cannot be covered by two records).</en>
        /// </lang>
        /// </summary>
        /// <param name="snapshot"><l zh-CN="待校验快照。" en="Snapshot to validate." /></param>
        /// <param name="reason"><l zh-CN="拒绝原因的稳定短语。" en="Stable rejection phrase." /></param>
        /// <returns><l zh-CN="快照可作为还原依据时为 true。" en="True when the snapshot is a valid restore basis." /></returns>
        public static bool TryValidateSnapshot(PortalOnlinePackageSnapshot snapshot, out string reason)
        {
            // <lang>
            //   <zh-CN>输出先置失败态，保证提前返回不留下未初始化原因。</zh-CN>
            //   <en>Initialise the output to a failure state so no early return leaves an uninitialised reason.</en>
            // </lang>
            reason = string.Empty;

            // <lang>
            //   <zh-CN>快照或清单缺失一律拒绝，不把「没有记录」解释为「无需还原」。</zh-CN>
            //   <en>A missing snapshot or manifest rejects; "no records" is never interpreted as "nothing to restore".</en>
            // </lang>
            if (snapshot == null || snapshot.Entries == null || snapshot.Entries.Count == 0)
            {
                reason = "Rollback snapshot is missing or empty.";
                return false;
            }

            // <lang>
            //   <zh-CN>Ordinal 精确比较路径，避免大小写折叠导致两条记录指向同一文件却都被接受。</zh-CN>
            //   <en>Ordinal path comparison prevents two records pointing at the same file from both being accepted through case folding.</en>
            // </lang>
            ISet<string> seenPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (PortalOnlinePackageSnapshotEntry entry in snapshot.Entries)
            {
                // <lang>
                //   <zh-CN>条目为 null、路径空白、含冒号或含父级跳转片段都拒绝（与暂存条目名策略同向）。</zh-CN>
                //   <en>Reject null entries, blank paths, colons, or parent-traversal segments (aligned with the staging entry-name policy).</en>
                // </lang>
                if (entry == null || string.IsNullOrWhiteSpace(entry.RelativePath) ||
                    entry.RelativePath.IndexOf(':') >= 0 ||
                    entry.RelativePath.Contains(".."))
                {
                    reason = "Rollback snapshot contains an invalid path.";
                    return false;
                }

                // <lang>
                //   <zh-CN>绝对路径或反斜杠开头的路径不接受；清单统一用 '/' 相对路径。</zh-CN>
                //   <en>Rooted paths or backslash-leading paths are refused; manifests use '/' relative paths only.</en>
                // </lang>
                if (entry.RelativePath.StartsWith("/", StringComparison.Ordinal) ||
                    entry.RelativePath.StartsWith("\\", StringComparison.Ordinal))
                {
                    reason = "Rollback snapshot path must be relative.";
                    return false;
                }

                // <lang>
                //   <zh-CN>长度非负、摘要必须是合法 SHA-256 hex；任一不符即整份快照不可用。</zh-CN>
                //   <en>Length must be non-negative and the digest a valid SHA-256 hex; any mismatch invalidates the whole snapshot.</en>
                // </lang>
                if (entry.ByteLength < 0L || !PortalOnlinePackageIntegrity.IsSha256Hex(entry.Sha256Hex))
                {
                    reason = "Rollback snapshot entry has an invalid length or hash.";
                    return false;
                }

                // <lang>
                //   <zh-CN>重复路径会使「零残留」结论失真，直接拒绝。</zh-CN>
                //   <en>A duplicate path would invalidate the zero-residue verdict, so reject it.</en>
                // </lang>
                if (!seenPaths.Add(entry.RelativePath))
                {
                    reason = "Rollback snapshot contains duplicate paths.";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判定一次还原是否达到「字节级一致、零残留」：调用方传入还原后的实际清单，
        ///   本方法逐条比对路径/长度/哈希，并断言没有多余文件。</zh-CN>
        ///   <en>Decides whether a restore achieved "byte-identical, zero residue": callers supply the post-restore
        ///   actual manifest, and this method compares path/length/hash per entry and asserts no extra files exist.</en>
        /// </lang>
        /// </summary>
        /// <param name="expected"><l zh-CN="偏离前的基线快照。" en="Pre-divergence baseline snapshot." /></param>
        /// <param name="actual"><l zh-CN="还原后的实际清单。" en="Actual manifest after restore." /></param>
        /// <param name="reason"><l zh-CN="不一致原因的稳定短语。" en="Stable mismatch phrase." /></param>
        /// <returns><l zh-CN="逐字节一致且无残留时为 true。" en="True when byte-identical with no residue." /></returns>
        public static bool TryVerifyZeroResidue(
            PortalOnlinePackageSnapshot expected,
            IList<PortalOnlinePackageSnapshotEntry> actual,
            out string reason)
        {
            // <lang>
            //   <zh-CN>输出先置失败态。</zh-CN>
            //   <en>Initialise the output to a failure state.</en>
            // </lang>
            reason = string.Empty;

            // <lang>
            //   <zh-CN>先校验基线快照本身，避免用无效基线得出「一致」的结论。</zh-CN>
            //   <en>Validate the baseline snapshot first so an invalid baseline cannot yield an "identical" verdict.</en>
            // </lang>
            string snapshotReason;
            if (!TryValidateSnapshot(expected, out snapshotReason))
            {
                reason = snapshotReason;
                return false;
            }

            // <lang>
            //   <zh-CN>实际清单缺失即拒绝，不解释为「空目录恰好零残留」。</zh-CN>
            //   <en>A missing actual manifest rejects; it is not interpreted as "an empty directory happens to have zero residue".</en>
            // </lang>
            if (actual == null)
            {
                reason = "Post-restore manifest is missing.";
                return false;
            }

            // <lang>
            //   <zh-CN>条目数必须相等：多出的文件即为残留，少掉的即为丢失，二者都不允许。</zh-CN>
            //   <en>Entry counts must match: an extra file is residue and a missing one is loss; neither is allowed.</en>
            // </lang>
            if (actual.Count != expected.Entries.Count)
            {
                reason = string.Format(CultureInfo.InvariantCulture,
                    "Post-restore file count {0} does not match baseline {1}.",
                    actual.Count, expected.Entries.Count);
                return false;
            }

            // <lang>
            //   <zh-CN>把实际清单按路径建索引；重复路径直接判为不一致，防止覆盖式比对。</zh-CN>
            //   <en>Index the actual manifest by path; duplicate paths are a mismatch so comparison can never be overwritten.</en>
            // </lang>
            IDictionary<string, PortalOnlinePackageSnapshotEntry> actualByPath =
                new Dictionary<string, PortalOnlinePackageSnapshotEntry>(StringComparer.Ordinal);
            foreach (PortalOnlinePackageSnapshotEntry entry in actual)
            {
                // <lang>
                //   <zh-CN>null 条目或空白路径按不一致处理，不抛异常到演练脚本。</zh-CN>
                //   <en>A null entry or blank path is a mismatch rather than an exception thrown to the drill script.</en>
                // </lang>
                if (entry == null || string.IsNullOrWhiteSpace(entry.RelativePath) ||
                    actualByPath.ContainsKey(entry.RelativePath))
                {
                    reason = "Post-restore manifest is malformed or has duplicate paths.";
                    return false;
                }
                actualByPath.Add(entry.RelativePath, entry);
            }

            // <lang>
            //   <zh-CN>逐条基线比对：路径必须存在，长度相等，摘要忽略大小写精确一致。</zh-CN>
            //   <en>Compare each baseline entry: the path must exist, length must be equal, and digests must match exactly ignoring case.</en>
            // </lang>
            foreach (PortalOnlinePackageSnapshotEntry baseline in expected.Entries)
            {
                // <lang>
                //   <zh-CN>承接与基线路径对应的还原后条目；缺失即代表还原丢文件。</zh-CN>
                //   <en>Holds the restored entry matching a baseline path; absence means restore lost a file.</en>
                // </lang>
                PortalOnlinePackageSnapshotEntry restored;
                if (!actualByPath.TryGetValue(baseline.RelativePath, out restored))
                {
                    reason = "A baseline file is missing after restore.";
                    return false;
                }

                // <lang>
                //   <zh-CN>长度不同或摘要不同都判定为未达到字节级一致。</zh-CN>
                //   <en>A differing length or digest means byte-level identity was not achieved.</en>
                // </lang>
                if (restored.ByteLength != baseline.ByteLength ||
                    !PortalOnlinePackageIntegrity.IsSha256Hex(restored.Sha256Hex) ||
                    !string.Equals(restored.Sha256Hex, baseline.Sha256Hex, StringComparison.OrdinalIgnoreCase))
                {
                    reason = "A restored file is not byte-identical to the baseline.";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按三种演练情形判定「是否允许继续安装」，并给出稳定的结论短语。
        ///   三种情形全部返回「拒绝继续」，但各自保留独立原因，便于证据区分「为什么没继续」。</zh-CN>
        ///   <en>Decides whether installation may continue for each of the three drill scenarios and returns stable verdict phrases.
        ///   All three refuse to continue, each with a distinct reason so evidence distinguishes "why it stopped".</en>
        /// </lang>
        /// </summary>
        /// <param name="scenario"><l zh-CN="演练情形。" en="Drill scenario." /></param>
        /// <param name="verdict"><l zh-CN="稳定结论短语：rejected 或 restorable。" en="Stable verdict: rejected or restorable." /></param>
        /// <param name="reason"><l zh-CN="该情形的拒绝原因。" en="Rejection reason for the scenario." /></param>
        /// <returns><l zh-CN="允许继续安装时为 true；三种演练情形恒为 false。" en="True when installation may continue; always false for the three drill scenarios." /></returns>
        public static bool TryEvaluateScenario(
            PortalOnlinePackageRollbackScenario scenario,
            out string verdict,
            out string reason)
        {
            // <lang>
            //   <zh-CN>默认结论为拒绝；只有显式传入「无故障」才会由调用方另行处理，本判定器不伪造放行。</zh-CN>
            //   <en>The default verdict is rejection; only an explicit "no fault" path (handled elsewhere) could allow, and this evaluator never fabricates an allow.</en>
            // </lang>
            verdict = VerdictRejected;

            // <lang>
            //   <zh-CN>按情形给出可区分的稳定原因，全部拒绝继续；不把演练结果写成「安装已发生」。</zh-CN>
            //   <en>Give a distinct stable reason per scenario, all refusing to continue; never describe a drill as "installation happened".</en>
            // </lang>
            switch (scenario)
            {
                case PortalOnlinePackageRollbackScenario.ValidationFailed:
                    reason = "Validation failed; installation was not started and the staged package stays isolated.";
                    break;
                case PortalOnlinePackageRollbackScenario.BackupCorrupted:
                    reason = "Backup is corrupted; restoring from it is forbidden and installation was not started.";
                    break;
                case PortalOnlinePackageRollbackScenario.CheckpointRejected:
                    reason = "Event-driven checkpoint rejected continuation; installation was not started.";
                    break;
                default:
                    // <lang>
                    //   <zh-CN>枚举外的伪造值也按拒绝处理，不因未知输入放行（fail-closed）。</zh-CN>
                    //   <en>Forged out-of-enum values also reject, so unknown input never allows (fail-closed).</en>
                    // </lang>
                    reason = "Unknown rollback drill scenario; rejected (fail-closed).";
                    break;
            }

            return false;
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>单模块/单上传失败隔离判定器：把「一次批量处理」拆成逐项结果，
    ///   保证某一项失败只影响该项，不扩散为整页/整批失败（backlog 容纳机制方向 #4）。</zh-CN>
    ///   <en>Failure-isolation evaluator for individual modules/uploads: splits a batch into per-item results so one failure
    ///   affects only that item and never escalates into a whole-page/whole-batch failure (backlog accommodation direction #4).</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>纯逻辑、不触 IO：调用方传入每项是否已成功，本类只负责聚合与「页面是否存活」的判定。
    ///   既有通用装配层已用 `try/catch + continue`（`DesktopDefault.aspx.cs`）实现页面级隔离，本类不重复实现页面代码，
    ///   只提供可单测的判定契约。**不把失败项伪装成成功**：失败清单如实返回，页面存活仅表示「其它模块继续处理」。</zh-CN>
    ///   <en>Pure and IO-free: callers supply per-item success flags, and this class aggregates them and decides whether the page survived.
    ///   The existing generic assembly layer already implements page-level isolation with try/catch + continue (`DesktopDefault.aspx.cs`);
    ///   this class does not duplicate page code, only a unit-testable decision contract. **Failed items are never disguised as
    ///   successes**: the failure list is returned honestly, and page survival only means "other modules kept processing".</en>
    /// </lang>
    /// </remarks>
    public static class PortalOnlinePackageFailureIsolation
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>聚合逐项结果：返回失败项标识列表，并判定页面是否存活（只要不是「零项」或「全项无标识」，页面即存活）。</zh-CN>
        ///   <en>Aggregates per-item results: returns failed item ids and decides whether the page survived (it survives unless there were zero items or all ids were blank).</en>
        /// </lang>
        /// </summary>
        /// <param name="itemIds"><l zh-CN="参与批次的模块/上传标识序列，不能为 null。" en="Module/upload ids in the batch; cannot be null." /></param>
        /// <param name="succeededById"><l zh-CN="每个标识对应的成功标志；缺失键按失败处理（fail-closed）。" en="Success flag per id; a missing key is treated as failure (fail-closed)." /></param>
        /// <param name="failedIds"><l zh-CN="如实返回失败项标识列表。" en="Failed item ids, returned honestly." /></param>
        /// <param name="pageSurvived"><l zh-CN="true 表示单个失败未终止整页/整批处理。" en="True means one failure did not terminate the whole page/batch." /></param>
        /// <returns><l zh-CN="输入合法时为 true（无论是否有失败项）；输入非法时为 false。" en="True for valid input (regardless of failures); false for invalid input." /></returns>
        public static bool TryAggregate(
            IList<string> itemIds,
            IDictionary<string, bool> succeededById,
            out IList<string> failedIds,
            out bool pageSurvived)
        {
            // <lang>
            //   <zh-CN>输出先置失败态与空失败清单，保证提前返回不留下未初始化值。</zh-CN>
            //   <en>Initialise outputs to a failure state and an empty failure list so no early return leaves them uninitialised.</en>
            // </lang>
            failedIds = new List<string>();
            pageSurvived = false;

            // <lang>
            //   <zh-CN>空批次或缺失成功映射属于调用方错误，按拒绝处理，不解释为「全部成功」。</zh-CN>
            //   <en>An empty batch or a missing success map is a caller error and rejects; neither is interpreted as "all succeeded".</en>
            // </lang>
            if (itemIds == null || itemIds.Count == 0 || succeededById == null)
            {
                return false;
            }

            // <lang>
            //   <zh-CN>逐项判定：映射缺失、标识空白或标志为 false 都计入失败，但不中断循环。</zh-CN>
            //   <en>Judge each item: a missing key, blank id, or false flag counts as a failure, but the loop is never interrupted.</en>
            // </lang>
            int validItemCount = 0;
            foreach (string itemId in itemIds)
            {
                // <lang>
                //   <zh-CN>空白标识无法归属，计入失败清单但不抛出，避免一项脏数据终止整批。</zh-CN>
                //   <en>A blank id cannot be attributed; record it as a failure without throwing so one dirty row cannot terminate the batch.</en>
                // </lang>
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    failedIds.Add(string.Empty);
                    continue;
                }

                validItemCount++;

                // <lang>
                //   <zh-CN>缺失键按失败（fail-closed）；显式 false 同样计入失败。</zh-CN>
                //   <en>A missing key is a failure (fail-closed); an explicit false also counts as a failure.</en>
                // </lang>
                bool succeeded;
                if (!succeededById.TryGetValue(itemId, out succeeded) || !succeeded)
                {
                    failedIds.Add(itemId);
                }
            }

            // <lang>
            //   <zh-CN>页面存活：存在至少一个可归属项，且失败项数小于总数（即至少还有其它模块继续处理）。
            //   全部失败仍判定页面存活——因为失败被逐项隔离，没有升级为整页异常；调用方据失败清单决定是否降级呈现。</zh-CN>
            //   <en>Page survival: at least one attributable item exists. Even when all items failed the page survives,
            //   because failures were isolated per item and never escalated into a page-level exception; callers use the failure list to decide degraded rendering.</en>
            // </lang>
            pageSurvived = validItemCount > 0;
            return true;
        }
    }
}
