using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：轻量审批/待办基础的数据访问契约。
    ///
    /// English: Data-access contract for the lightweight approval/work-item foundation.
    /// </summary>
    /// <remarks>
    /// 中文：P12.3 第一版只负责业务对象到待办记录的最小映射，不实现 BPM、转办、会签或外部通知。
    ///
    /// English: The first P12.3 version maps business objects to minimal work-item records only; it does not implement
    /// BPM, delegation, co-signing, or external notifications.
    /// </remarks>
    public interface IPortalWorkItemDb
    {
        /// <summary>
        /// 中文：检查待办表和事件表是否已部署。
        ///
        /// English: Checks whether the work-item and event tables are deployed.
        /// </summary>
        /// <returns>中文：表可用时为 <c>true</c>。English: <c>true</c> when the tables are available.</returns>
        bool IsSchemaAvailable();

        /// <summary>
        /// 中文：为业务对象创建或复用一个未完成待办。
        ///
        /// English: Creates or reuses an unfinished work item for a business object.
        /// </summary>
        /// <param name="request">中文：创建参数。English: Creation parameters.</param>
        /// <returns>中文：写入结果。English: Write result.</returns>
        PortalWorkItemResult EnsureWorkItem(PortalWorkItemCreateRequest request);

        /// <summary>
        /// 中文：完成或取消业务对象对应的未完成待办。
        ///
        /// English: Completes or cancels the unfinished work item for a business object.
        /// </summary>
        /// <param name="request">中文：完成参数。English: Completion parameters.</param>
        /// <returns>中文：写入结果。English: Write result.</returns>
        PortalWorkItemResult CompleteBusinessWorkItem(PortalWorkItemCompletionRequest request);

        /// <summary>
        /// 中文：读取后台待办列表。
        ///
        /// English: Reads the administration work-item list.
        /// </summary>
        /// <param name="status">中文：状态筛选；空值表示全部。English: Status filter; empty means all statuses.</param>
        /// <param name="take">中文：最多返回条数。English: Maximum number of rows to return.</param>
        /// <returns>中文：待办列表。English: Work-item list.</returns>
        IList<PortalWorkItemInfo> GetAdminWorkItems(string status, int take);

        /// <summary>
        /// 中文：读取指定待办的最近事件。
        ///
        /// English: Reads recent events for the specified work item.
        /// </summary>
        /// <param name="workItemId">中文：待办标识。English: Work-item identifier.</param>
        /// <param name="take">中文：最多返回条数。English: Maximum number of rows to return.</param>
        /// <returns>中文：事件列表。English: Event list.</returns>
        IList<PortalWorkItemEventInfo> GetWorkItemEvents(long workItemId, int take);
    }
}
