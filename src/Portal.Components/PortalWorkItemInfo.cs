using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>轻量待办的后台查询投影。</zh-CN>
    ///   <en>Administration query projection for a lightweight work item.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalWorkItemInfo
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>待办标识。</zh-CN>
        ///   <en>Work-item identifier.</en>
        /// </lang>
        /// </summary>
        public long WorkItemId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>业务对象类型。</zh-CN>
        ///   <en>Business-object kind.</en>
        /// </lang>
        /// </summary>
        public string BusinessKind { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>业务对象标识。</zh-CN>
        ///   <en>Business-object identifier.</en>
        /// </lang>
        /// </summary>
        public string BusinessId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>标题。</zh-CN>
        ///   <en>Title.</en>
        /// </lang>
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>低敏摘要。</zh-CN>
        ///   <en>Low-sensitivity summary.</en>
        /// </lang>
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>待办状态。</zh-CN>
        ///   <en>Work-item status.</en>
        /// </lang>
        /// </summary>
        public string WorkItemStatus { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>指定办理用户标识。</zh-CN>
        ///   <en>Assigned user identifier.</en>
        /// </lang>
        /// </summary>
        public int? AssignedUserId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>指定办理用户名。</zh-CN>
        ///   <en>Assigned user name.</en>
        /// </lang>
        /// </summary>
        public string AssignedUserName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>指定办理角色或权限键。</zh-CN>
        ///   <en>Assigned role or permission key.</en>
        /// </lang>
        /// </summary>
        public string AssignedRoleKey { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建 UTC 时间。</zh-CN>
        ///   <en>Creation UTC time.</en>
        /// </lang>
        /// </summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建者。</zh-CN>
        ///   <en>Creator.</en>
        /// </lang>
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>到期 UTC 时间。</zh-CN>
        ///   <en>Due UTC time.</en>
        /// </lang>
        /// </summary>
        public DateTime? DueUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>完成 UTC 时间。</zh-CN>
        ///   <en>Completion UTC time.</en>
        /// </lang>
        /// </summary>
        public DateTime? CompletedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>完成人。</zh-CN>
        ///   <en>Completer.</en>
        /// </lang>
        /// </summary>
        public string CompletedBy { get; set; }
    }
}
