using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>创建或确保轻量待办的可变跨层参数。</zh-CN>
    ///   <en>Mutable cross-layer parameters used to create or ensure a lightweight work item.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本 DTO 只携带调用方声明，不执行验证、授权或输出编码。当前数据层会复制、裁剪并补齐默认值，不修改调用方实例；调用方必须先授权业务动作，并只传入适合后台展示和事件记录的低敏纯文本。</zh-CN>
    ///   <en>This DTO only carries caller assertions; it performs no validation, authorization, or output encoding. The current data layer copies, trims, and defaults values without mutating the caller instance. Callers must authorize the domain action first and supply only low-sensitivity plain text suitable for administration display and event records.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalWorkItemCreateRequest
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>必填的稳定业务对象类型；当前数据层裁剪至 80 个字符。该值用于关联，不是 CLR 类型名或授权策略。</zh-CN>
        ///   <en>Required stable business-object kind, trimmed to 80 characters by the current data layer. It is a correlation discriminator, not a CLR type name or authorization policy.</en>
        /// </lang>
        /// </summary>
        public string BusinessKind { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>必填的稳定业务对象标识；当前数据层裁剪至 80 个字符。标识只用于关联，调用方仍须验证对象存在性和访问权。</zh-CN>
        ///   <en>Required stable business-object identifier, trimmed to 80 characters by the current data layer. The identifier is only for correlation; the caller must still verify object existence and access.</en>
        /// </lang>
        /// </summary>
        public string BusinessId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>必填的低敏纯文本标题；当前数据层裁剪至 200 个字符，展示方仍须按输出上下文编码。</zh-CN>
        ///   <en>Required low-sensitivity plain-text title, trimmed to 200 characters by the current data layer. Renderers must still encode it for the output context.</en>
        /// </lang>
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选低敏纯文本摘要；当前数据层裁剪至 500 个字符，并在新建待办时同时写入 Created 事件备注。不得放入口令、令牌或敏感业务正文。</zh-CN>
        ///   <en>Optional low-sensitivity plain-text summary, trimmed to 500 characters by the current data layer and also written as the Created-event comment for a new item. It must not contain passwords, tokens, or sensitive domain content.</en>
        /// </lang>
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>指定办理门户用户标识；非正数会被归一化为空，且用户标识与角色键至少应提供一项。该指派是路由提示，不证明当前用户身份或权限。</zh-CN>
        ///   <en>Assigned Portal user identifier. Non-positive values are normalized to null, and either this value or a role key must be supplied. The assignment is a routing hint and proves neither current-user identity nor authorization.</en>
        /// </lang>
        /// </summary>
        public int? AssignedUserId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>指定办理角色或权限键；当前数据层裁剪至 120 个字符。数据层不解析成员关系，消费方必须使用受控键并另行授权。</zh-CN>
        ///   <en>Assigned role or permission key, trimmed to 120 characters by the current data layer. The data layer does not resolve membership; consumers must use a controlled key and authorize separately.</en>
        /// </lang>
        /// </summary>
        public string AssignedRoleKey { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选创建 UTC 时间；为空时数据层使用 <see cref="DateTime.UtcNow"/>。数据层不转换时区或验证 <see cref="DateTime.Kind"/>，调用方提供值时必须确保为 UTC。</zh-CN>
        ///   <en>Optional creation UTC time; the data layer uses <see cref="DateTime.UtcNow"/> when absent. The data layer neither converts time zones nor validates <see cref="DateTime.Kind"/>, so caller-supplied values must already be UTC.</en>
        /// </lang>
        /// </summary>
        public DateTime? CreatedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建者账号名或系统标识；空白时使用 <c>system</c>，当前数据层裁剪至 100 个字符。它用于审计展示，不是身份或授权证明。</zh-CN>
        ///   <en>Creator account name or system identifier. Blank values become <c>system</c>, and the current data layer trims the value to 100 characters. It is for audit display and is not identity or authorization proof.</en>
        /// </lang>
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选到期 UTC 时间；数据层按原值保存，不验证其晚于创建时间，也不自动改变待办状态。</zh-CN>
        ///   <en>Optional due UTC time. The data layer stores it as supplied, does not verify that it follows creation time, and does not automatically change work-item status.</en>
        /// </lang>
        /// </summary>
        public DateTime? DueUtc { get; set; }
    }
}
