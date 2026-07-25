namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户账号与员工建立当前有效绑定的请求。</zh-CN>
    ///   <en>Request for creating a current active binding between a Portal user and an employee.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该请求只描述“为某个门户账号建立或更新当前有效员工绑定”的意图；账号权限、员工状态、工号唯一性和审计落库由后台服务与数据库层执行。员工号可用于业务识别和可选登录入口，但不是认证凭据；原因字段只允许写低敏说明，不得包含密码、Cookie、证件号或其他敏感凭据。</zh-CN>
    ///   <en>This request only describes the intent to create or update the current active employee binding for one portal account; account authorization, employee status, employee-code uniqueness, and audit persistence are enforced by admin services and the database layer. The employee code can be used for business identification and optional sign-in, but it is not a credential; the reason field is only for low-sensitivity notes and must not contain passwords, cookies, identity numbers, or other sensitive credentials.</en>
    /// </lang>
    /// </remarks>
    public sealed class UserEmployeeBindingSaveRequest
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>目标门户账号的内部标识；调用方应传入已存在且大于零的用户 ID。</zh-CN>
        ///   <en>Internal identifier of the target portal account; callers should pass an existing user ID greater than zero.</en>
        /// </lang>
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>要绑定的员工号；它是企业业务标识，也可能参与登录标识解析，但不能被当作密码或安全令牌。</zh-CN>
        ///   <en>Employee code to bind; it is an enterprise business identifier and may participate in sign-in identifier resolution, but it must not be treated as a password or security token.</en>
        /// </lang>
        /// </summary>
        public string EmployeeCode { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>管理员填写的低敏绑定原因或备注；该字段会进入审计语境，内容应能解释变更原因，但不能承载敏感信息。</zh-CN>
        ///   <en>Low-sensitivity binding reason or note entered by the administrator; this field enters the audit context and should explain the change without carrying sensitive information.</en>
        /// </lang>
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>执行绑定操作的管理员显示标识；它只用于审计与诊断记录，不作为授权判断依据。</zh-CN>
        ///   <en>Display identifier of the administrator performing the binding; it is used only for audit and diagnostics and is not an authorization source.</en>
        /// </lang>
        /// </summary>
        public string ActorName { get; set; }
    }
}
