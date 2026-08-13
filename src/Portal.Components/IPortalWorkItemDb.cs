using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>轻量审批/待办基础的数据访问契约。</zh-CN>
    ///   <en>Data-access contract for the lightweight approval/work-item foundation.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P12.3 第一版只负责业务对象到待办记录的最小投影，不实现 BPM、转办、会签或外部通知。该门面不校验业务对象权限，也不与调用方的业务事务形成原子边界；调用方必须先完成授权和领域写入，并把待办失败作为补充能力降级处理。</zh-CN>
    ///   <en>The first P12.3 version only projects business objects into minimal work-item records; it does not implement BPM, delegation, co-signing, or external notifications. This facade validates neither business-object authorization nor an atomic boundary with the caller's domain transaction. Callers must authorize and persist the domain action first, then treat work-item failures as degradation of supplemental capability.</en>
    /// </lang>
    /// </remarks>
    public interface IPortalWorkItemDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>检查当前数据源能否同时访问待办表和事件表。</zh-CN>
        ///   <en>Checks whether the current data source can access both the work-item and event tables.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>返回值是一次可用性探测，不保证随后写入成功，也不证明当前用户有权读取或处理任何业务对象。表缺失、元数据查询失败或连接异常都可表现为 <c>false</c>。</zh-CN>
        ///   <en>The result is a point-in-time availability probe. It guarantees neither a later successful write nor authorization for the current user to read or handle any business object. Missing tables, metadata-query failures, and connection failures may all appear as <c>false</c>.</en>
        /// </lang>
        /// </remarks>
        /// <returns>
        /// <l>
        ///   <zh-CN>两个表在探测时均可访问为 <c>true</c>；否则为 <c>false</c>。</zh-CN>
        ///   <en><c>true</c> when both tables are accessible at probe time; otherwise <c>false</c>.</en>
        /// </l>
        /// </returns>
        bool IsSchemaAvailable();

        /// <summary>
        /// <lang>
        ///   <zh-CN>为业务对象创建待办，或复用其最新的未完成待办。</zh-CN>
        ///   <en>Creates a work item for a business object or reuses its newest unfinished work item.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>调用方仍拥有请求对象；实现可复制、裁剪和补默认值，但不得依赖本方法完成业务授权。复用路径不会承诺刷新既有标题、摘要、指派或到期时间。调用方应根据 <see cref="PortalWorkItemResult.Succeeded"/> 判断结果，不把可展示消息当作授权或幂等令牌。</zh-CN>
        ///   <en>The caller retains ownership of the request object. An implementation may copy, trim, and default its values, but this method must not be relied on for business authorization. The reuse path does not promise to refresh an existing title, summary, assignment, or due time. Callers should inspect <see cref="PortalWorkItemResult.Succeeded"/> and must not treat its display message as authorization or an idempotency token.</en>
        /// </lang>
        /// </remarks>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>创建参数；当前实现接受空引用并返回失败结果，必填业务键、标题和办理主体由数据层归一化后校验。</zh-CN>
        ///   <en>Creation parameters. The current implementation accepts a null reference and returns a failure result; the data layer normalizes and validates the required business key, title, and assignee.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>成功时包含可复用或新建的待办标识；验证、架构或运行失败时返回低敏失败结果。</zh-CN>
        ///   <en>A low-sensitivity result containing the reused or newly created work-item identifier on success, or a failure for validation, schema, or runtime errors.</en>
        /// </l>
        /// </returns>
        PortalWorkItemResult EnsureWorkItem(PortalWorkItemCreateRequest request);

        /// <summary>
        /// <lang>
        ///   <zh-CN>把业务对象对应的未完成待办更新为调用方指定的目标状态并记录事件。</zh-CN>
        ///   <en>Updates the unfinished work item for a business object to the caller-supplied target status and records an event.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该契约不更新业务对象、不验证状态迁移，也不检查事件类型或目标状态是否来自稳定常量。调用方必须先完成领域动作和授权，并传入受控的 <see cref="PortalWorkItemEventTypes"/> 与 <see cref="PortalWorkItemStatuses"/> 值；重复调用在没有未完成待办时可返回失败。</zh-CN>
        ///   <en>This contract neither updates the business object, validates a state transition, nor checks that event and target-status values come from the stable constants. Callers must first complete and authorize the domain action, then supply controlled <see cref="PortalWorkItemEventTypes"/> and <see cref="PortalWorkItemStatuses"/> values. A repeated call may fail when no unfinished work item remains.</en>
        /// </lang>
        /// </remarks>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>状态更新参数；当前实现接受空引用并返回失败结果，且会复制、裁剪并补齐时间与操作者默认值。</zh-CN>
        ///   <en>State-update parameters. The current implementation accepts a null reference and returns a failure result, and it copies, trims, and defaults the time and actor values.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>成功时包含被更新待办标识；未找到未完成待办、验证、架构或运行失败时返回低敏失败结果。</zh-CN>
        ///   <en>A low-sensitivity result containing the updated work-item identifier on success, or a failure when no unfinished item is found or validation, schema, or runtime processing fails.</en>
        /// </l>
        /// </returns>
        PortalWorkItemResult CompleteBusinessWorkItem(PortalWorkItemCompletionRequest request);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取后台待办的有界、最新优先查询快照。</zh-CN>
        ///   <en>Reads a bounded, newest-first query snapshot of administration work items.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>本方法不实施后台页面授权；调用方必须先限制访问。返回列表及其 DTO 是可变的时间点投影，不自动反映并发更新。当前实现对架构不可用或查询异常返回空列表，因此空列表不能区分“没有数据”和“读取失败”。</zh-CN>
        ///   <en>This method does not enforce administration-page authorization; callers must restrict access first. The returned list and DTOs are mutable point-in-time projections and do not automatically reflect concurrent updates. The current implementation returns an empty list for unavailable schema or query failures, so an empty result cannot distinguish “no data” from “read failure.”</en>
        /// </lang>
        /// </remarks>
        /// <param name="status">
        /// <l>
        ///   <zh-CN>精确状态筛选；空白表示全部。当前实现只裁剪文本，不校验是否为 <see cref="PortalWorkItemStatuses"/> 常量。</zh-CN>
        ///   <en>Exact status filter; blank means all statuses. The current implementation only trims the text and does not validate it against <see cref="PortalWorkItemStatuses"/> constants.</en>
        /// </l>
        /// </param>
        /// <param name="take">
        /// <l>
        ///   <zh-CN>期望最大条数；当前实现对非正数使用 50，并把上限限制为 200。</zh-CN>
        ///   <en>Requested maximum row count; the current implementation uses 50 for non-positive values and caps the count at 200.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>最新优先的可变待办投影列表；读取失败时也可为空。</zh-CN>
        ///   <en>A mutable newest-first list of work-item projections, which may also be empty when reading fails.</en>
        /// </l>
        /// </returns>
        IList<PortalWorkItemInfo> GetAdminWorkItems(string status, int take);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定待办最近发生优先的事件快照。</zh-CN>
        ///   <en>Reads a newest-occurrence-first event snapshot for the specified work item.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>待办标识只用于查询定位，不证明调用方可查看对应业务对象。返回事件是可变历史投影，不能替代当前状态或授权事实。当前实现对非法标识、架构不可用或查询异常均返回空列表。</zh-CN>
        ///   <en>The work-item identifier is only a query locator and does not prove that the caller may view the associated business object. Returned events are mutable historical projections and cannot replace current-state or authorization facts. The current implementation returns an empty list for invalid identifiers, unavailable schema, and query failures.</en>
        /// </lang>
        /// </remarks>
        /// <param name="workItemId">
        /// <l>
        ///   <zh-CN>正数待办标识；非正数返回空列表。</zh-CN>
        ///   <en>Positive work-item identifier; a non-positive value yields an empty list.</en>
        /// </l>
        /// </param>
        /// <param name="take">
        /// <l>
        ///   <zh-CN>期望最大条数；当前实现对非正数使用 20，并把上限限制为 200。</zh-CN>
        ///   <en>Requested maximum row count; the current implementation uses 20 for non-positive values and caps the count at 200.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>最近发生优先的可变事件投影列表；非法输入或读取失败时也可为空。</zh-CN>
        ///   <en>A mutable newest-occurrence-first list of event projections, which may also be empty for invalid input or read failures.</en>
        /// </l>
        /// </returns>
        IList<PortalWorkItemEventInfo> GetWorkItemEvents(long workItemId, int take);
    }
}
