using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：P6.3 员工生命周期状态常量。
    ///
    /// English: P6.3 employee lifecycle status constants.
    /// </summary>
    public static class PortalEmployeeStatuses
    {
        /// <summary>中文：在职员工。English: Active employee.</summary>
        public const string Active = "Active";

        /// <summary>中文：待确认员工。English: Pending employee.</summary>
        public const string Pending = "Pending";

        /// <summary>中文：暂停使用的员工。English: Suspended employee.</summary>
        public const string Suspended = "Suspended";

        /// <summary>中文：已离职员工。English: Employee who has left.</summary>
        public const string Left = "Left";

        /// <summary>
        /// 中文：判断状态是否属于第一版已知员工状态。
        ///
        /// English: Determines whether the value is a known first-version employee status.
        /// </summary>
        public static bool IsKnown(string value)
        {
            return string.Equals(value, Active, StringComparison.Ordinal) ||
                   string.Equals(value, Pending, StringComparison.Ordinal) ||
                   string.Equals(value, Suspended, StringComparison.Ordinal) ||
                   string.Equals(value, Left, StringComparison.Ordinal);
        }
    }
}
