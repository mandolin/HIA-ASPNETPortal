using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>模块形态描述字段只读契约的稳定单元测试；覆盖封闭词表、缺省兼容、fail-closed 与负向注入。</zh-CN>
    ///   <en>Stable unit tests for the read-only module form-description contract; cover closed vocabularies, backward-compatible
    ///   defaults, fail-closed behaviour, and negative injection.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>这些测试只读取纯内存契约，不连接数据库、不读文件或配置、不依赖 IIS/HTTP 上下文，也不改变内核装载链。每次调用构造独立输入，互不共享可变状态。</zh-CN>
    ///   <en>These tests read only pure in-memory contracts; they connect to no database, read no file or configuration,
    ///   depend on no IIS/HTTP context, and do not change the kernel loading chain. Each test builds independent inputs and shares no mutable state.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalModuleFormContractTests
    {
        private static PortalModuleFormContract.FieldInput Host(string value)
        {
            return PortalModuleFormContract.FieldInput.FromString(value);
        }

        private static PortalModuleFormContract.FieldInput Entry(string value)
        {
            return PortalModuleFormContract.FieldInput.FromString(value);
        }

        /// <summary><lang><zh-CN>宿主框架与入口形态词表必须恰为已实现值，精确比较拒绝大小写、空白与第三值。</zh-CN><en>Vocabularies contain exactly the implemented values; exact comparison rejects case, whitespace and third values.</en></lang></summary>
        [TestMethod]
        public void Vocabularies_ClosedAndExact()
        {
            Assert.AreEqual("AspNetWebForms", PortalModuleHostFrameworks.AspNetWebForms);
            Assert.AreEqual("Ascx", PortalModuleEntryKinds.Ascx);

            Assert.IsTrue(PortalModuleHostFrameworks.IsRegistered("AspNetWebForms"));
            Assert.IsFalse(PortalModuleHostFrameworks.IsRegistered("aspnetwebforms"));
            Assert.IsFalse(PortalModuleHostFrameworks.IsRegistered(" AspNetWebForms "));
            Assert.IsFalse(PortalModuleHostFrameworks.IsRegistered("AspNetMvc"));
            Assert.IsFalse(PortalModuleHostFrameworks.IsRegistered(string.Empty));
            Assert.IsFalse(PortalModuleHostFrameworks.IsRegistered(null));

            Assert.IsTrue(PortalModuleEntryKinds.IsRegistered("Ascx"));
            Assert.IsFalse(PortalModuleEntryKinds.IsRegistered("ascx"));
            Assert.IsFalse(PortalModuleEntryKinds.IsRegistered("Mvc"));
            Assert.IsFalse(PortalModuleEntryKinds.IsRegistered(" Ascx "));
            Assert.IsFalse(PortalModuleEntryKinds.IsRegistered(null));
        }

        /// <summary><lang><zh-CN>既有包（无形态字段）必须零失败通过，保证兼容、不改变装载链结果。</zh-CN><en>Legacy manifests without form fields must pass with zero reasons, preserving compatibility and loading-chain results.</en></lang></summary>
        [TestMethod]
        public void LegacyManifest_WithoutFormFields_Passes()
        {
            IList<string> reasons;
            bool valid = PortalModuleFormContract.TryValidate(
                PortalModuleFormContract.FieldInput.Undeclared(),
                PortalModuleFormContract.FieldInput.Undeclared(),
                null,
                out reasons);

            Assert.IsTrue(valid);
            Assert.AreEqual(0, reasons.Count);
        }

        /// <summary><lang><zh-CN>合法形态声明（已登记词表 + 合法依赖数组）必须通过。</zh-CN><en>A valid form declaration with registered vocabulary and legal dependency array must pass.</en></lang></summary>
        [TestMethod]
        public void ValidFormDeclaration_Passes()
        {
            IList<string> reasons;
            bool valid = PortalModuleFormContract.TryValidate(
                Host("AspNetWebForms"),
                Entry("Ascx"),
                new List<string> { "HIA.Core", "BasicBusiness.FormBuilder" },
                out reasons);

            Assert.IsTrue(valid);
            Assert.AreEqual(0, reasons.Count);
        }

        /// <summary><lang><zh-CN>未知宿主框架 / 入口形态必须 fail-closed，且不回显输入值。</zh-CN><en>Unknown host framework or entry form must fail closed without echoing the input value.</en></lang></summary>
        [TestMethod]
        public void UnknownVocabularyValues_FailClosedWithoutEcho()
        {
            IList<string> reasons;
            bool valid = PortalModuleFormContract.TryValidate(Host("AspNetMvc"), Entry("WebForms"), null, out reasons);

            Assert.IsFalse(valid);
            Assert.AreEqual(2, reasons.Count);
            foreach (string reason in reasons)
            {
                Assert.IsFalse(reason.Contains("AspNetMvc"));
                Assert.IsFalse(reason.Contains("WebForms"));
            }
        }

        /// <summary><lang><zh-CN>字段类型错误（非字符串）必须 fail-closed；大小写变体按未知值拒绝。</zh-CN><en>Non-string field values and case variants must fail closed.</en></lang></summary>
        [TestMethod]
        public void WrongFieldTypesAndCaseVariants_FailClosed()
        {
            IList<string> reasons;
            Assert.IsFalse(PortalModuleFormContract.TryValidate(
                PortalModuleFormContract.FieldInput.NonString(),
                PortalModuleFormContract.FieldInput.NonString(),
                null,
                out reasons));
            Assert.AreEqual(2, reasons.Count);

            Assert.IsFalse(PortalModuleFormContract.TryValidate(
                Host("aspnetwebforms"), Entry("ascx"), null, out reasons));
            Assert.AreEqual(2, reasons.Count);
        }

        /// <summary><lang><zh-CN>依赖数组：空数组合法；空白项、超长项、超量项均 fail-closed。</zh-CN><en>Dependency array: an empty array is legal; blank, over-long and excessive items fail closed.</en></lang></summary>
        [TestMethod]
        public void DependencyArray_InvalidFormsFailClosed()
        {
            IList<string> reasons;

            Assert.IsTrue(PortalModuleFormContract.TryValidate(
                PortalModuleFormContract.FieldInput.Undeclared(),
                PortalModuleFormContract.FieldInput.Undeclared(),
                new List<string>(),
                out reasons));
            Assert.AreEqual(0, reasons.Count);

            Assert.IsFalse(PortalModuleFormContract.TryValidate(
                PortalModuleFormContract.FieldInput.Undeclared(),
                PortalModuleFormContract.FieldInput.Undeclared(),
                new List<string> { "   " },
                out reasons));
            Assert.AreEqual(1, reasons.Count);

            string overLong = new string('x', PortalModuleFormContract.MaxDependencyLength + 1);
            Assert.IsFalse(PortalModuleFormContract.TryValidate(
                PortalModuleFormContract.FieldInput.Undeclared(),
                PortalModuleFormContract.FieldInput.Undeclared(),
                new List<string> { overLong },
                out reasons));
            Assert.AreEqual(1, reasons.Count);

            List<string> tooMany = Enumerable.Repeat("HIA.Core", PortalModuleFormContract.MaxDependencyCount + 1).ToList();
            Assert.IsFalse(PortalModuleFormContract.TryValidate(
                PortalModuleFormContract.FieldInput.Undeclared(),
                PortalModuleFormContract.FieldInput.Undeclared(),
                tooMany,
                out reasons));
            Assert.AreEqual(1, reasons.Count);
        }

        /// <summary><lang><zh-CN>校验器须为纯函数：两次实跑结果逐字一致、不修改输入集合、不产生副作用。</zh-CN><en>The validator is a pure function: two runs produce identical reasons, do not mutate the input collection, and produce no side effects.</en></lang></summary>
        [TestMethod]
        public void Validation_IsRepeatableAndDoesNotMutateInputs()
        {
            List<string> dependencies = new List<string> { "HIA.Core" };
            IList<string> first;
            IList<string> second;

            bool firstResult = PortalModuleFormContract.TryValidate(Host("AspNetWebForms"), Entry("Ascx"), dependencies, out first);
            bool secondResult = PortalModuleFormContract.TryValidate(Host("AspNetWebForms"), Entry("Ascx"), dependencies, out second);

            Assert.AreEqual(firstResult, secondResult);
            Assert.AreEqual(string.Join("|", first), string.Join("|", second));
            Assert.AreEqual(1, dependencies.Count);
            Assert.AreEqual("HIA.Core", dependencies[0]);
        }
    }
}
