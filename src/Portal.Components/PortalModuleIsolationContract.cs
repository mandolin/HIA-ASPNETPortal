using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>入口注册与失败隔离的**纯内存只读契约判定器**。三项判定：
    ///   ① 入口在单一真源中唯一（复用 `PortalNavigationRegistry` 的条目形状，不新增 registry）；
    ///   ② 单模块失败可隔离（失败被记录且渲染循环 `continue`，其它模块继续）；
    ///   ③ 诊断记录字段齐备且类别在调用方传入的已登记词表内（不新建第二套审计链）。
    ///   **不接入装载链、不读文件/配置、不写任何内容**。</zh-CN>
    ///   <en>Pure in-memory read-only contract decisions for entry registration and failure isolation:
    ///   (1) an entry is unique in the single source of truth (reusing `PortalNavigationRegistry` entry shape, no new registry);
    ///   (2) a single module failure is isolated (recorded and the render loop continues, other modules still render);
    ///   (3) the diagnostic record is well formed and its category is in the caller-supplied registered vocabulary
    ///   (no second audit chain). It is NOT wired into the loading chain and reads/writes no file or config.</en>
    /// </lang>
    /// </summary>
    public static class PortalModuleIsolationContract
    {
        /// <summary><lang><zh-CN>入口在单一真源中不唯一（0 命中或重复）。</zh-CN><en>Entry is not unique in the single source (zero or duplicate hits).</en></lang></summary>
        public const string ReasonEntryNotUnique = "Isolation.EntryNotUnique";

        /// <summary><lang><zh-CN>失败未被记录为诊断，隔离不成立。</zh-CN><en>Failure was not recorded as a diagnostic; isolation does not hold.</en></lang></summary>
        public const string ReasonDiagnosticMissing = "Isolation.DiagnosticMissing";

        /// <summary><lang><zh-CN>渲染循环未 `continue`，失败会阻断其它模块。</zh-CN><en>Render loop did not `continue`; the failure blocks other modules.</en></lang></summary>
        public const string ReasonLoopStopped = "Isolation.LoopStopped";

        /// <summary><lang><zh-CN>没有后续模块，无法证明"其它模块继续"。</zh-CN><en>No subsequent module exists, so "other modules continue" cannot be shown.</en></lang></summary>
        public const string ReasonNoSubsequentModule = "Isolation.NoSubsequentModule";

        /// <summary><lang><zh-CN>诊断字段不齐备或类别未登记。</zh-CN><en>Diagnostic fields are incomplete or the category is unregistered.</en></lang></summary>
        public const string ReasonDiagnosticIncomplete = "Isolation.DiagnosticIncomplete";

        /// <summary><lang><zh-CN>模块失败上下文（由调用方从真实渲染循环提取）。</zh-CN><en>Module-failure context extracted by the caller from the real render loop.</en></lang></summary>
        public struct ModuleFailureInput
        {
            /// <summary>失败模块标识。</summary>
            public string ModuleId;

            /// <summary>失败分类（如 ModulePackage.Load）。</summary>
            public string FailureCategory;

            /// <summary>是否已记录诊断。</summary>
            public bool DiagnosticRecorded;

            /// <summary>渲染循环是否已 continue（跳过该模块）。</summary>
            public bool ContinueCalled;

            /// <summary><zh-CN>构造失败上下文。</zh-CN><en>Builds a failure context.</en></summary>
            public static ModuleFailureInput Create(string moduleId, string failureCategory, bool diagnosticRecorded, bool continueCalled)
            {
                return new ModuleFailureInput
                {
                    ModuleId = moduleId,
                    FailureCategory = failureCategory,
                    DiagnosticRecorded = diagnosticRecorded,
                    ContinueCalled = continueCalled
                };
            }
        }

        /// <summary><lang><zh-CN>诊断记录字段（由调用方从 `PortalDiagnosticEntry` 提取）。</zh-CN><en>Diagnostic fields extracted by the caller from `PortalDiagnosticEntry`.</en></lang></summary>
        public struct DiagnosticInput
        {
            /// <summary>事件标识。</summary>
            public string EventId;

            /// <summary>UTC 时间（非空即视为已记录）。</summary>
            public DateTime UtcTime;

            /// <summary>类别（须在已登记词表内）。</summary>
            public string Category;

            /// <summary>摘要（模块标识由调用方要求包含在其中，因为 DTO 无专用字段）。</summary>
            public string Message;

            /// <summary>异常类型（无异常时可为空）。</summary>
            public string ExceptionType;

            /// <summary><zh-CN>构造诊断输入。</zh-CN><en>Builds a diagnostic input.</en></summary>
            public static DiagnosticInput Create(string eventId, DateTime utcTime, string category, string message, string exceptionType)
            {
                return new DiagnosticInput
                {
                    EventId = eventId,
                    UtcTime = utcTime,
                    Category = category,
                    Message = message,
                    ExceptionType = exceptionType
                };
            }
        }

        /// <summary><lang><zh-CN>判定结果：是否成立 + 受控原因（成立时为空文本）。</zh-CN><en>Decision: holds flag plus a controlled reason (empty when it holds).</en></lang></summary>
        public struct Decision
        {
            /// <summary>判定是否成立。</summary>
            public bool Holds;

            /// <summary>受控原因。</summary>
            public string Reason;

            /// <summary><zh-CN>构造成立结果。</zh-CN><en>Builds a holding decision.</en></summary>
            public static Decision Pass()
            {
                return new Decision { Holds = true, Reason = string.Empty };
            }

            /// <summary><zh-CN>构造不成立结果。</zh-CN><en>Builds a failing decision.</en></summary>
            public static Decision Fail(string reason)
            {
                return new Decision { Holds = false, Reason = reason ?? string.Empty };
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判定入口键在单一真源中是否恰好命中一次。`entries` 由调用方从
        ///   `PortalNavigationRegistry.GetEntries()` 取得；空白键或不唯一即判不成立。</zh-CN>
        ///   <en>Decides whether an entry key hits the single source exactly once. `entries` comes from
        ///   `PortalNavigationRegistry.GetEntries()`; a blank key or a duplicate does not hold.</en>
        /// </lang>
        /// </summary>
        /// <param name="entries">调用方从 registry 取得的只读条目投影。</param>
        /// <param name="entryKey">待判定入口键。</param>
        /// <returns>恰好一次命中返回成立，否则 `Isolation.EntryNotUnique`。</returns>
        public static Decision IsEntryUnique(IList<PortalNavigationEntry> entries, string entryKey)
        {
            if (entries == null || string.IsNullOrWhiteSpace(entryKey))
            {
                return Decision.Fail(ReasonEntryNotUnique);
            }

            int hits = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                PortalNavigationEntry entry = entries[i];
                if (entry != null && string.Equals(entry.EntryKey, entryKey.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    hits++;
                }
            }

            return hits == 1 ? Decision.Pass() : Decision.Fail(ReasonEntryNotUnique);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判定单模块失败是否可隔离：须已记录诊断、渲染循环已 `continue`、
        ///   且至少还有一个后续模块可继续渲染（否则无法证明"不整页失败"）。</zh-CN>
        ///   <en>Decides whether a single module failure is isolated: a diagnostic must be recorded, the render loop
        ///   must have `continue`d, and at least one subsequent module must remain (otherwise "not blocking the page" is unprovable).</en>
        /// </lang>
        /// </summary>
        /// <param name="failure">失败上下文。</param>
        /// <param name="subsequentModuleCount">后续模块数量。</param>
        /// <returns>隔离成立或受控原因。</returns>
        public static Decision CanIsolateFailure(ModuleFailureInput failure, int subsequentModuleCount)
        {
            if (!failure.DiagnosticRecorded)
            {
                return Decision.Fail(ReasonDiagnosticMissing);
            }

            if (!failure.ContinueCalled)
            {
                return Decision.Fail(ReasonLoopStopped);
            }

            if (subsequentModuleCount <= 0)
            {
                return Decision.Fail(ReasonNoSubsequentModule);
            }

            return Decision.Pass();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判定诊断记录字段是否齐备：EventId / Category / Message 非空、UtcTime 非零、
        ///   Message 含模块标识（`PortalDiagnosticEntry` 无 ModuleId 专用字段）、Category 在已登记词表内。
        ///   不新建第二套审计链 —— 词表由调用方传入。</zh-CN>
        ///   <en>Decides whether a diagnostic record is well formed: EventId/Category/Message non-blank, UtcTime set,
        ///   Message contains the module id (`PortalDiagnosticEntry` has no dedicated ModuleId field), and Category is in
        ///   the caller-supplied registered vocabulary. No second audit chain is created.</en>
        /// </lang>
        /// </summary>
        /// <param name="diagnostic">诊断字段。</param>
        /// <param name="moduleId">失败模块标识。</param>
        /// <param name="registeredCategories">调用方传入的已登记类别词表。</param>
        /// <returns>字段齐备返回成立，否则 `Isolation.DiagnosticIncomplete`。</returns>
        public static Decision IsDiagnosticWellFormed(DiagnosticInput diagnostic, string moduleId, IList<string> registeredCategories)
        {
            if (string.IsNullOrWhiteSpace(diagnostic.EventId) ||
                diagnostic.UtcTime == default(DateTime) ||
                string.IsNullOrWhiteSpace(diagnostic.Category) ||
                string.IsNullOrWhiteSpace(diagnostic.Message))
            {
                return Decision.Fail(ReasonDiagnosticIncomplete);
            }

            if (string.IsNullOrWhiteSpace(moduleId) || diagnostic.Message.IndexOf(moduleId, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return Decision.Fail(ReasonDiagnosticIncomplete);
            }

            if (registeredCategories == null)
            {
                return Decision.Fail(ReasonDiagnosticIncomplete);
            }

            bool registered = false;
            for (int i = 0; i < registeredCategories.Count; i++)
            {
                string candidate = registeredCategories[i];
                if (!string.IsNullOrWhiteSpace(candidate) && string.Equals(candidate.Trim(), diagnostic.Category.Trim(), StringComparison.Ordinal))
                {
                    registered = true;
                    break;
                }
            }

            return registered ? Decision.Pass() : Decision.Fail(ReasonDiagnosticIncomplete);
        }
    }
}
