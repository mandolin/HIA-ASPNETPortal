namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>结束门户账号与员工当前有效绑定的请求。</zh-CN>
    ///   <en>Request for ending a current active binding between a Portal user and an employee.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该对象只描述解绑动作，不承载权限。调用方必须先确认操作者可维护账号员工绑定，并确保解绑不会误删历史关系记录。</zh-CN>
    ///   <en>This object only describes an unbinding action and does not carry permission. Callers must first confirm that the operator may maintain user-employee bindings and ensure the action does not delete historical relationship records by mistake.</en>
    /// </lang>
    /// </remarks>
    public sealed class UserEmployeeBindingEndRequest
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>要结束的绑定记录标识。</zh-CN>
        ///   <en>The binding identifier to end.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>数据层会按此标识定位当前有效绑定；不存在或已结束时应返回低敏失败结果。</zh-CN>
        ///   <en>The data layer uses this identifier to locate the current active binding; missing or already-ended records should produce a low-sensitivity failure result.</en>
        /// </lang>
        /// </remarks>
        public int BindingId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>非敏感解绑说明。</zh-CN>
        ///   <en>Non-sensitive unbinding reason.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>原因会进入审计或后台提示摘要，调用方应避免填入密码、证件号、私人联系方式等敏感信息。</zh-CN>
        ///   <en>The reason may enter audit records or administration summaries, so callers should avoid passwords, identity numbers, private contact information, and other sensitive content.</en>
        /// </lang>
        /// </remarks>
        public string Reason { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>执行解绑的操作者标识。</zh-CN>
        ///   <en>The operator identifier ending the binding.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该字段用于审计和历史记录，不作为授权依据；权限必须来自当前请求身份和后台权限判断。</zh-CN>
        ///   <en>This field is for audit and history only, not authorization; authorization must come from the current request identity and administration permission checks.</en>
        /// </lang>
        /// </remarks>
        public string ActorName { get; set; }
    }
}
