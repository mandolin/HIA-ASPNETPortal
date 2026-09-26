using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>协同事项数据范围可见性的纯决策策略：只依据服务端已解析的证据回答"这条数据是否可见"。</zh-CN>
    ///   <en>Pure decision policy for collaboration-item data-scope visibility: it answers "is this row visible" only from server-resolved evidence.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本类型是 P63.1 数据范围契约的唯一判定入口，供列表与详情共用，保证两条读取路径使用同一规则。判定遵循 fail-closed：证据缺失、身份缺失或范围信息不足时一律拒绝，而不是放行或返回"部分可见"。本策略不读取数据库、不解析配置、不接触 HTTP 上下文，因此可在单测中直接验证。</zh-CN>
    ///   <en>This type is the single decision entry point of the P63.1 data-scope contract, shared by list and detail reads so both paths use one rule. The decision is fail-closed: missing evidence, missing identity, or insufficient scope information always denies rather than allows or returns a "partially visible" state. The policy reads no database, no configuration, and no HTTP context, so it can be verified directly in unit tests.</en>
    /// </lang>
    /// </remarks>
    public static class CollaborationItemDataScopePolicy
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>按归属、参与人和组织三个维度判定当前动作人是否可见指定协同事项。</zh-CN>
        ///   <en>Decides whether the current actor may view the specified collaboration item through the ownership, participant, and organization dimensions.</en>
        /// </lang>
        /// </summary>
        /// <param name="item">
        /// <l>
        ///   <zh-CN>待判定的协同事项投影；为空引用时判定失败。</zh-CN>
        ///   <en>Collaboration-item projection to evaluate; a null reference fails the decision.</en>
        /// </l>
        /// </param>
        /// <param name="scope">
        /// <l>
        ///   <zh-CN>服务端解析出的当前动作人数据范围快照；为空引用时判定失败。</zh-CN>
        ///   <en>Server-resolved data-scope snapshot of the current actor; a null reference fails the decision.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>任一维度成立时为 <c>true</c>；证据不足或全部维度不成立时为 <c>false</c>，且不区分"不存在"与"无权访问"。</zh-CN>
        ///   <en><c>true</c> when any dimension holds; <c>false</c> when evidence is insufficient or all dimensions fail, without distinguishing "does not exist" from "not authorized".</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>维度判定顺序为：管理员延续既有集中查看能力 → 归属（发起人或负责人）→ 参与人集合 → 组织范围。组织维度要求事项组织单元标识存在且落在可见集合内；事项未标注组织或可见集合为空时该维度不授予可见性。</zh-CN>
        ///   <en>The dimension order is: administrator continues the existing centralized viewing capability, then ownership (initiator or owner), then the participant set, then the organization scope. The organization dimension requires the item to carry an organization-unit identifier that falls inside the visible collection; an item without an organization unit or an empty visible collection grants nothing through this dimension.</en>
        /// </lang>
        /// </remarks>
        public static bool CanView(CollaborationItemInfo item, CollaborationItemDataScope scope)
        {
            // <lang>
            //   <zh-CN>事项、范围快照或身份任一缺失都直接拒绝：无法确定范围时按越权处理，不退回"放行"。</zh-CN>
            //   <en>Deny directly when the item, the scope snapshot, or the identity is missing: an undeterminable scope is treated as out-of-scope instead of falling back to "allow".</en>
            // </lang>
            if (item == null || scope == null || scope.ActorUserId <= 0)
            {
                return false;
            }

            // <lang>
            //   <zh-CN>管理员继续沿用既有集中查看能力；该能力仍由上游协同管理员权限门禁约束，不在此处推断角色继承。</zh-CN>
            //   <en>Administrators continue to use the existing centralized viewing capability; that capability remains constrained by the upstream collaboration-admin permission gate and no role inheritance is inferred here.</en>
            // </lang>
            if (scope.IsAdministrator)
            {
                return true;
            }

            // <lang>
            //   <zh-CN>归属维度：发起人或负责人为当前用户时可见；负责人可空，只有存在值才参与比较。</zh-CN>
            //   <en>Ownership dimension: visible when the initiator or the owner is the current user; the owner is nullable and participates only when a value exists.</en>
            // </lang>
            if (item.InitiatorUserId == scope.ActorUserId ||
                (item.OwnerUserId.HasValue && item.OwnerUserId.Value == scope.ActorUserId))
            {
                return true;
            }

            // <lang>
            //   <zh-CN>参与人维度：由调用方在服务端查询参与人集合后传入，协办与关注共用同一入口。</zh-CN>
            //   <en>Participant dimension: supplied by the caller after a server-side participant-set query; Collaborator and Watcher share the same entry.</en>
            // </lang>
            if (scope.IsParticipant)
            {
                return true;
            }

            // <lang>
            //   <zh-CN>组织维度：事项必须标注组织单元且该单元落在可见组织范围内；空可见集合按范围未知处理并拒绝。</zh-CN>
            //   <en>Organization dimension: the item must carry an organization unit that falls inside the visible organization scope; an empty visible collection is treated as unknown scope and denies.</en>
            // </lang>
            return item.OrganizationUnitId.HasValue &&
                   scope.VisibleOrganizationUnitIds.Contains(item.OrganizationUnitId.Value);
        }
    }
}
