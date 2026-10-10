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

    /// <summary><lang><zh-CN>请假申请读取模型。</zh-CN><en>Leave-request read model.</en></lang></summary>
    public sealed class LeaveRequestInfo
    {
        /// <summary><lang><zh-CN>申请主键。</zh-CN><en>Request identity key.</en></lang></summary>
        public long RequestId { get; set; }
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
    }
}
