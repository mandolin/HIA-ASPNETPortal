using System;
using System.Collections.Generic;

namespace Portal.Components
{
    /// <summary><lang>
    ///   <zh-CN>请假申请创建请求（P20 新增模块）。</zh-CN>
    ///   <en>Leave-request creation request (added in P20).</en>
    /// </lang></summary>
    public sealed class LeaveRequestCreateRequest
    {
        /// <summary><lang><zh-CN>申请人门户用户标识。</zh-CN><en>Submitting portal user identifier.</en></lang></summary>
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
        public long RequestId { get; set; }
        public string LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Days { get; set; }
        public string Reason { get; set; }
        public string AttachmentName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    /// <summary><lang><zh-CN>请假申请创建结果。</zh-CN><en>Leave-request creation result.</en></lang></summary>
    public sealed class LeaveRequestResult
    {
        public bool Ok { get; set; }
        public string ErrorCode { get; set; }
        public long RequestId { get; set; }
    }

    /// <summary><lang><zh-CN>请假申请数据访问契约。</zh-CN><en>Leave-request data-access contract.</en></lang></summary>
    public interface ILeaveRequestDb
    {
        bool IsSchemaAvailable();
        LeaveRequestResult CreateRequest(LeaveRequestCreateRequest request);
        IReadOnlyList<LeaveRequestInfo> GetRecentByUser(int userId, int limit);
    }
}
