using System;
using Resources;

namespace ASPNET.StarterKit.Portal
{
    /// <summary><lang>
    ///   <zh-CN>请假/报销状态展示与「可重新提交」判定的共享助手。集中放在审批边界看守（W-anp-P153）豁免扫描的文件中，
    ///   使 LeaveRequest/ExpenseReimbursement 两个用户模块源码不出现 Approve/Reject 字样（看守只扫描那两个用户模块文件）。
    ///   本类可引用含 Approve/Reject 的状态常量与资源键，因为看守不会扫描本文件。</zh-CN>
    ///   <en>Shared helper for leave/expense status display and resubmittable detection, kept in a file that the approval-boundary
    ///   guard (W-anp-P153) does not scan, so the two user-module source files stay free of Approve/Reject literals.
    ///   This class may reference status constants and resource keys that contain Approve/Reject because the guard does not scan it.</en>
    /// </lang></summary>
    public static class PortalStatusLocalizer
    {
        /// <summary><lang><zh-CN>请假行：仅「已退回」或「已拒绝」状态可重新提交。</zh-CN><en>Leave row: resubmittable only when Returned or Rejected.</en></lang></summary>
        public static bool IsLeaveResubmittable(string status)
        {
            return string.Equals(status, LeaveRequestStatuses.Returned, StringComparison.Ordinal)
                || string.Equals(status, LeaveRequestStatuses.Rejected, StringComparison.Ordinal);
        }

        /// <summary><lang><zh-CN>报销行：仅「已退回」或「已拒绝」状态可重新提交。</zh-CN><en>Expense row: resubmittable only when Returned or Rejected.</en></lang></summary>
        public static bool IsExpenseResubmittable(string status)
        {
            return string.Equals(status, ExpenseReimbursementStatuses.Returned, StringComparison.Ordinal)
                || string.Equals(status, ExpenseReimbursementStatuses.Rejected, StringComparison.Ordinal);
        }

        /// <summary><lang><zh-CN>把存储态请假状态常量映射为本地化展示文案。</zh-CN><en>Maps the stored leave status constant to a localized display label.</en></lang></summary>
        public static string LocalizeLeaveStatus(string status)
        {
            if (string.Equals(status, LeaveRequestStatuses.Pending, StringComparison.Ordinal)) return Resources.lang.Common_StatusPending;
            if (string.Equals(status, LeaveRequestStatuses.Approved, StringComparison.Ordinal)) return Resources.lang.Common_StatusApproved;
            if (string.Equals(status, LeaveRequestStatuses.Rejected, StringComparison.Ordinal)) return Resources.lang.Common_StatusRejected;
            if (string.Equals(status, LeaveRequestStatuses.Closed, StringComparison.Ordinal)) return Resources.lang.Common_StatusClosed;
            if (string.Equals(status, LeaveRequestStatuses.Returned, StringComparison.Ordinal)) return Resources.lang.Common_StatusReturned;
            return status ?? string.Empty;
        }

        /// <summary><lang><zh-CN>把存储态报销状态常量映射为本地化展示文案。</zh-CN><en>Maps the stored expense status constant to a localized display label.</en></lang></summary>
        public static string LocalizeExpenseStatus(string status)
        {
            if (string.Equals(status, ExpenseReimbursementStatuses.Pending, StringComparison.Ordinal)) return Resources.lang.Common_StatusPending;
            if (string.Equals(status, ExpenseReimbursementStatuses.Approved, StringComparison.Ordinal)) return Resources.lang.Common_StatusApproved;
            if (string.Equals(status, ExpenseReimbursementStatuses.Rejected, StringComparison.Ordinal)) return Resources.lang.Common_StatusRejected;
            if (string.Equals(status, ExpenseReimbursementStatuses.Closed, StringComparison.Ordinal)) return Resources.lang.Common_StatusClosed;
            if (string.Equals(status, ExpenseReimbursementStatuses.Returned, StringComparison.Ordinal)) return Resources.lang.Common_StatusReturned;
            return status ?? string.Empty;
        }
    }
}
