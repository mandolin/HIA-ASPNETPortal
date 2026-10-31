using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary><lang>
    ///   <zh-CN>请假申请创建请求（P20 新增模块）。</zh-CN>
    ///   <en>Leave-request creation request (added in P20).</en>
    /// </lang></summary>
    public sealed class LeaveRequestCreateRequest
    {
        /// <summary><lang><zh-CN>申请人门户用户标识。</zh-CN><en>Submitting Portal user identifier.</en></lang></summary>
        public int UserId { get; set; }

        /// <summary><lang><zh-CN>请假类型（年假/事假/病假/调休）。</zh-CN><en>Leave type (annual/personal/sick/compensatory).</en></lang></summary>
        public string LeaveType { get; set; }

        /// <summary><lang><zh-CN>开始日期。</zh-CN><en>Start date.</en></lang></summary>
        public DateTime StartDate { get; set; }

        /// <summary><lang><zh-CN>结束日期。</zh-CN><en>End date.</en></lang></summary>
        public DateTime EndDate { get; set; }

        /// <summary><lang><zh-CN>请假天数。</zh-CN><en>Leave days.</en></lang></summary>
        public decimal Days { get; set; }

        /// <summary><lang><zh-CN>请假事由。</zh-CN><en>Leave reason.</en></lang></summary>
        public string Reason { get; set; }

        /// <summary><lang><zh-CN>附件文件名（v1 仅存名，二进制存储为后续项）。</zh-CN><en>Attachment file name (v1 stores name only; binary storage is a follow-up).</en></lang></summary>
        public string AttachmentName { get; set; }
    }

    /// <summary><lang><zh-CN>请假申请重新提交请求（申请人从退回或拒绝状态重新提交）。</zh-CN><en>Leave-request resubmission request (applicant resubmits from a returned or rejected state).</en></lang></summary>
    public sealed class LeaveRequestResubmitRequest
    {
        /// <summary><lang><zh-CN>申请主键。</zh-CN><en>Request identity key.</en></lang></summary>
        public long RequestId { get; set; }
        /// <summary><lang><zh-CN>申请人用户标识（用于审计与归属核对）。</zh-CN><en>Applicant user identity (for audit and ownership checks).</en></lang></summary>
        public int ApplicantUserId { get; set; }
        /// <summary><lang><zh-CN>操作者名称（审计用）。</zh-CN><en>Actor name for audit.</en></lang></summary>
        public string ActorName { get; set; }
    }

    /// <summary><lang><zh-CN>请假申请重新提交结果。</zh-CN><en>Leave-request resubmission result.</en></lang></summary>
    public sealed class LeaveRequestResubmitResult
    {
        /// <summary><lang><zh-CN>是否成功（受保护更新命中一行）。</zh-CN><en>Whether the guarded update hit a row.</en></lang></summary>
        public bool Succeeded { get; set; }
        /// <summary><lang><zh-CN>低敏结果说明（InvalidRequest/NotFound/DbError/OK）。</zh-CN><en>Low-sensitivity result note (InvalidRequest/NotFound/DbError/OK).</en></lang></summary>
        public string Message { get; set; }
        /// <summary><lang><zh-CN>申请主键。</zh-CN><en>Request identity key.</en></lang></summary>
        public long RequestId { get; set; }
    }

    /// <summary><lang><zh-CN>请假申请读取模型。</zh-CN><en>Leave-request read model.</en></lang></summary>
    public sealed class LeaveRequestInfo
    {
        /// <summary><lang><zh-CN>申请主键。</zh-CN><en>Request identity key.</en></lang></summary>
        public long RequestId { get; set; }
        /// <summary><lang><zh-CN>申请人门户用户标识（后台审核展示用）。</zh-CN><en>Submitting Portal user identifier (for admin review display).</en></lang></summary>
        public int UserId { get; set; }
        /// <summary><lang><zh-CN>请假类型。</zh-CN><en>Leave type.</en></lang></summary>
        public string LeaveType { get; set; }
        /// <summary><lang><zh-CN>开始日期（UTC）。</zh-CN><en>Start date (UTC).</en></lang></summary>
        public DateTime StartDate { get; set; }
        /// <summary><lang><zh-CN>结束日期（UTC）。</zh-CN><en>End date (UTC).</en></lang></summary>
        public DateTime EndDate { get; set; }
        /// <summary><lang><zh-CN>请假天数。</zh-CN><en>Leave days.</en></lang></summary>
        public decimal Days { get; set; }
        /// <summary><lang><zh-CN>请假事由。</zh-CN><en>Leave reason.</en></lang></summary>
        public string Reason { get; set; }
        /// <summary><lang><zh-CN>附件文件名。</zh-CN><en>Attachment file name.</en></lang></summary>
        public string AttachmentName { get; set; }
        /// <summary><lang><zh-CN>审批状态（如 Pending/Approved/Rejected）。</zh-CN><en>Approval status (e.g. Pending/Approved/Rejected).</en></lang></summary>
        public string Status { get; set; }
        /// <summary><lang><zh-CN>创建时间（UTC）。</zh-CN><en>Creation time (UTC).</en></lang></summary>
        public DateTime CreatedUtc { get; set; }
        /// <summary><lang><zh-CN>审核时间（UTC，后台审核写入后为非空）。</zh-CN><en>Review time (UTC; non-null after an admin review).</en></lang></summary>
        public DateTime? ReviewedUtc { get; set; }
        /// <summary><lang><zh-CN>审核人门户用户标识。</zh-CN><en>Reviewer Portal user identifier.</en></lang></summary>
        public int? ReviewedByUserId { get; set; }
        /// <summary><lang><zh-CN>审核意见。</zh-CN><en>Review comment.</en></lang></summary>
        public string ReviewComment { get; set; }
    }

    /// <summary><lang><zh-CN>请假申请创建结果。</zh-CN><en>Leave-request creation result.</en></lang></summary>
    public sealed class LeaveRequestResult
    {
        /// <summary><lang><zh-CN>是否创建成功。</zh-CN><en>Whether the creation succeeded.</en></lang></summary>
        public bool Ok { get; set; }
        /// <summary><lang><zh-CN>低敏错误码（失败时使用）。</zh-CN><en>Low-sensitivity error code (used on failure).</en></lang></summary>
        public string ErrorCode { get; set; }
        /// <summary><lang><zh-CN>新建申请主键（成功时有效）。</zh-CN><en>New request identity key (valid on success).</en></lang></summary>
        public long RequestId { get; set; }
    }

    /// <summary><lang><zh-CN>请假申请审批状态键（与后台审核页筛选及工作项投影保持一致）。</zh-CN><en>Leave-request approval status keys (consistent with the admin review-page filter and work-item projection).</en></lang></summary>
    public static class LeaveRequestStatuses
    {
        /// <summary><lang><zh-CN>待审批。</zh-CN><en>Pending review.</en></lang></summary>
        public const string Pending = "Pending";
        /// <summary><lang><zh-CN>已批准。</zh-CN><en>Approved.</en></lang></summary>
        public const string Approved = "Approved";
        /// <summary><lang><zh-CN>已拒绝。</zh-CN><en>Rejected.</en></lang></summary>
        public const string Rejected = "Rejected";
        /// <summary><lang><zh-CN>已退回（等待申请人修订后重新提交）。</zh-CN><en>Returned (awaiting applicant revision and resubmission).</en></lang></summary>
        public const string Returned = "Returned";
        /// <summary><lang><zh-CN>已关闭。</zh-CN><en>Closed.</en></lang></summary>
        public const string Closed = "Closed";
    }

    /// <summary><lang><zh-CN>请假申请审核请求（后台审核页提交）。</zh-CN><en>Leave-request review request (submitted from the admin review page).</en></lang></summary>
    public sealed class LeaveRequestReviewRequest
    {
        /// <summary><lang><zh-CN>申请主键。</zh-CN><en>Request identity key.</en></lang></summary>
        public long RequestId { get; set; }
        /// <summary><lang><zh-CN>审核动作键（Approve/Reject/Return）。</zh-CN><en>Review action key (Approve/Reject/Return).</en></lang></summary>
        public string ActionKey { get; set; }
        /// <summary><lang><zh-CN>审核意见（已归一化）。</zh-CN><en>Review comment (normalized).</en></lang></summary>
        public string ReviewComment { get; set; }
        /// <summary><lang><zh-CN>审核人用户标识。</zh-CN><en>Reviewer user identifier.</en></lang></summary>
        public int ReviewedByUserId { get; set; }
        /// <summary><lang><zh-CN>审核人名称。</zh-CN><en>Reviewer name.</en></lang></summary>
        public string ReviewedBy { get; set; }
        /// <summary><lang><zh-CN>审核时间（UTC）。</zh-CN><en>Review time (UTC).</en></lang></summary>
        public DateTime ReviewedUtc { get; set; }
    }

    /// <summary><lang><zh-CN>请假申请审核结果。</zh-CN><en>Leave-request review result.</en></lang></summary>
    public sealed class LeaveRequestReviewResult
    {
        /// <summary><lang><zh-CN>是否成功。</zh-CN><en>Whether the review succeeded.</en></lang></summary>
        public bool Succeeded { get; set; }
        /// <summary><lang><zh-CN>低敏结果消息（失败时使用）。</zh-CN><en>Low-sensitivity result message (used on failure).</en></lang></summary>
        public string Message { get; set; }
        /// <summary><lang><zh-CN>申请主键。</zh-CN><en>Request identity key.</en></lang></summary>
        public long RequestId { get; set; }
    }

    /// <summary><lang><zh-CN>请假申请数据访问契约。</zh-CN><en>Leave-request data-access contract.</en></lang></summary>
    public interface ILeaveRequestDb
    {
        /// <summary><lang><zh-CN>探测业务表是否可用。</zh-CN><en>Probe whether the business table is available.</en></lang></summary>
        bool IsSchemaAvailable();
        /// <summary><lang><zh-CN>创建一条请假申请。</zh-CN><en>Create a leave request.</en></lang></summary>
        /// <param name="request"><l><zh-CN>创建请求。</zh-CN><en>Creation request.</en></l></param>
        LeaveRequestResult CreateRequest(LeaveRequestCreateRequest request);
        /// <summary><lang><zh-CN>取申请人的最近申请列表。</zh-CN><en>Get the applicant's recent requests.</en></lang></summary>
        /// <param name="userId"><l><zh-CN>用户标识。</zh-CN><en>User identifier.</en></l></param>
        /// <param name="limit"><l><zh-CN>上限。</zh-CN><en>Limit.</en></l></param>
        IReadOnlyList<LeaveRequestInfo> GetRecentByUser(int userId, int limit);
        /// <summary><lang><zh-CN>按状态筛选读取后台审核用请假申请列表（空筛选值表示全部）。</zh-CN><en>Reads the administration review list filtered by status (an empty filter means all).</en></lang></summary>
        /// <param name="statusFilter"><l><zh-CN>状态筛选值。</zh-CN><en>Status filter value.</en></l></param>
        /// <param name="limit"><l><zh-CN>上限。</zh-CN><en>Limit.</en></l></param>
        IList<LeaveRequestInfo> GetAdminRequests(string statusFilter, int limit);
        /// <summary><lang><zh-CN>按动作键审核请假申请（更新状态与审核人信息）。</zh-CN><en>Reviews a leave request by action key (updates status and reviewer info).</en></lang></summary>
        /// <param name="request"><l><zh-CN>审核请求。</zh-CN><en>Review request.</en></l></param>
        LeaveRequestReviewResult ReviewRequest(LeaveRequestReviewRequest request);
        /// <summary><lang><zh-CN>把退回或拒绝状态的请假申请重置为待审批，并清空审核身份列（保留退回意见作为历史）；工作项重开由调用方负责。</zh-CN><en>Resets a returned or rejected leave request to pending and clears the reviewer-identity columns (keeping the return comment as history); the caller reopens the work item.</en></lang></summary>
        LeaveRequestResubmitResult ResubmitRequest(LeaveRequestResubmitRequest request);
    }
}
