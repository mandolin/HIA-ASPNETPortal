using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>协同事项数据范围快照：一次服务端解析得到的动作人归属、参与与组织范围证据。</zh-CN>
    ///   <en>Collaboration-item data-scope snapshot: server-resolved evidence of an actor's ownership, participation, and organization scope.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本类型只承载判定所需的只读证据：不读取数据库、不推断角色继承、不缓存跨请求状态。组织范围集合为空表示"范围未知或没有任何组织范围"，此时组织维度不得授予可见性（fail-closed）。</zh-CN>
    ///   <en>This type carries only read-only evidence for the decision: it reads no database, infers no role inheritance, and caches no cross-request state. An empty organization-scope collection means "scope unknown or no organization scope at all", in which case the organization dimension must not grant visibility (fail-closed).</en>
    /// </lang>
    /// </remarks>
    public sealed class CollaborationItemDataScope
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>用服务端已解析的归属、参与和组织证据创建数据范围快照。</zh-CN>
        ///   <en>Creates a data-scope snapshot from server-resolved ownership, participation, and organization evidence.</en>
        /// </lang>
        /// </summary>
        /// <param name="actorUserId">
        /// <l>
        ///   <zh-CN>服务端重新解析得到的门户用户标识；非正值表示身份缺失，判定会直接拒绝。</zh-CN>
        ///   <en>Portal-user identifier re-resolved by the server; a non-positive value means the identity is missing and the decision denies directly.</en>
        /// </l>
        /// </param>
        /// <param name="isAdministrator">
        /// <l>
        ///   <zh-CN>服务端按角色或协同管理员权限计算出的管理员标记；该标记只延续既有集中查看能力，仍受上游权限门禁约束。</zh-CN>
        ///   <en>Administrator flag computed by the server from roles or the collaboration-admin permission; it only continues the existing centralized viewing capability and remains constrained by the upstream permission gate.</en>
        /// </l>
        /// </param>
        /// <param name="isParticipant">
        /// <l>
        ///   <zh-CN>当前用户是否属于事项参与人集合（协办或关注）；由调用方在服务端查询后传入。</zh-CN>
        ///   <en>Whether the current user belongs to the item participant set (Collaborator or Watcher); supplied by the caller after a server-side query.</en>
        /// </l>
        /// </param>
        /// <param name="visibleOrganizationUnitIds">
        /// <l>
        ///   <zh-CN>当前用户可见组织单元标识集合；可为空引用，空集合按"范围未知"处理。</zh-CN>
        ///   <en>Organization-unit identifiers visible to the current user; may be null, and an empty collection is treated as "scope unknown".</en>
        /// </l>
        /// </param>
        public CollaborationItemDataScope(int actorUserId, bool isAdministrator, bool isParticipant, IEnumerable<int> visibleOrganizationUnitIds)
        {
            // <lang>
            //   <zh-CN>先固定身份、管理员和参与三项标量证据，使后续判定只依赖本次快照而不依赖可变入参。</zh-CN>
            //   <en>First freeze the scalar evidence of identity, administrator, and participation so later decisions depend only on this snapshot rather than on mutable inputs.</en>
            // </lang>
            ActorUserId = actorUserId;
            IsAdministrator = isAdministrator;
            IsParticipant = isParticipant;

            // <lang>
            //   <zh-CN>复制组织标识集合并丢弃非正值、去重后固定为只读列表，避免调用方后续修改集合影响已完成的判定。</zh-CN>
            //   <en>Copy the organization identifiers, drop non-positive values, de-duplicate, and freeze them into a read-only list so later caller mutation cannot change an already computed decision.</en>
            // </lang>
            VisibleOrganizationUnitIds = (visibleOrganizationUnitIds ?? Enumerable.Empty<int>())
                .Where(unitId => unitId > 0)
                .Distinct()
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>服务端重新解析得到的门户用户标识。</zh-CN>
        ///   <en>Portal-user identifier re-resolved by the server.</en>
        /// </lang>
        /// </summary>
        public int ActorUserId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>服务端计算出的管理员标记。</zh-CN>
        ///   <en>Administrator flag computed by the server.</en>
        /// </lang>
        /// </summary>
        public bool IsAdministrator { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前用户是否属于事项参与人集合。</zh-CN>
        ///   <en>Whether the current user belongs to the item participant set.</en>
        /// </lang>
        /// </summary>
        public bool IsParticipant { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前用户可见组织单元标识的只读集合；为空表示组织维度不授予可见性。</zh-CN>
        ///   <en>Read-only collection of organization-unit identifiers visible to the current user; empty means the organization dimension grants no visibility.</en>
        /// </lang>
        /// </summary>
        public IList<int> VisibleOrganizationUnitIds { get; private set; }
    }
}
