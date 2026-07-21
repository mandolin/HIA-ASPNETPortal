using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：轻量待办的后台查询投影。
    ///
    /// English: Administration query projection for a lightweight work item.
    /// </summary>
    public sealed class PortalWorkItemInfo
    {
        /// <summary>中文：待办标识。English: Work-item identifier.</summary>
        public long WorkItemId { get; set; }

        /// <summary>中文：业务对象类型。English: Business-object kind.</summary>
        public string BusinessKind { get; set; }

        /// <summary>中文：业务对象标识。English: Business-object identifier.</summary>
        public string BusinessId { get; set; }

        /// <summary>中文：标题。English: Title.</summary>
        public string Title { get; set; }

        /// <summary>中文：低敏摘要。English: Low-sensitivity summary.</summary>
        public string Summary { get; set; }

        /// <summary>中文：待办状态。English: Work-item status.</summary>
        public string WorkItemStatus { get; set; }

        /// <summary>中文：指定办理用户标识。English: Assigned user identifier.</summary>
        public int? AssignedUserId { get; set; }

        /// <summary>中文：指定办理用户名。English: Assigned user name.</summary>
        public string AssignedUserName { get; set; }

        /// <summary>中文：指定办理角色或权限键。English: Assigned role or permission key.</summary>
        public string AssignedRoleKey { get; set; }

        /// <summary>中文：创建 UTC 时间。English: Creation UTC time.</summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>中文：创建者。English: Creator.</summary>
        public string CreatedBy { get; set; }

        /// <summary>中文：到期 UTC 时间。English: Due UTC time.</summary>
        public DateTime? DueUtc { get; set; }

        /// <summary>中文：完成 UTC 时间。English: Completion UTC time.</summary>
        public DateTime? CompletedUtc { get; set; }

        /// <summary>中文：完成人。English: Completer.</summary>
        public string CompletedBy { get; set; }
    }
}
