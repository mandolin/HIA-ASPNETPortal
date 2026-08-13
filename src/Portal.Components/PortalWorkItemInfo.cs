using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>轻量待办的可变后台查询投影。</zh-CN>
    ///   <en>Mutable administration query projection for a lightweight work item.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本 DTO 是某次查询的显示快照，不自动反映并发更新，也不实施后台访问、业务对象或办理权限。字符串来自持久化数据且未按 HTML 上下文编码；页面必须先授权，再对输出编码，并且不得把指派、创建者或完成者显示字段当作身份凭据。</zh-CN>
    ///   <en>This DTO is a display snapshot from one query. It does not automatically reflect concurrent updates and enforces no administration, business-object, or handling authorization. Strings come from persisted data and are not HTML-context encoded. Pages must authorize first, encode output, and never treat assignment, creator, or completer display fields as identity credentials.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalWorkItemInfo
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>持久化待办标识；仅用于关联和后续查询，不证明查看或处理权限。</zh-CN>
        ///   <en>Persisted work-item identifier used only for correlation and subsequent queries; it proves no view or handling permission.</en>
        /// </lang>
        /// </summary>
        public long WorkItemId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>持久化业务对象类型；用于路由显示和关联，不是 CLR 类型或授权策略。</zh-CN>
        ///   <en>Persisted business-object kind used for display routing and correlation; it is neither a CLR type nor an authorization policy.</en>
        /// </lang>
        /// </summary>
        public string BusinessKind { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>字符串形式的业务对象标识；消费方必须在访问对应对象前另行校验存在性和权限。</zh-CN>
        ///   <en>Business-object identifier represented as text; consumers must separately validate existence and authorization before accessing the associated object.</en>
        /// </lang>
        /// </summary>
        public string BusinessId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>低敏纯文本标题快照；未进行 HTML 或其他输出上下文编码。</zh-CN>
        ///   <en>Low-sensitivity plain-text title snapshot that has not been HTML- or otherwise output-context encoded.</en>
        /// </lang>
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可为空的低敏纯文本摘要快照；不能假定已净化或适合直接输出。</zh-CN>
        ///   <en>Nullable low-sensitivity plain-text summary snapshot; it must not be assumed sanitized or safe for direct output.</en>
        /// </lang>
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>持久化待办状态；通常来自 <see cref="PortalWorkItemStatuses"/>，但投影不验证稳定值，也不能替代业务对象当前状态或授权。</zh-CN>
        ///   <en>Persisted work-item status, normally from <see cref="PortalWorkItemStatuses"/>. The projection does not validate stable values and cannot replace current domain state or authorization.</en>
        /// </lang>
        /// </summary>
        public string WorkItemStatus { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选的指定办理门户用户标识；它是持久化路由事实，不证明当前请求用户可办理。</zh-CN>
        ///   <en>Optional assigned Portal user identifier. It is a persisted routing fact and does not prove that the current request user may handle the item.</en>
        /// </lang>
        /// </summary>
        public int? AssignedUserId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>通过用户表左连接取得的可选显示名；账号缺失或已删除时可为空，不得用于身份比较。</zh-CN>
        ///   <en>Optional display name obtained through a left join to the user table. It may be null when the account is missing or deleted and must not be used for identity comparison.</en>
        /// </lang>
        /// </summary>
        public string AssignedUserName { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选的指定办理角色或权限键；这是路由提示，消费方仍须根据当前成员关系执行授权。</zh-CN>
        ///   <en>Optional assigned role or permission key. It is a routing hint; consumers must still authorize against current membership.</en>
        /// </lang>
        /// </summary>
        public string AssignedRoleKey { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>持久化创建 UTC 时间；DTO 不转换时区或验证 <see cref="DateTime.Kind"/>。</zh-CN>
        ///   <en>Persisted creation UTC time; the DTO neither converts time zones nor validates <see cref="DateTime.Kind"/>.</en>
        /// </lang>
        /// </summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建者账号名或系统标识的历史显示值；不是当前身份或授权证据。</zh-CN>
        ///   <en>Historical display value for the creator account name or system identifier; it is not evidence of current identity or authorization.</en>
        /// </lang>
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选到期 UTC 时间；为空表示未设置，非空也不会由 DTO 自动判定过期或改变状态。</zh-CN>
        ///   <en>Optional due UTC time. Null means no due time, while a value does not cause the DTO to determine expiration or change status.</en>
        /// </lang>
        /// </summary>
        public DateTime? DueUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选完成 UTC 时间；为空通常表示尚未记录完成，但消费方应同时解释持久化状态。</zh-CN>
        ///   <en>Optional completion UTC time. Null normally means completion has not been recorded, but consumers should interpret it together with the persisted status.</en>
        /// </lang>
        /// </summary>
        public DateTime? CompletedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>完成人账号名或系统标识的可选历史显示值；不得用于当前用户身份或权限判断。</zh-CN>
        ///   <en>Optional historical display value for the completer account name or system identifier; it must not be used for current-user identity or authorization decisions.</en>
        /// </lang>
        /// </summary>
        public string CompletedBy { get; set; }
    }
}
