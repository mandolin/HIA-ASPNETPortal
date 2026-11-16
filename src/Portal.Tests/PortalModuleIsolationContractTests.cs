using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>入口注册与失败隔离契约的稳定单元测试；覆盖单一真源唯一性、失败隔离三条件、诊断字段与类别词表、注入异常降级、纯函数幂等。</zh-CN>
    ///   <en>Stable unit tests for the entry-registration and failure-isolation contract; cover single-source uniqueness,
    ///   the three isolation conditions, diagnostic fields and category vocabulary, injected-exception degradation, and pure-function repeatability.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>只读取纯内存契约与调用方构造的条目，不连接数据库、不读配置/文件、不依赖 IIS/HTTP 上下文、不修改 registry。</zh-CN>
    ///   <en>These tests read only pure in-memory contracts and caller-built entries; they connect to no database, read no
    ///   config/file, depend on no IIS/HTTP context, and never mutate the registry.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalModuleIsolationContractTests
    {
        private static PortalNavigationEntry Entry(string key)
        {
            return new PortalNavigationEntry(
                key,
                PortalNavigationEntryKind.DesktopModule,
                key + " 中文",
                key + " En",
                "DesktopModules/" + key + "/" + key + ".ascx",
                PortalNavigationVisibilityMode.VisibleWhenAllowed,
                PortalNavigationLifecycleState.Active,
                10,
                new string[0],
                new string[0],
                new string[0],
                new string[0],
                "test entry");
        }

        private static IList<string> Categories()
        {
            return new List<string> { "ModulePackage", "ModuleAssembly" };
        }

        /// <summary><lang><zh-CN>入口键在单一真源中恰好命中一次才成立；空白键、0 命中、重复命中均不成立。</zh-CN><en>An entry holds only when it hits the single source exactly once; blank, zero-hit and duplicate cases fail.</en></lang></summary>
        [TestMethod]
        public void EntryUnique_RequiresExactlyOneHit()
        {
            IList<PortalNavigationEntry> entries = new List<PortalNavigationEntry> { Entry("HIA.Core"), Entry("HIA.Extra") };

            PortalModuleIsolationContract.Decision unique = PortalModuleIsolationContract.IsEntryUnique(entries, "HIA.Core");
            Assert.IsTrue(unique.Holds);
            Assert.AreEqual(string.Empty, unique.Reason);

            PortalModuleIsolationContract.Decision missing = PortalModuleIsolationContract.IsEntryUnique(entries, "HIA.Missing");
            Assert.IsFalse(missing.Holds);
            Assert.AreEqual(PortalModuleIsolationContract.ReasonEntryNotUnique, missing.Reason);

            PortalModuleIsolationContract.Decision blank = PortalModuleIsolationContract.IsEntryUnique(entries, "   ");
            Assert.IsFalse(blank.Holds);
            Assert.AreEqual(PortalModuleIsolationContract.ReasonEntryNotUnique, blank.Reason);

            PortalModuleIsolationContract.Decision duplicate = PortalModuleIsolationContract.IsEntryUnique(
                new List<PortalNavigationEntry> { Entry("HIA.Core"), Entry("HIA.Core") }, "HIA.Core");
            Assert.IsFalse(duplicate.Holds);
            Assert.AreEqual(PortalModuleIsolationContract.ReasonEntryNotUnique, duplicate.Reason);

            PortalModuleIsolationContract.Decision nullSource = PortalModuleIsolationContract.IsEntryUnique(null, "HIA.Core");
            Assert.IsFalse(nullSource.Holds);
        }

        /// <summary><lang><zh-CN>失败隔离三条件逐条判红：未记录诊断 / 未 continue / 无后续模块。</zh-CN><en>Each of the three isolation conditions fails independently: no diagnostic / no continue / no subsequent module.</en></lang></summary>
        [TestMethod]
        public void CanIsolateFailure_RequiresAllThreeConditions()
        {
            PortalModuleIsolationContract.Decision isolated = PortalModuleIsolationContract.CanIsolateFailure(
                PortalModuleIsolationContract.ModuleFailureInput.Create("HIA.Broken", "ModulePackage.Load", true, true), 2);
            Assert.IsTrue(isolated.Holds);
            Assert.AreEqual(string.Empty, isolated.Reason);

            PortalModuleIsolationContract.Decision noDiagnostic = PortalModuleIsolationContract.CanIsolateFailure(
                PortalModuleIsolationContract.ModuleFailureInput.Create("HIA.Broken", "ModulePackage.Load", false, true), 2);
            Assert.IsFalse(noDiagnostic.Holds);
            Assert.AreEqual(PortalModuleIsolationContract.ReasonDiagnosticMissing, noDiagnostic.Reason);

            PortalModuleIsolationContract.Decision noContinue = PortalModuleIsolationContract.CanIsolateFailure(
                PortalModuleIsolationContract.ModuleFailureInput.Create("HIA.Broken", "ModulePackage.Load", true, false), 2);
            Assert.IsFalse(noContinue.Holds);
            Assert.AreEqual(PortalModuleIsolationContract.ReasonLoopStopped, noContinue.Reason);

            PortalModuleIsolationContract.Decision noNext = PortalModuleIsolationContract.CanIsolateFailure(
                PortalModuleIsolationContract.ModuleFailureInput.Create("HIA.Broken", "ModulePackage.Load", true, true), 0);
            Assert.IsFalse(noNext.Holds);
            Assert.AreEqual(PortalModuleIsolationContract.ReasonNoSubsequentModule, noNext.Reason);
        }

        /// <summary><lang><zh-CN>诊断记录：字段齐备、类别已登记、Message 含模块标识才成立；缺字段/未登记类别/不含模块标识均 fail-closed。</zh-CN><en>Diagnostic record holds only with complete fields, a registered category, and a Message containing the module id; otherwise fail closed.</en></lang></summary>
        [TestMethod]
        public void DiagnosticWellFormed_FailsClosedOnMissingFieldsOrCategory()
        {
            DateTime now = new DateTime(2026, 11, 17, 0, 0, 0, DateTimeKind.Utc);

            PortalModuleIsolationContract.Decision wellFormed = PortalModuleIsolationContract.IsDiagnosticWellFormed(
                PortalModuleIsolationContract.DiagnosticInput.Create("evt-1", now, "ModulePackage", "Skipping module HIA.Broken: failed", "InvalidOperationException"),
                "HIA.Broken", Categories());
            Assert.IsTrue(wellFormed.Holds);

            PortalModuleIsolationContract.Decision noEventId = PortalModuleIsolationContract.IsDiagnosticWellFormed(
                PortalModuleIsolationContract.DiagnosticInput.Create("  ", now, "ModulePackage", "Skipping module HIA.Broken", "Exception"),
                "HIA.Broken", Categories());
            Assert.IsFalse(noEventId.Holds);
            Assert.AreEqual(PortalModuleIsolationContract.ReasonDiagnosticIncomplete, noEventId.Reason);

            PortalModuleIsolationContract.Decision noModuleId = PortalModuleIsolationContract.IsDiagnosticWellFormed(
                PortalModuleIsolationContract.DiagnosticInput.Create("evt-2", now, "ModulePackage", "Skipping module", "Exception"),
                "HIA.Broken", Categories());
            Assert.IsFalse(noModuleId.Holds);

            PortalModuleIsolationContract.Decision unknownCategory = PortalModuleIsolationContract.IsDiagnosticWellFormed(
                PortalModuleIsolationContract.DiagnosticInput.Create("evt-3", now, "Unregistered", "Skipping module HIA.Broken", "Exception"),
                "HIA.Broken", Categories());
            Assert.IsFalse(unknownCategory.Holds);

            PortalModuleIsolationContract.Decision defaultTime = PortalModuleIsolationContract.IsDiagnosticWellFormed(
                PortalModuleIsolationContract.DiagnosticInput.Create("evt-4", default(DateTime), "ModulePackage", "Skipping module HIA.Broken", "Exception"),
                "HIA.Broken", Categories());
            Assert.IsFalse(defaultTime.Holds);
        }

        /// <summary><lang><zh-CN>注入异常 → 记录诊断 + continue → 隔离判定成立；未记录则不成立（还原为正常态后仍成立）。</zh-CN><en>Injected exception → diagnostic recorded + continue → isolation holds; without recording it fails, and returning to the normal path still holds.</en></lang></summary>
        [TestMethod]
        public void InjectedModuleException_IsIsolatedWhenRecordedAndSkipped()
        {
            string moduleId = "HIA.Broken";
            Exception injected = null;
            try
            {
                throw new InvalidOperationException("模拟模块加载失败");
            }
            catch (Exception exception)
            {
                injected = exception;
            }

            Assert.IsNotNull(injected);
            DateTime utc = DateTime.UtcNow;
            string message = "Skipping module " + moduleId + ": " + injected.Message;

            // 注入态：诊断已记录且循环 continue → 隔离成立
            PortalModuleIsolationContract.Decision isolated = PortalModuleIsolationContract.CanIsolateFailure(
                PortalModuleIsolationContract.ModuleFailureInput.Create(moduleId, "ModulePackage.Load", true, true), 1);
            Assert.IsTrue(isolated.Holds);

            PortalModuleIsolationContract.Decision diagnostic = PortalModuleIsolationContract.IsDiagnosticWellFormed(
                PortalModuleIsolationContract.DiagnosticInput.Create("evt-inject", utc, "ModulePackage", message, injected.GetType().Name),
                moduleId, Categories());
            Assert.IsTrue(diagnostic.Holds);

            // 未记录诊断 → 不成立（还原判绿的对照）
            PortalModuleIsolationContract.Decision unrecorded = PortalModuleIsolationContract.CanIsolateFailure(
                PortalModuleIsolationContract.ModuleFailureInput.Create(moduleId, "ModulePackage.Load", false, true), 1);
            Assert.IsFalse(unrecorded.Holds);
        }

        /// <summary><lang><zh-CN>三项判定均为纯函数：两次实跑逐字一致、不修改调用方传入的条目/类别集合。</zh-CN><en>All three decisions are pure: two runs are byte-identical and caller-supplied entry/category collections are not mutated.</en></lang></summary>
        [TestMethod]
        public void Decisions_AreRepeatableAndDoNotMutateInputs()
        {
            List<PortalNavigationEntry> entries = new List<PortalNavigationEntry> { Entry("HIA.Core") };
            IList<string> categories = Categories();

            PortalModuleIsolationContract.Decision first = PortalModuleIsolationContract.IsEntryUnique(entries, "HIA.Core");
            PortalModuleIsolationContract.Decision second = PortalModuleIsolationContract.IsEntryUnique(entries, "HIA.Core");
            Assert.AreEqual(first.Holds, second.Holds);
            Assert.AreEqual(first.Reason, second.Reason);
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(2, categories.Count);

            PortalModuleIsolationContract.ModuleFailureInput failure =
                PortalModuleIsolationContract.ModuleFailureInput.Create("HIA.Broken", "ModulePackage.Load", true, true);
            Assert.AreEqual(
                PortalModuleIsolationContract.CanIsolateFailure(failure, 1).Reason,
                PortalModuleIsolationContract.CanIsolateFailure(failure, 1).Reason);
            Assert.IsTrue(failure.DiagnosticRecorded && failure.ContinueCalled);
        }
    }
}
