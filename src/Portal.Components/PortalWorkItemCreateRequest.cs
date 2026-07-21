using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：创建或确保轻量待办的参数。
    ///
    /// English: Parameters used to create or ensure a lightweight work item.
    /// </summary>
    public sealed class PortalWorkItemCreateRequest
    {
        /// <summary>中文：业务对象类型。English: Business-object kind.</summary>
        public string BusinessKind { get; set; }

        /// <summary>中文：业务对象标识。English: Business-object identifier.</summary>
        public string BusinessId { get; set; }

        /// <summary>中文：待办标题。English: Work-item title.</summary>
        public string Title { get; set; }

        /// <summary>中文：低敏摘要。English: Low-sensitivity summary.</summary>
        public string Summary { get; set; }

        /// <summary>中文：指定办理门户用户标识；为空时由角色键承接。English: Assigned Portal user id; role key is used when empty.</summary>
        public int? AssignedUserId { get; set; }

        /// <summary>中文：指定办理角色或权限键。English: Assigned role or permission key.</summary>
        public string AssignedRoleKey { get; set; }

        /// <summary>中文：创建 UTC 时间；为空时数据层使用当前 UTC。English: Creation UTC time; data layer uses current UTC when empty.</summary>
        public DateTime? CreatedUtc { get; set; }

        /// <summary>中文：创建者账号名或系统标识。English: Creator account name or system identifier.</summary>
        public string CreatedBy { get; set; }

        /// <summary>中文：可选到期 UTC 时间。English: Optional due UTC time.</summary>
        public DateTime? DueUtc { get; set; }
    }
}
