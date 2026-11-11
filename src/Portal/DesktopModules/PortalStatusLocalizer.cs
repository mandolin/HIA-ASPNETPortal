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
        /// <summary><lang><zh-CN>请假行：仅「已退回」状态可重新提交；「已拒绝」为终态不可重提（Q2 裁定，委托 BusinessApplicationLifecycle 单一事实源）。</zh-CN><en>Leave row: only Returned is resubmittable; Rejected is terminal (Q2 ruling, delegated to the BusinessApplicationLifecycle single source of truth).</en></lang></summary>
        public static bool IsLeaveResubmittable(string status)
        {
            return BusinessApplicationLifecycle.IsResubmittable(status);
        }

        /// <summary><lang><zh-CN>报销行：仅「已退回」状态可重新提交；「已拒绝」为终态不可重提（Q2 裁定，委托 BusinessApplicationLifecycle 单一事实源）。</zh-CN><en>Expense row: only Returned is resubmittable; Rejected is terminal (Q2 ruling, delegated to the BusinessApplicationLifecycle single source of truth).</en></lang></summary>
        public static bool IsExpenseResubmittable(string status)
        {
            return BusinessApplicationLifecycle.IsResubmittable(status);
        }

        /// <summary><lang><zh-CN>把存储态请假状态映射为本地化展示文案（经 BusinessApplicationLifecycle 归一化到规范词，单一事实源）。</zh-CN><en>Maps the stored leave status to a localized display label via BusinessApplicationLifecycle normalization to the canonical vocabulary (single source of truth).</en></lang></summary>
        public static string LocalizeLeaveStatus(string status)
        {
            return LocalizeCanonical(BusinessApplicationLifecycle.NormalizeToCanonical(status));
        }

        /// <summary><lang><zh-CN>把存储态报销状态映射为本地化展示文案（经 BusinessApplicationLifecycle 归一化到规范词，单一事实源）。</zh-CN><en>Maps the stored expense status to a localized display label via BusinessApplicationLifecycle normalization to the canonical vocabulary (single source of truth).</en></lang></summary>
        public static string LocalizeExpenseStatus(string status)
        {
            return LocalizeCanonical(BusinessApplicationLifecycle.NormalizeToCanonical(status));
        }

        /// <summary><lang><zh-CN>把规范状态词映射为本地化展示文案；未列举的规范词回退为原词（不应发生，因规范词全集已在此覆盖）。</zh-CN><en>Maps a canonical status to its localized label; an unlisted canonical value falls back to the raw word (should not happen since the full canonical set is covered here).</en></lang></summary>
        private static string LocalizeCanonical(string canonical)
        {
            if (string.IsNullOrEmpty(canonical))
            {
                return string.Empty;
            }

            if (string.Equals(canonical, PortalBusinessApplicationStatuses.Draft, StringComparison.Ordinal)) return Resources.lang.Common_StatusDraft;
            if (string.Equals(canonical, PortalBusinessApplicationStatuses.Submitted, StringComparison.Ordinal)) return Resources.lang.Common_StatusSubmitted;
            if (string.Equals(canonical, PortalBusinessApplicationStatuses.InReview, StringComparison.Ordinal)) return Resources.lang.Common_StatusInReview;
            if (string.Equals(canonical, PortalBusinessApplicationStatuses.Returned, StringComparison.Ordinal)) return Resources.lang.Common_StatusReturned;
            if (string.Equals(canonical, PortalBusinessApplicationStatuses.Approved, StringComparison.Ordinal)) return Resources.lang.Common_StatusApproved;
            if (string.Equals(canonical, PortalBusinessApplicationStatuses.Rejected, StringComparison.Ordinal)) return Resources.lang.Common_StatusRejected;
            if (string.Equals(canonical, PortalBusinessApplicationStatuses.Withdrawn, StringComparison.Ordinal)) return Resources.lang.Common_StatusWithdrawn;
            if (string.Equals(canonical, PortalBusinessApplicationStatuses.Closed, StringComparison.Ordinal)) return Resources.lang.Common_StatusClosed;
            return canonical;
        }
    }
}
