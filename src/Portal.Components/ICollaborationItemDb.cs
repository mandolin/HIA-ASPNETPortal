using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>企业协同事项和事项事件的数据访问契约。</zh-CN>
    ///   <en>Data-access contract for enterprise collaboration items and item events.</en>
    /// </lang>
    /// </summary>
    public interface ICollaborationItemDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>检查协同事项主表和事件表是否已部署。</zh-CN>
        ///   <en>Checks whether the collaboration-item fact and event tables are deployed.</en>
        /// </lang>
        /// </summary>
        bool IsSchemaAvailable();

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建并提交一条企业协同事项。</zh-CN>
        ///   <en>Creates and submits one enterprise collaboration item.</en>
        /// </lang>
        /// </summary>
        CollaborationItemResult CreateSubmittedItem(CollaborationItemCreateRequest request);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定用户最近发起或负责的协同事项。</zh-CN>
        ///   <en>Reads recent collaboration items initiated by or assigned to a specific user.</en>
        /// </lang>
        /// </summary>
        IList<CollaborationItemInfo> GetRecentItemsForUser(int userId, int take);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取后台协同事项列表。</zh-CN>
        ///   <en>Reads the administration collaboration-item list.</en>
        /// </lang>
        /// </summary>
        IList<CollaborationItemInfo> GetAdminItems(string status, int take);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前用户可见的事项时间线；服务端会重新验证参与者或管理员范围。</zh-CN>
        ///   <en>Reads the item timeline visible to the current user; the server revalidates participant or administrator scope.</en>
        /// </lang>
        /// </summary>
        IList<CollaborationItemEventInfo> GetVisibleEvents(long itemId, int actorUserId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>批量读取多个事项对当前用户可见的时间线，用于消除列表逐行读取造成的 N+1 查询。</zh-CN>
        ///   <en>Reads the timelines of several items visible to the current user in one batch, removing the N+1 queries caused by per-row reads in a list.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>语义与逐条 <see cref="GetVisibleEvents"/> 等价：动作人授权只计算一次，但**每个事项仍逐个执行 <c>CanParticipate</c> 与 <c>CanView</c> 校验**（安全约束不得为性能合并掉）；未被授权或不存在的事项在结果中为空列表，使调用方无需区分内部状态。返回的字典**包含全部请求标识**（无事件者为空列表），条目内顺序与逐条读取一致。空输入、schema 不可用或读取失败时返回全部为空列表的结果。</zh-CN>
        ///   <en>Semantically equivalent to the per-item <see cref="GetVisibleEvents"/>: actor authorization is computed once, yet <c>CanParticipate</c> and <c>CanView</c> are still evaluated **for every item individually** (a security constraint that must not be collapsed for performance). Items that are unauthorized or missing appear as empty lists so callers never distinguish internal states. The returned dictionary **contains every requested identifier** (empty list when no events exist) and preserves the per-item ordering of the single-item read. Empty input, unavailable schema, or read failure yields all-empty lists.</en>
        /// </lang>
        /// </remarks>
        /// <param name="itemIds">
        /// <l>
        ///   <zh-CN>待读取的事项标识集合，可为 <c>null</c>；非正数与重复项被丢弃，超出实现上限的部分被截断。</zh-CN>
        ///   <en>Item identifiers to read, possibly <c>null</c>; non-positive and duplicate entries are discarded, and entries beyond the implementation cap are truncated.</en>
        /// </l>
        /// </param>
        /// <param name="actorUserId">
        /// <l>
        ///   <zh-CN>当前动作人用户标识；无效时全部返回空列表。</zh-CN>
        ///   <en>Current actor user identifier; an invalid value yields all-empty lists.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>以事项标识为键的可见事件集合。</zh-CN>
        ///   <en>Visible events keyed by item identifier.</en>
        /// </l>
        /// </returns>
        IDictionary<long, IList<CollaborationItemEventInfo>> GetVisibleEventsForItems(IList<long> itemIds, int actorUserId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建不改变事项状态的纯文本评论。</zh-CN>
        ///   <en>Creates a plain-text comment that does not change item state.</en>
        /// </lang>
        /// </summary>
        CollaborationItemCommentResult AddComment(CollaborationItemCommentCreateRequest request);

        /// <summary>
        /// <lang>
        ///   <zh-CN>执行协同事项状态动作。</zh-CN>
        ///   <en>Applies a state action to a collaboration item.</en>
        /// </lang>
        /// </summary>
        CollaborationItemResult ApplyAction(CollaborationItemActionRequest request);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取事项的参与人集合。</zh-CN>
        ///   <en>Reads the participant set of an item.</en>
        /// </lang>
        /// </summary>
        IList<CollaborationItemParticipantInfo> GetParticipants(long itemId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>批量读取多个事项的参与人集合，用于消除列表逐行读取造成的 N+1 查询。</zh-CN>
        ///   <en>Reads the participant sets of several items in one batch, removing the N+1 queries caused by per-row reads in a list.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>语义与逐条 <see cref="GetParticipants"/> 等价：返回的字典**包含全部请求标识**（无参与人者为空列表），条目内按参与人标识升序，与逐条读取一致。空输入、参与人表缺失或读取失败时返回全部为空列表的结果。</zh-CN>
        ///   <en>Semantically equivalent to the per-item <see cref="GetParticipants"/>: the returned dictionary **contains every requested identifier** (empty list when there are no participants) and orders entries by participant identifier ascending, matching the single-item read. Empty input, a missing participant table, or read failure yields all-empty lists.</en>
        /// </lang>
        /// </remarks>
        /// <param name="itemIds">
        /// <l>
        ///   <zh-CN>待读取的事项标识集合，可为 <c>null</c>；非正数与重复项被丢弃，超出实现上限的部分被截断。</zh-CN>
        ///   <en>Item identifiers to read, possibly <c>null</c>; non-positive and duplicate entries are discarded, and entries beyond the implementation cap are truncated.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>以事项标识为键的参与人集合。</zh-CN>
        ///   <en>Participant sets keyed by item identifier.</en>
        /// </l>
        /// </returns>
        IDictionary<long, IList<CollaborationItemParticipantInfo>> GetParticipantsForItems(IList<long> itemIds);

        /// <summary>
        /// <lang>
        ///   <zh-CN>添加一个事项参与人；服务端重新校验操作者授权。</zh-CN>
        ///   <en>Adds one item participant; the server revalidates actor authorization.</en>
        /// </lang>
        /// </summary>
        CollaborationItemParticipantResult AddParticipant(CollaborationItemParticipantCreateRequest request);

        /// <summary>
        /// <lang>
        ///   <zh-CN>移除一个事项参与人；服务端重新校验操作者授权。</zh-CN>
        ///   <en>Removes one item participant; the server revalidates actor authorization.</en>
        /// </lang>
        /// </summary>
        CollaborationItemParticipantResult RemoveParticipant(long itemId, int userId, int actorUserId);
    }
}
