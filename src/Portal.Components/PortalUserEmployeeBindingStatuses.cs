using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：门户账号与员工绑定生命周期状态常量。
    ///
    /// English: Portal-user to employee binding lifecycle status constants.
    /// </summary>
    public static class PortalUserEmployeeBindingStatuses
    {
        /// <summary>中文：当前有效绑定。English: Current active binding.</summary>
        public const string Active = "Active";

        /// <summary>中文：待确认绑定。English: Pending binding.</summary>
        public const string Pending = "Pending";

        /// <summary>中文：已禁用绑定。English: Disabled binding.</summary>
        public const string Disabled = "Disabled";

        /// <summary>中文：已结束绑定。English: Ended binding.</summary>
        public const string Ended = "Ended";

        /// <summary>
        /// 中文：判断状态是否属于第一版已知绑定状态。
        ///
        /// English: Determines whether the value is a known first-version binding status.
        /// </summary>
        public static bool IsKnown(string value)
        {
            return string.Equals(value, Active, StringComparison.Ordinal) ||
                   string.Equals(value, Pending, StringComparison.Ordinal) ||
                   string.Equals(value, Disabled, StringComparison.Ordinal) ||
                   string.Equals(value, Ended, StringComparison.Ordinal);
        }
    }
}
