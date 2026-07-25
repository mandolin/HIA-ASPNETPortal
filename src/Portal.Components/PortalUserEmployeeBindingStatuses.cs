using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户账号与员工绑定生命周期状态常量。</zh-CN>
    ///   <en>Portal-user to employee binding lifecycle status constants.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>这些字符串会进入账号员工绑定表、登录身份解析、后台筛选和审计记录，属于持久化契约值；新增状态时应同步登录解析、绑定管理和迁移脚本。</zh-CN>
    ///   <en>These strings appear in user-employee binding tables, login identity resolution, administration filters, and audit records, so they are persisted contract values. When adding statuses, update login resolution, binding administration, and migration scripts together.</en>
    /// </lang>
    /// </remarks>
    public static class PortalUserEmployeeBindingStatuses
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>表示门户账号当前有效绑定到员工主数据。</zh-CN>
        ///   <en>Represents a portal user currently and actively bound to employee master data.</en>
        /// </lang>
        /// </summary>
        public const string Active = "Active";

        /// <summary>
        /// <lang>
        ///   <zh-CN>表示绑定已创建但仍需管理员或业务流程确认。</zh-CN>
        ///   <en>Represents a binding that has been created but still requires administrator or business-flow confirmation.</en>
        /// </lang>
        /// </summary>
        public const string Pending = "Pending";

        /// <summary>
        /// <lang>
        ///   <zh-CN>表示绑定被管理员禁用，不能参与登录身份解析。</zh-CN>
        ///   <en>Represents a binding disabled by an administrator and unavailable for login identity resolution.</en>
        /// </lang>
        /// </summary>
        public const string Disabled = "Disabled";

        /// <summary>
        /// <lang>
        ///   <zh-CN>表示历史绑定已经结束，只可作为审计或历史查询信息。</zh-CN>
        ///   <en>Represents a historical binding that has ended and should be used only for audit or historical queries.</en>
        /// </lang>
        /// </summary>
        public const string Ended = "Ended";

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断状态是否属于第一版已知绑定状态。</zh-CN>
        ///   <en>Determines whether the value is a known first-version binding status.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>要验证的绑定状态字符串。</zh-CN>
        ///   <en>The binding-status string to validate.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>状态精确匹配当前稳定常量之一时返回 <c>true</c>。</zh-CN>
        ///   <en>Returns <c>true</c> when the status exactly matches one of the current stable constants.</en>
        /// </l>
        /// </returns>
        public static bool IsKnown(string value)
        {
            return string.Equals(value, Active, StringComparison.Ordinal) ||
                   string.Equals(value, Pending, StringComparison.Ordinal) ||
                   string.Equals(value, Disabled, StringComparison.Ordinal) ||
                   string.Equals(value, Ended, StringComparison.Ordinal);
        }
    }
}
