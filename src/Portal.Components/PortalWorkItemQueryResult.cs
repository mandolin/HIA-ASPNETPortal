using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>待办查询的只读结果，用于区分"查到零条"与"读取失败"。</zh-CN>
    ///   <en>Read-only result of a work-item query that distinguishes "zero rows found" from "read failure".</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>既有待办读取契约把架构缺失与查询异常统一返回空列表，因此空列表无法区分"没有数据"和"读取失败"。本类型把成功标记与投影列表放在一起，使前台能够分别呈现空态与失败态，而不必把异常细节暴露给用户。列表本身仍是可变的时间点投影，不自动反映并发更新。</zh-CN>
    ///   <en>The established work-item read contracts return an empty list both for unavailable schema and for query failures, so an empty result cannot distinguish "no data" from "read failure". This type pairs a success flag with the projection list so a front end can render empty and failure states separately without exposing exception details to the user. The list itself remains a mutable point-in-time projection and does not automatically reflect concurrent updates.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalWorkItemQueryResult
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>指示本次查询是否成功完成；失败时 <see cref="Items"/> 为空且不表示"确实没有待办"。</zh-CN>
        ///   <en>Indicates whether the query completed successfully. When it is <c>false</c>, <see cref="Items"/> is empty and does not mean "there are definitely no work items".</en>
        /// </lang>
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>最新优先的待办投影列表；成功且无命中时为空列表，永不为空引用。</zh-CN>
        ///   <en>A newest-first list of work-item projections. It is an empty list on a successful query with no matches and is never a null reference.</en>
        /// </lang>
        /// </summary>
        public IList<PortalWorkItemInfo> Items { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>用成功标记与投影列表初始化查询结果；列表为空引用时归一化为只读空列表。</zh-CN>
        ///   <en>Initializes the query result with a success flag and a projection list; a null list is normalized to an empty read-only list.</en>
        /// </lang>
        /// </summary>
        /// <param name="succeeded">
        /// <l>
        ///   <zh-CN>查询是否成功完成。</zh-CN>
        ///   <en>Whether the query completed successfully.</en>
        /// </l>
        /// </param>
        /// <param name="items">
        /// <l>
        ///   <zh-CN>待办投影列表；可为空引用，此时归一化为只读空列表。</zh-CN>
        ///   <en>The work-item projection list; it may be null, in which case it is normalized to an empty read-only list.</en>
        /// </l>
        /// </param>
        public PortalWorkItemQueryResult(bool succeeded, IList<PortalWorkItemInfo> items)
        {
            Succeeded = succeeded;
            Items = items ?? (IList<PortalWorkItemInfo>)new List<PortalWorkItemInfo>();
        }
    }
}
