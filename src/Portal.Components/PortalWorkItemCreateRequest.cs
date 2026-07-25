using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>创建或确保轻量待办的参数。</zh-CN>
    ///   <en>Parameters used to create or ensure a lightweight work item.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalWorkItemCreateRequest
    {
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
        ///   <zh-CN>待办标题。</zh-CN>
        ///   <en>Work-item title.</en>
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
        ///   <zh-CN>指定办理门户用户标识；为空时由角色键承接。</zh-CN>
        ///   <en>Assigned Portal user id; role key is used when empty.</en>
        /// </lang>
        /// </summary>
        public int? AssignedUserId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>指定办理角色或权限键。</zh-CN>
        ///   <en>Assigned role or permission key.</en>
        /// </lang>
        /// </summary>
        public string AssignedRoleKey { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建 UTC 时间；为空时数据层使用当前 UTC。</zh-CN>
        ///   <en>Creation UTC time; data layer uses current UTC when empty.</en>
        /// </lang>
        /// </summary>
        public DateTime? CreatedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建者账号名或系统标识。</zh-CN>
        ///   <en>Creator account name or system identifier.</en>
        /// </lang>
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选到期 UTC 时间。</zh-CN>
        ///   <en>Optional due UTC time.</en>
        /// </lang>
        /// </summary>
        public DateTime? DueUtc { get; set; }
    }
}
