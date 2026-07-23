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
    ///   <zh-CN>P12.3 第一版只负责业务对象到待办记录的最小映射，不实现 BPM、转办、会签或外部通知。</zh-CN>
    ///   <en>The first P12.3 version maps business objects to minimal work-item records only; it does not implement BPM, delegation, co-signing, or external notifications.</en>
    /// </lang>
    /// </remarks>
    public interface IPortalWorkItemDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>检查待办表和事件表是否已部署。</zh-CN>
        ///   <en>Checks whether the work-item and event tables are deployed.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>表可用时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the tables are available.</en>
        /// </l>
        /// </returns>
        bool IsSchemaAvailable();

        /// <summary>
        /// <lang>
        ///   <zh-CN>为业务对象创建或复用一个未完成待办。</zh-CN>
        ///   <en>Creates or reuses an unfinished work item for a business object.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>创建参数。</zh-CN>
        ///   <en>Creation parameters.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>写入结果。</zh-CN>
        ///   <en>Write result.</en>
        /// </l>
        /// </returns>
        PortalWorkItemResult EnsureWorkItem(PortalWorkItemCreateRequest request);

        /// <summary>
        /// <lang>
        ///   <zh-CN>完成或取消业务对象对应的未完成待办。</zh-CN>
        ///   <en>Completes or cancels the unfinished work item for a business object.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>完成参数。</zh-CN>
        ///   <en>Completion parameters.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>写入结果。</zh-CN>
        ///   <en>Write result.</en>
        /// </l>
        /// </returns>
        PortalWorkItemResult CompleteBusinessWorkItem(PortalWorkItemCompletionRequest request);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取后台待办列表。</zh-CN>
        ///   <en>Reads the administration work-item list.</en>
        /// </lang>
        /// </summary>
        /// <param name="status">
        /// <l>
        ///   <zh-CN>状态筛选；空值表示全部。</zh-CN>
        ///   <en>Status filter; empty means all statuses.</en>
        /// </l>
        /// </param>
        /// <param name="take">
        /// <l>
        ///   <zh-CN>最多返回条数。</zh-CN>
        ///   <en>Maximum number of rows to return.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>待办列表。</zh-CN>
        ///   <en>Work-item list.</en>
        /// </l>
        /// </returns>
        IList<PortalWorkItemInfo> GetAdminWorkItems(string status, int take);

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定待办的最近事件。</zh-CN>
        ///   <en>Reads recent events for the specified work item.</en>
        /// </lang>
        /// </summary>
        /// <param name="workItemId">
        /// <l>
        ///   <zh-CN>待办标识。</zh-CN>
        ///   <en>Work-item identifier.</en>
        /// </l>
        /// </param>
        /// <param name="take">
        /// <l>
        ///   <zh-CN>最多返回条数。</zh-CN>
        ///   <en>Maximum number of rows to return.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>事件列表。</zh-CN>
        ///   <en>Event list.</en>
        /// </l>
        /// </returns>
        IList<PortalWorkItemEventInfo> GetWorkItemEvents(long workItemId, int take);
    }
}
