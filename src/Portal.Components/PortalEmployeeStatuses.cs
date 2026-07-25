using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>P6.3 员工生命周期状态常量。</zh-CN>
    ///   <en>P6.3 employee lifecycle status constants.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>这些字符串会进入员工主数据、后台筛选、审计和文档证据，属于持久化契约值；新增状态时应补数据库脚本、页面枚举和兼容说明，不要直接改名。</zh-CN>
    ///   <en>These strings appear in employee master data, administration filters, audits, and documentation evidence, so they are persisted contract values. When adding statuses, update database scripts, page lists, and compatibility notes instead of renaming existing values.</en>
    /// </lang>
    /// </remarks>
    public static class PortalEmployeeStatuses
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>表示在职且可作为当前有效绑定目标的员工。</zh-CN>
        ///   <en>Represents an active employee who may be used as a current binding target.</en>
        /// </lang>
        /// </summary>
        public const string Active = "Active";

        /// <summary>
        /// <lang>
        ///   <zh-CN>表示已导入或已创建但仍需管理员确认的员工。</zh-CN>
        ///   <en>Represents an imported or created employee record that still requires administrator confirmation.</en>
        /// </lang>
        /// </summary>
        public const string Pending = "Pending";

        /// <summary>
        /// <lang>
        ///   <zh-CN>表示临时暂停使用的员工记录。</zh-CN>
        ///   <en>Represents an employee record that is temporarily suspended.</en>
        /// </lang>
        /// </summary>
        public const string Suspended = "Suspended";

        /// <summary>
        /// <lang>
        ///   <zh-CN>表示已离职或不再作为有效业务人员使用的员工。</zh-CN>
        ///   <en>Represents an employee who has left or is no longer used as an active business person.</en>
        /// </lang>
        /// </summary>
        public const string Left = "Left";

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断状态是否属于第一版已知员工状态。</zh-CN>
        ///   <en>Determines whether the value is a known first-version employee status.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>要验证的状态字符串。</zh-CN>
        ///   <en>The status string to validate.</en>
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
                   string.Equals(value, Suspended, StringComparison.Ordinal) ||
                   string.Equals(value, Left, StringComparison.Ordinal);
        }
    }
}
