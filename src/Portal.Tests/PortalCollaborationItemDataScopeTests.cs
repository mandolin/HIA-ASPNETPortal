using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPNET.StarterKit.Portal.Tests
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>覆盖协同事项数据范围可见性纯策略的契约测试。</zh-CN>
    ///   <en>Contract tests covering the pure collaboration-item data-scope visibility policy.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>测试只构造内存中的事项投影和范围快照并直接调用 <see cref="CollaborationItemDataScopePolicy"/>：不读取配置、不连接数据库、不接触 HTTP 上下文，也不使用任何真实账号或凭据。重点证明"范围外用户不可见"与"证据不足时 fail-closed"。</zh-CN>
    ///   <en>The tests build in-memory item projections and scope snapshots and call <see cref="CollaborationItemDataScopePolicy"/> directly: they read no configuration, connect to no database, touch no HTTP context, and use no real account or credential. They focus on proving that out-of-scope users see nothing and that insufficient evidence fails closed.</en>
    /// </lang>
    /// </remarks>
    [TestClass]
    public sealed class PortalCollaborationItemDataScopeTests
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>验证事项或范围快照缺失时一律拒绝，不返回"部分可见"也不放行。</zh-CN>
        ///   <en>Verifies that a missing item or scope snapshot always denies instead of allowing or returning a partially visible result.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void CanView_ReturnsFalseWhenEvidenceIsMissing()
        {
            Assert.IsFalse(
                CollaborationItemDataScopePolicy.CanView(null, CreateScope(7, false, false, null)),
                "事项缺失时应直接拒绝。");
            Assert.IsFalse(
                CollaborationItemDataScopePolicy.CanView(CreateItem(1, 2, null, 3), null),
                "范围快照缺失时应直接拒绝。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证动作人身份缺失（非正值用户标识）时拒绝，避免匿名或未解析身份获得可见性。</zh-CN>
        ///   <en>Verifies that a missing actor identity (non-positive user identifier) denies so anonymous or unresolved identities gain no visibility.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void CanView_ReturnsFalseWhenActorIdentityIsMissing()
        {
            CollaborationItemInfo item = CreateItem(1, 2, null, 3);

            Assert.IsFalse(
                CollaborationItemDataScopePolicy.CanView(item, CreateScope(0, false, true, new[] { 3 })),
                "动作人标识为 0 时应拒绝，即使其他维度成立。");
            Assert.IsFalse(
                CollaborationItemDataScopePolicy.CanView(item, CreateScope(-1, true, true, new[] { 3 })),
                "负的动作人标识同样应拒绝。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证归属维度：发起人与负责人各自可见，且不依赖组织范围。</zh-CN>
        ///   <en>Verifies the ownership dimension: initiator and owner are each visible independently of the organization scope.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void CanView_GrantsInitiatorAndOwner()
        {
            CollaborationItemInfo initiatorItem = CreateItem(1, 5, 11, 9);
            CollaborationItemInfo ownerItem = CreateItem(2, 11, 5, 9);

            Assert.IsTrue(
                CollaborationItemDataScopePolicy.CanView(initiatorItem, CreateScope(5, false, false, null)),
                "发起人在没有任何组织范围时仍可见本事项。");
            Assert.IsTrue(
                CollaborationItemDataScopePolicy.CanView(ownerItem, CreateScope(5, false, false, null)),
                "负责人在没有任何组织范围时仍可见本事项。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证参与人维度：既非发起人也非负责人，但在参与人集合内时可见。</zh-CN>
        ///   <en>Verifies the participant dimension: visible when the actor is neither initiator nor owner but belongs to the participant set.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void CanView_GrantsParticipant()
        {
            CollaborationItemInfo item = CreateItem(1, 21, 22, 30);

            Assert.IsTrue(
                CollaborationItemDataScopePolicy.CanView(item, CreateScope(7, false, true, null)),
                "参与人集合成员应可见，即使组织范围为空。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证组织维度：事项组织单元落在可见范围内时可见。</zh-CN>
        ///   <en>Verifies the organization dimension: visible when the item's organization unit falls inside the visible scope.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void CanView_GrantsOrganizationScope()
        {
            CollaborationItemInfo item = CreateItem(1, 31, 32, 40);

            Assert.IsTrue(
                CollaborationItemDataScopePolicy.CanView(item, CreateScope(8, false, false, new[] { 40, 41 })),
                "事项组织单元落在可见范围内时应可见。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证范围外用户不可见：非归属、非参与人且组织不匹配时拒绝。</zh-CN>
        ///   <en>Verifies that out-of-scope users see nothing: deny when the actor is neither owner, nor participant, nor inside the item's organization.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void CanView_DeniesOutOfScopeUser()
        {
            CollaborationItemInfo item = CreateItem(1, 41, 42, 50);
            CollaborationItemDataScope scope = CreateScope(9, false, false, new[] { 60, 61 });

            Assert.IsFalse(
                CollaborationItemDataScopePolicy.CanView(item, scope),
                "范围外用户读取该事项时必须不可见。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证组织范围未知时拒绝：可见集合为空且事项标注了组织单元时不授予可见性。</zh-CN>
        ///   <en>Verifies denial when the organization scope is unknown: an empty visible collection grants nothing for an item that carries an organization unit.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void CanView_DeniesWhenOrganizationScopeIsUnknown()
        {
            CollaborationItemInfo item = CreateItem(1, 51, 52, 70);

            Assert.IsFalse(
                CollaborationItemDataScopePolicy.CanView(item, CreateScope(10, false, false, null)),
                "组织范围为空引用时按未知处理并拒绝。");
            Assert.IsFalse(
                CollaborationItemDataScopePolicy.CanView(item, CreateScope(10, false, false, new int[0])),
                "组织范围为空集合时同样拒绝。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证事项未标注组织单元时不因组织维度放宽；只有归属或参与维度可授予可见性。</zh-CN>
        ///   <en>Verifies that an item without an organization unit is not widened through the organization dimension; only ownership or participation can grant visibility.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void CanView_DeniesUnscopedItemForUnrelatedUser()
        {
            CollaborationItemInfo item = CreateItem(1, 61, 62, null);

            Assert.IsFalse(
                CollaborationItemDataScopePolicy.CanView(item, CreateScope(11, false, false, new[] { 80 })),
                "事项没有组织单元时，仅凭可见组织范围不得授予可见性。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证管理员继续沿用既有集中查看能力，该能力仍由上游权限门禁约束。</zh-CN>
        ///   <en>Verifies that administrators continue to use the existing centralized viewing capability, which remains constrained by the upstream permission gate.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void CanView_GrantsAdministrator()
        {
            CollaborationItemInfo item = CreateItem(1, 71, 72, 90);

            Assert.IsTrue(
                CollaborationItemDataScopePolicy.CanView(item, CreateScope(12, true, false, null)),
                "管理员可读取范围外事项；该能力由上游协同管理员权限门禁约束。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>验证范围快照会规范化组织标识：空引用转空集合、丢弃非正值并去重。</zh-CN>
        ///   <en>Verifies that the scope snapshot normalizes organization identifiers: null becomes an empty collection, non-positive values are dropped, and duplicates are removed.</en>
        /// </lang>
        /// </summary>
        [TestMethod]
        public void Scope_NormalizesVisibleOrganizationUnitIds()
        {
            CollaborationItemDataScope emptyScope = new CollaborationItemDataScope(3, false, false, null);
            Assert.AreEqual(0, emptyScope.VisibleOrganizationUnitIds.Count, "空引用应规范化为空集合。");

            CollaborationItemDataScope normalizedScope = new CollaborationItemDataScope(3, false, false, new[] { 4, 0, -1, 4, 5 });
            Assert.AreEqual(2, normalizedScope.VisibleOrganizationUnitIds.Count, "应丢弃非正值并去重。");
            CollectionAssert.AreEqual(
                new List<int> { 4, 5 },
                new List<int>(normalizedScope.VisibleOrganizationUnitIds),
                "规范化后应保留原始顺序的有效组织标识。");
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>构造仅含判定所需字段的协同事项投影，避免测试依赖数据库读取结果。</zh-CN>
        ///   <en>Builds a collaboration-item projection carrying only the fields the decision needs so tests never depend on database reads.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>事项标识，仅用于区分用例，不参与判定。</zh-CN>
        ///   <en>Item identifier used only to distinguish cases; it does not participate in the decision.</en>
        /// </l>
        /// </param>
        /// <param name="initiatorUserId">
        /// <l>
        ///   <zh-CN>发起人门户用户标识。</zh-CN>
        ///   <en>Initiator Portal user identifier.</en>
        /// </l>
        /// </param>
        /// <param name="ownerUserId">
        /// <l>
        ///   <zh-CN>负责人门户用户标识，可为空。</zh-CN>
        ///   <en>Owner Portal user identifier; may be null.</en>
        /// </l>
        /// </param>
        /// <param name="organizationUnitId">
        /// <l>
        ///   <zh-CN>事项所属组织单元标识，可为空。</zh-CN>
        ///   <en>Owning organization-unit identifier of the item; may be null.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>仅用于判定的内存事项投影。</zh-CN>
        ///   <en>In-memory item projection used only by the decision.</en>
        /// </l>
        /// </returns>
        private static CollaborationItemInfo CreateItem(long itemId, int initiatorUserId, int? ownerUserId, int? organizationUnitId)
        {
            return new CollaborationItemInfo
            {
                ItemId = itemId,
                InitiatorUserId = initiatorUserId,
                OwnerUserId = ownerUserId,
                OrganizationUnitId = organizationUnitId
            };
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>构造一次服务端解析后的数据范围快照，供纯策略判定使用。</zh-CN>
        ///   <en>Builds a server-resolved data-scope snapshot for use by the pure policy decision.</en>
        /// </lang>
        /// </summary>
        /// <param name="actorUserId">
        /// <l>
        ///   <zh-CN>动作人门户用户标识。</zh-CN>
        ///   <en>Actor Portal user identifier.</en>
        /// </l>
        /// </param>
        /// <param name="isAdministrator">
        /// <l>
        ///   <zh-CN>服务端计算出的管理员标记。</zh-CN>
        ///   <en>Administrator flag computed by the server.</en>
        /// </l>
        /// </param>
        /// <param name="isParticipant">
        /// <l>
        ///   <zh-CN>动作人是否属于事项参与人集合。</zh-CN>
        ///   <en>Whether the actor belongs to the item participant set.</en>
        /// </l>
        /// </param>
        /// <param name="visibleOrganizationUnitIds">
        /// <l>
        ///   <zh-CN>动作人可见组织单元标识集合，可为空引用。</zh-CN>
        ///   <en>Organization-unit identifiers visible to the actor; may be null.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>仅用于判定的内存范围快照。</zh-CN>
        ///   <en>In-memory scope snapshot used only by the decision.</en>
        /// </l>
        /// </returns>
        private static CollaborationItemDataScope CreateScope(int actorUserId, bool isAdministrator, bool isParticipant, int[] visibleOrganizationUnitIds)
        {
            return new CollaborationItemDataScope(actorUserId, isAdministrator, isParticipant, visibleOrganizationUnitIds);
        }
    }
}
