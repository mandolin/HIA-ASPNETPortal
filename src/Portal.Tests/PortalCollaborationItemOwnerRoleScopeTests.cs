using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>负责人角色键维度（W72）的契约测试：锁定读取边界与既有写资格对齐，且不破坏其它维度的既有语义。</zh-CN>
    ///   <en>Contract tests for the owner-role-key dimension (W72): they pin that the read boundary aligns with the existing write eligibility without disturbing the other dimensions.</en>
    /// </lang>
    /// </summary>
    [TestClass]
    public class PortalCollaborationItemOwnerRoleScopeTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>持有负责人角色键的动作人应可见该事项，即使既不是参与人、也没有任何组织范围。</zh-CN>
        ///   <en>An actor holding the owner role key must be able to view the item even when it is not a participant and has no organization scope at all.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void CanView_OwnerRoleKeyHolder_GrantsVisibility()
        {
            CollaborationItemInfo item = CreateItem(ownerRoleKey: "Business.Collaboration.Handle");
            CollaborationItemDataScope scope = new CollaborationItemDataScope(
                actorUserId: 4242,
                isAdministrator: false,
                isParticipant: false,
                visibleOrganizationUnitIds: new List<int>(),
                holdsOwnerRoleKey: true);

            Assert.IsTrue(
                CollaborationItemDataScopePolicy.CanView(item, scope),
                "持有负责人角色键的动作人必须可见该事项，否则写资格无法执行。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>未持有负责人角色键且其它维度均不成立时，必须拒绝（fail-closed）。</zh-CN>
        ///   <en>Without the owner role key and with every other dimension failing, the decision must deny (fail-closed).</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void CanView_WithoutAnyDimension_Denies()
        {
            CollaborationItemInfo item = CreateItem(ownerRoleKey: "Business.Collaboration.Handle");
            CollaborationItemDataScope scope = new CollaborationItemDataScope(
                actorUserId: 4242,
                isAdministrator: false,
                isParticipant: false,
                visibleOrganizationUnitIds: new List<int>(),
                holdsOwnerRoleKey: false);

            Assert.IsFalse(
                CollaborationItemDataScopePolicy.CanView(item, scope),
                "没有任何维度成立时必须拒绝。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>组织维度仍然生效：未持有角色键但事项组织落在可见范围内时应可见。</zh-CN>
        ///   <en>The organization dimension still works: without the role key, an item whose organization falls inside the visible scope is visible.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void CanView_OrganizationDimension_StillApplies()
        {
            CollaborationItemInfo item = CreateItem(ownerRoleKey: null);
            item.OrganizationUnitId = 88;
            CollaborationItemDataScope scope = new CollaborationItemDataScope(
                actorUserId: 4242,
                isAdministrator: false,
                isParticipant: false,
                visibleOrganizationUnitIds: new List<int> { 88 },
                holdsOwnerRoleKey: false);

            Assert.IsTrue(
                CollaborationItemDataScopePolicy.CanView(item, scope),
                "组织维度在既有顺序下仍应授予可见性。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>管理员仍然优先：即使不具备其它任何维度也应可见（沿用既有集中查看能力）。</zh-CN>
        ///   <en>Administrators still take precedence: they remain visible even without any other dimension (continuing the existing centralized viewing capability).</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void CanView_Administrator_StillTakesPrecedence()
        {
            CollaborationItemInfo item = CreateItem(ownerRoleKey: null);
            CollaborationItemDataScope scope = new CollaborationItemDataScope(
                actorUserId: 4242,
                isAdministrator: true,
                isParticipant: false,
                visibleOrganizationUnitIds: new List<int>(),
                holdsOwnerRoleKey: false);

            Assert.IsTrue(
                CollaborationItemDataScopePolicy.CanView(item, scope),
                "管理员仍应优先可见。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>构造向后兼容：不传负责人角色键参数时该维度默认为假，不授予可见性。</zh-CN>
        ///   <en>Constructor compatibility: omitting the owner-role-key parameter defaults the dimension to false and grants nothing.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Scope_DefaultsOwnerRoleKeyToFalse()
        {
            CollaborationItemDataScope scope = new CollaborationItemDataScope(
                4242,
                false,
                false,
                new List<int>());

            Assert.IsFalse(scope.HoldsOwnerRoleKey, "省略参数时负责人角色键维度必须默认为假。");

            CollaborationItemInfo item = CreateItem(ownerRoleKey: "Business.Collaboration.Handle");
            Assert.IsFalse(
                CollaborationItemDataScopePolicy.CanView(item, scope),
                "默认假值时不得授予可见性。");
        }

        /// <summary><lang><zh-CN>创建一个与当前动作人无关的协同事项投影。</zh-CN><en>Creates a collaboration-item projection unrelated to the current actor.</en></lang></summary>
        /// <param name="ownerRoleKey"><l><zh-CN>负责人角色键；可为空。</zh-CN><en>The owner role key; may be null.</en></l></param>
        /// <returns><l><zh-CN>协同事项投影。</zh-CN><en>The collaboration-item projection.</en></l></returns>
        private static CollaborationItemInfo CreateItem(string ownerRoleKey)
        {
            return new CollaborationItemInfo
            {
                InitiatorUserId = 7,
                OwnerUserId = null,
                OwnerRoleKey = ownerRoleKey,
                OrganizationUnitId = null
            };
        }
    }
}
