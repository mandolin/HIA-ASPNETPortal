using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>权限与审计契约的稳定单元测试；覆盖权限键单一事实源、管理级前缀拒绝、扁平键拒绝、审计词表/outcome 白名单与 A 级三边界机器核对。</zh-CN>
    ///   <en>Stable unit tests for the permission-and-audit contract; cover the registered permission-key vocabulary,
    ///   administrative-prefix and flat-key rejection, audit vocabulary/outcome allow-list, and machine checks for the three A-level boundaries.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>只读取纯内存词表与判定，不连接数据库、不读配置、不新增权限键或审计事件常量、不依赖 IIS/HTTP 上下文。</zh-CN>
    ///   <en>These tests read only in-memory vocabularies and decisions; they connect to no database, read no config,
    ///   add no permission-key or audit-event constant, and depend on no IIS/HTTP context.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalModulePermissionAuditContractTests
    {
        private static IList<string> PermissionKeys()
        {
            return new List<string>
            {
                "Module.Catalog.View", "Module.Catalog.Edit", "Module.AssemblyState.View", "Module.AssemblyState.Edit",
                "Module.OnlinePackage.Upload", "Module.OnlinePackage.Confirm",
                "EnterpriseCapability.View",
                "EnterpriseCapability.Foundation.Manage", "EnterpriseCapability.Foundation.Review",
                "EnterpriseCapability.BasicBusiness.Manage", "EnterpriseCapability.BasicBusiness.Review",
                "EnterpriseCapability.Platform.Manage", "EnterpriseCapability.Platform.Review"
            };
        }

        private static IList<string> Capabilities()
        {
            return new List<string>
            {
                "BasicBusiness.FormBuilder", "BasicBusiness.Workflow", "BasicBusiness.EmployeeProfileConfirm",
                "EnterpriseBase.EnterpriseDirectory"
            };
        }

        private static IList<string> AuditCategories()
        {
            return new List<string> { "ModuleAssembly", "BusinessModule", "OnlinePackage" };
        }

        private static IList<string> AuditActions()
        {
            return new List<string> { "AssemblyStateCheck", "AssemblyStateRollbackDrill", "OnlinePackageUploadValidated" };
        }

        /// <summary><lang><zh-CN>权限键须在已登记集合内；未登记/空白/null 一律 fail-closed。</zh-CN><en>Permission keys must be in the registered set; unknown/blank/null keys fail closed.</en></lang></summary>
        [TestMethod]
        public void PermissionKey_MustBeRegistered()
        {
            Assert.IsTrue(PortalModulePermissionAuditContract.IsModuleActionPermissionWellFormed(
                "EnterpriseCapability.BasicBusiness.Manage", PermissionKeys()).Holds);
            Assert.IsTrue(PortalModulePermissionAuditContract.IsModuleActionPermissionWellFormed(
                "Module.Catalog.Edit", PermissionKeys()).Holds);

            PortalModulePermissionAuditContract.Decision unknown = PortalModulePermissionAuditContract.IsModuleActionPermissionWellFormed(
                "Module.Shape.Install", PermissionKeys());
            Assert.IsFalse(unknown.Holds);
            Assert.AreEqual(PortalModulePermissionAuditContract.ReasonPermissionUnknown, unknown.Reason);

            Assert.IsFalse(PortalModulePermissionAuditContract.IsModuleActionPermissionWellFormed("   ", PermissionKeys()).Holds);
            Assert.IsFalse(PortalModulePermissionAuditContract.IsModuleActionPermissionWellFormed(null, PermissionKeys()).Holds);
            Assert.IsFalse(PortalModulePermissionAuditContract.IsModuleActionPermissionWellFormed("Module.Catalog.Edit", null).Holds);
        }

        /// <summary><lang><zh-CN>管理级前缀与扁平单层键一律拒绝，不因在已登记集合内而放行。</zh-CN><en>Administrative prefixes and flat single-layer keys are rejected even if present in the registered set.</en></lang></summary>
        [TestMethod]
        public void PermissionKey_RejectsAdminPrefixAndFlatKey()
        {
            IList<string> keys = new List<string> { "Admins.FullControl", "Admin.Roles.Edit", "Business.Admin.All", "EnterpriseCapability.Manage" };

            PortalModulePermissionAuditContract.Decision admins = PortalModulePermissionAuditContract.IsModuleActionPermissionWellFormed("Admins.FullControl", keys);
            Assert.IsFalse(admins.Holds);
            Assert.AreEqual(PortalModulePermissionAuditContract.ReasonAdminPrefix, admins.Reason);

            Assert.IsFalse(PortalModulePermissionAuditContract.IsModuleActionPermissionWellFormed("Admin.Roles.Edit", keys).Holds);
            Assert.IsFalse(PortalModulePermissionAuditContract.IsModuleActionPermissionWellFormed("Business.Admin.All", keys).Holds);

            PortalModulePermissionAuditContract.Decision flat = PortalModulePermissionAuditContract.IsModuleActionPermissionWellFormed("EnterpriseCapability.Manage", keys);
            Assert.IsFalse(flat.Holds);
            Assert.AreEqual(PortalModulePermissionAuditContract.ReasonFlatKey, flat.Reason);
        }

        /// <summary><lang><zh-CN>审计记录：类别/动作须在已登记词表内，outcome 须在 Success/Failure/Skipped 白名单内。</zh-CN><en>Audit records require registered category/action and an outcome in the Success/Failure/Skipped allow-list.</en></lang></summary>
        [TestMethod]
        public void AuditRecord_RequiresRegisteredVocabularyAndOutcome()
        {
            Assert.IsTrue(PortalModulePermissionAuditContract.IsAuditRecordWellFormed(
                "ModuleAssembly", "AssemblyStateCheck", "Success", AuditCategories(), AuditActions()).Holds);
            Assert.IsTrue(PortalModulePermissionAuditContract.IsAuditRecordWellFormed(
                "OnlinePackage", "OnlinePackageUploadValidated", "Failure", AuditCategories(), AuditActions()).Holds);

            PortalModulePermissionAuditContract.Decision badCategory = PortalModulePermissionAuditContract.IsAuditRecordWellFormed(
                "Unregistered", "AssemblyStateCheck", "Success", AuditCategories(), AuditActions());
            Assert.IsFalse(badCategory.Holds);
            Assert.AreEqual(PortalModulePermissionAuditContract.ReasonAuditUnknown, badCategory.Reason);

            PortalModulePermissionAuditContract.Decision badOutcome = PortalModulePermissionAuditContract.IsAuditRecordWellFormed(
                "ModuleAssembly", "AssemblyStateCheck", "FabricatedSuccess", AuditCategories(), AuditActions());
            Assert.IsFalse(badOutcome.Holds);
            Assert.AreEqual(PortalModulePermissionAuditContract.ReasonAuditBadOutcome, badOutcome.Reason);

            Assert.IsFalse(PortalModulePermissionAuditContract.IsAuditRecordWellFormed(
                "ModuleAssembly", "UnknownAction", "Success", AuditCategories(), AuditActions()).Holds);
        }

        /// <summary><lang><zh-CN>边界①：授予管理级前缀权限即判红；普通权限键与空值通过（空值沿用条件口径不阻断）。</zh-CN><en>Boundary 1: granting an administrative-prefix permission fails; ordinary keys and blank values pass (blank follows the conditional rule).</en></lang></summary>
        [TestMethod]
        public void Boundary1_NoAdminGrant()
        {
            Assert.IsTrue(PortalModulePermissionAuditContract.CheckNoAdminGrant("EnterpriseCapability.BasicBusiness.Manage").Holds);
            Assert.IsTrue(PortalModulePermissionAuditContract.CheckNoAdminGrant(null).Holds);
            Assert.IsTrue(PortalModulePermissionAuditContract.CheckNoAdminGrant("   ").Holds);

            PortalModulePermissionAuditContract.Decision grant = PortalModulePermissionAuditContract.CheckNoAdminGrant("Admins.FullControl");
            Assert.IsFalse(grant.Holds);
            Assert.AreEqual(PortalModulePermissionAuditContract.ReasonBoundaryAdminGrant, grant.Reason);
            Assert.IsFalse(PortalModulePermissionAuditContract.CheckNoAdminGrant("Admin.Roles.Edit").Holds);
        }

        /// <summary><lang><zh-CN>边界②：授权依据只接受 RegisteredPermissionKey；来源/能力声明/角色名一律拒绝。</zh-CN><en>Boundary 2: only RegisteredPermissionKey is accepted; origin/capability claims and role names are rejected.</en></lang></summary>
        [TestMethod]
        public void Boundary2_OnlyRegisteredKeyIsAuthorizationBasis()
        {
            Assert.IsTrue(PortalModulePermissionAuditContract.CheckAuthorizationBasis(
                PortalModulePermissionAuditContract.AuthorizationBasisRegisteredKey).Holds);

            string[] rejected =
            {
                PortalModulePermissionAuditContract.AuthorizationBasisOriginClaim,
                PortalModulePermissionAuditContract.AuthorizationBasisCapabilityClaim,
                PortalModulePermissionAuditContract.AuthorizationBasisRoleName,
                "Admins",
                string.Empty,
                null
            };

            foreach (string basis in rejected)
            {
                PortalModulePermissionAuditContract.Decision decision = PortalModulePermissionAuditContract.CheckAuthorizationBasis(basis);
                Assert.IsFalse(decision.Holds);
                Assert.AreEqual(PortalModulePermissionAuditContract.ReasonBoundaryOriginClaim, decision.Reason);
            }
        }

        /// <summary><lang><zh-CN>边界③：能力条目须在封闭词表内；未声明能力不阻断（与 Provisioner 条件口径一致）。</zh-CN><en>Boundary 3: capability ids must be in the closed registry; an undeclared capability is not blocked (matching the Provisioner rule).</en></lang></summary>
        [TestMethod]
        public void Boundary3_NoSelfCreatedCapability()
        {
            Assert.IsTrue(PortalModulePermissionAuditContract.CheckCapabilityRegistered("BasicBusiness.FormBuilder", Capabilities()).Holds);
            Assert.IsTrue(PortalModulePermissionAuditContract.CheckCapabilityRegistered(null, Capabilities()).Holds);
            Assert.IsTrue(PortalModulePermissionAuditContract.CheckCapabilityRegistered("   ", Capabilities()).Holds);

            PortalModulePermissionAuditContract.Decision selfCreated = PortalModulePermissionAuditContract.CheckCapabilityRegistered(
                "BasicBusiness.SelfCreated", Capabilities());
            Assert.IsFalse(selfCreated.Holds);
            Assert.AreEqual(PortalModulePermissionAuditContract.ReasonBoundarySelfCreated, selfCreated.Reason);
        }

        /// <summary><lang><zh-CN>全部判定为纯函数：两次实跑逐字一致、不修改调用方传入的词表。</zh-CN><en>All decisions are pure: two runs are byte-identical and caller-supplied vocabularies are not mutated.</en></lang></summary>
        [TestMethod]
        public void Decisions_AreRepeatableAndDoNotMutateInputs()
        {
            IList<string> keys = PermissionKeys();
            IList<string> capabilities = Capabilities();
            int keyCount = keys.Count;
            int capCount = capabilities.Count;

            PortalModulePermissionAuditContract.Decision first = PortalModulePermissionAuditContract.IsModuleActionPermissionWellFormed("Module.Catalog.Edit", keys);
            PortalModulePermissionAuditContract.Decision second = PortalModulePermissionAuditContract.IsModuleActionPermissionWellFormed("Module.Catalog.Edit", keys);
            Assert.AreEqual(first.Holds, second.Holds);
            Assert.AreEqual(first.Reason, second.Reason);
            Assert.AreEqual(keyCount, keys.Count);

            Assert.AreEqual(
                PortalModulePermissionAuditContract.CheckCapabilityRegistered("BasicBusiness.FormBuilder", capabilities).Reason,
                PortalModulePermissionAuditContract.CheckCapabilityRegistered("BasicBusiness.FormBuilder", capabilities).Reason);
            Assert.AreEqual(capCount, capabilities.Count);
        }
    }
}
