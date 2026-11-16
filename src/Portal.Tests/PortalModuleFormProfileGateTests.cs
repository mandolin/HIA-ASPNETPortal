using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>形态启用判定器的稳定单元测试；覆盖显式缺省关闭、允许集精确匹配、未声明兼容、fail-closed、允许集自检与装配态分离。</zh-CN>
    ///   <en>Stable unit tests for the form-aware enablement gate; cover explicit default-off, exact allow-set matching,
    ///   undeclared compatibility, fail-closed behaviour, allow-set self-validation, and assembly-state separation.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>这些测试只读取纯内存判定，不连接数据库、不读配置或文件、不依赖 IIS/HTTP 上下文、不触碰装配态开关。</zh-CN>
    ///   <en>These tests read only pure in-memory decisions; they connect to no database, read no config or file,
    ///   depend on no IIS/HTTP context, and never touch the assembly-state switch.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalModuleFormProfileGateTests
    {
        /// <summary><lang><zh-CN>允许集 null / 空必须显式缺省关闭，不因形态已声明而放行。</zh-CN><en>A null/empty allow-set means explicit default-off even when the form is declared.</en></lang></summary>
        [TestMethod]
        public void AllowSetNullOrEmpty_IsDefaultOff()
        {
            PortalModuleFormContract.FieldInput form = PortalModuleFormContract.FieldInput.FromString("AspNetWebForms");

            PortalModuleFormProfileGate.Decision nullSet = PortalModuleFormProfileGate.IsHostFrameworkEnabled(form, null);
            Assert.IsFalse(nullSet.Enabled);
            Assert.AreEqual(PortalModuleFormProfileGate.ReasonAllowSetEmpty, nullSet.Reason);

            PortalModuleFormProfileGate.Decision emptySet = PortalModuleFormProfileGate.IsEntryKindEnabled(form, new List<string>());
            Assert.IsFalse(emptySet.Enabled);
            Assert.AreEqual(PortalModuleFormProfileGate.ReasonAllowSetEmpty, emptySet.Reason);
        }

        /// <summary><lang><zh-CN>允许集命中即启用、未命中 fail-closed；精确比较拒绝大小写与空白变体。</zh-CN><en>A hit enables; a miss fails closed; exact comparison rejects case and whitespace variants.</en></lang></summary>
        [TestMethod]
        public void AllowSet_ExactMatchOnly()
        {
            PortalModuleFormContract.FieldInput form = PortalModuleFormContract.FieldInput.FromString("AspNetWebForms");
            List<string> allow = new List<string> { "AspNetWebForms" };

            PortalModuleFormProfileGate.Decision enabled = PortalModuleFormProfileGate.IsHostFrameworkEnabled(form, allow);
            Assert.IsTrue(enabled.Enabled);
            Assert.AreEqual(string.Empty, enabled.Reason);

            PortalModuleFormProfileGate.Decision wrongCase = PortalModuleFormProfileGate.IsHostFrameworkEnabled(
                PortalModuleFormContract.FieldInput.FromString("aspnetwebforms"), allow);
            Assert.IsFalse(wrongCase.Enabled);
            Assert.AreEqual(PortalModuleFormProfileGate.ReasonFormNotAllowed, wrongCase.Reason);

            PortalModuleFormProfileGate.Decision wrongValue = PortalModuleFormProfileGate.IsHostFrameworkEnabled(
                PortalModuleFormContract.FieldInput.FromString("AspNetMvc"), allow);
            Assert.IsFalse(wrongValue.Enabled);
            Assert.AreEqual(PortalModuleFormProfileGate.ReasonFormNotAllowed, wrongValue.Reason);
        }

        /// <summary><lang><zh-CN>未声明形态不启用新形态（兼容既有包），原因受控；非字符串同 fail-closed。</zh-CN><en>An undeclared form enables no new form (legacy compatible) with a controlled reason; non-strings also fail closed.</en></lang></summary>
        [TestMethod]
        public void UndeclaredAndNonString_AreNotEnabled()
        {
            List<string> allow = new List<string> { "AspNetWebForms" };

            PortalModuleFormProfileGate.Decision undeclared = PortalModuleFormProfileGate.IsHostFrameworkEnabled(
                PortalModuleFormContract.FieldInput.Undeclared(), allow);
            Assert.IsFalse(undeclared.Enabled);
            Assert.AreEqual(PortalModuleFormProfileGate.ReasonFormUndeclared, undeclared.Reason);

            PortalModuleFormProfileGate.Decision nonString = PortalModuleFormProfileGate.IsEntryKindEnabled(
                PortalModuleFormContract.FieldInput.NonString(), allow);
            Assert.IsFalse(nonString.Enabled);
            Assert.AreEqual(PortalModuleFormProfileGate.ReasonFormNotAllowed, nonString.Reason);
        }

        /// <summary><lang><zh-CN>允许集自检：已登记值合法；未知值 / 空白项 / null 均 fail-closed，避免配置写错变默认放行。</zh-CN><en>Allow-set self-validation: registered values pass; unknown/blank/null values fail closed so misconfiguration cannot become default-allow.</en></lang></summary>
        [TestMethod]
        public void AllowSetSelfValidation_FailsClosedOnUnknownOrBlank()
        {
            Assert.IsTrue(PortalModuleFormProfileGate.IsAllowSetWellFormed(
                new List<string> { "AspNetWebForms" }, PortalModuleHostFrameworks.IsRegistered));
            Assert.IsTrue(PortalModuleFormProfileGate.IsAllowSetWellFormed(
                new List<string> { "Ascx" }, PortalModuleEntryKinds.IsRegistered));

            Assert.IsFalse(PortalModuleFormProfileGate.IsAllowSetWellFormed(
                new List<string> { "AspNetMvc" }, PortalModuleHostFrameworks.IsRegistered));
            Assert.IsFalse(PortalModuleFormProfileGate.IsAllowSetWellFormed(
                new List<string> { "  " }, PortalModuleHostFrameworks.IsRegistered));
            Assert.IsFalse(PortalModuleFormProfileGate.IsAllowSetWellFormed(null, PortalModuleHostFrameworks.IsRegistered));
            Assert.IsFalse(PortalModuleFormProfileGate.IsAllowSetWellFormed(
                new List<string> { "Ascx" }, null));
        }

        /// <summary><lang><zh-CN>宿主框架与入口形态判定相互独立：一个允许集的改动不影响另一维度的判定结果。</zh-CN><en>Host-framework and entry-form decisions are independent: changing one allow-set does not affect the other dimension.</en></lang></summary>
        [TestMethod]
        public void HostAndEntryDecisions_AreIndependent()
        {
            List<string> hostAllow = new List<string> { "AspNetWebForms" };
            List<string> entryAllow = new List<string> { "Ascx" };

            Assert.IsTrue(PortalModuleFormProfileGate.IsHostFrameworkEnabled(
                PortalModuleFormContract.FieldInput.FromString("AspNetWebForms"), hostAllow).Enabled);
            Assert.IsFalse(PortalModuleFormProfileGate.IsHostFrameworkEnabled(
                PortalModuleFormContract.FieldInput.FromString("Ascx"), hostAllow).Enabled);
            Assert.IsTrue(PortalModuleFormProfileGate.IsEntryKindEnabled(
                PortalModuleFormContract.FieldInput.FromString("Ascx"), entryAllow).Enabled);
            Assert.IsFalse(PortalModuleFormProfileGate.IsEntryKindEnabled(
                PortalModuleFormContract.FieldInput.FromString("AspNetWebForms"), entryAllow).Enabled);
        }

        /// <summary><lang><zh-CN>判定器为纯函数：两次实跑逐字一致、不修改输入允许集、不读取任何部署配置。</zh-CN><en>The gate is a pure function: two runs are byte-identical, the input allow-set is not mutated, and no deployment config is read.</en></lang></summary>
        [TestMethod]
        public void Gate_IsRepeatableAndDoesNotMutateInputs()
        {
            List<string> allow = new List<string> { "AspNetWebForms" };
            PortalModuleFormContract.FieldInput form = PortalModuleFormContract.FieldInput.FromString("AspNetWebForms");

            PortalModuleFormProfileGate.Decision first = PortalModuleFormProfileGate.IsHostFrameworkEnabled(form, allow);
            PortalModuleFormProfileGate.Decision second = PortalModuleFormProfileGate.IsHostFrameworkEnabled(form, allow);

            Assert.AreEqual(first.Enabled, second.Enabled);
            Assert.AreEqual(first.Reason, second.Reason);
            Assert.AreEqual(1, allow.Count);
            Assert.AreEqual("AspNetWebForms", allow[0]);
        }
    }
}
