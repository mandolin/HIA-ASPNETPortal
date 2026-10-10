using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary><lang>
    ///   <zh-CN>费用报销创建请求（P20 新增模块）。</zh-CN>
    ///   <en>Expense-reimbursement creation request (added in P20).</en>
    /// </lang></summary>
    public sealed class ExpenseReimbursementCreateRequest
    {
        /// <summary><lang><zh-CN>申请人门户用户标识。</zh-CN><en>Submitting Portal user identifier.</en></lang></summary>
        public int UserId { get; set; }

        /// <summary><lang><zh-CN>费用类别（差旅/餐饮/办公/其他）。</zh-CN><en>Expense category (travel/meal/office/other).</en></lang></summary>
        public string Category { get; set; }

        /// <summary><lang><zh-CN>报销金额。</zh-CN><en>Reimbursement amount.</en></lang></summary>
        public decimal Amount { get; set; }

        /// <summary><lang><zh-CN>费用事由。</zh-CN><en>Expense reason.</en></lang></summary>
        public string Reason { get; set; }

        /// <summary><lang><zh-CN>发票/附件文件名（v1 仅存名，二进制存储为后续项）。</zh-CN><en>Invoice/attachment file name (v1 stores name only; binary storage is a follow-up).</en></lang></summary>
        public string AttachmentName { get; set; }
    }

    /// <summary><lang><zh-CN>费用报销读取模型。</zh-CN><en>Expense-reimbursement read model.</en></lang></summary>
    public sealed class ExpenseReimbursementInfo
    {
        /// <summary><lang><zh-CN>报销主键。</zh-CN><en>Reimbursement identity key.</en></lang></summary>
        public long RequestId { get; set; }
        /// <summary><lang><zh-CN>费用类别（差旅/餐饮/办公/其他）。</zh-CN><en>Expense category (travel/meal/office/other).</en></lang></summary>
        public string Category { get; set; }
        /// <summary><lang><zh-CN>报销金额。</zh-CN><en>Reimbursement amount.</en></lang></summary>
        public decimal Amount { get; set; }
        /// <summary><lang><zh-CN>费用事由。</zh-CN><en>Expense reason.</en></lang></summary>
        public string Reason { get; set; }
        /// <summary><lang><zh-CN>发票/附件文件名。</zh-CN><en>Invoice/attachment file name.</en></lang></summary>
        public string AttachmentName { get; set; }
        /// <summary><lang><zh-CN>审批状态（如 Pending/Approved/Rejected）。</zh-CN><en>Approval status (e.g. Pending/Approved/Rejected).</en></lang></summary>
        public string Status { get; set; }
        /// <summary><lang><zh-CN>创建时间（UTC）。</zh-CN><en>Creation time (UTC).</en></lang></summary>
        public DateTime CreatedUtc { get; set; }
    }

    /// <summary><lang><zh-CN>费用报销创建结果。</zh-CN><en>Expense-reimbursement creation result.</en></lang></summary>
    public sealed class ExpenseReimbursementResult
    {
        /// <summary><lang><zh-CN>是否创建成功。</zh-CN><en>Whether the creation succeeded.</en></lang></summary>
        public bool Ok { get; set; }
        /// <summary><lang><zh-CN>低敏错误码（失败时使用）。</zh-CN><en>Low-sensitivity error code (used on failure).</en></lang></summary>
        public string ErrorCode { get; set; }
        /// <summary><lang><zh-CN>新建报销主键（成功时有效）。</zh-CN><en>New reimbursement identity key (valid on success).</en></lang></summary>
        public long RequestId { get; set; }
    }

    /// <summary><lang><zh-CN>费用报销数据访问契约。</zh-CN><en>Expense-reimbursement data-access contract.</en></lang></summary>
    public interface IExpenseReimbursementDb
    {
        /// <summary><lang><zh-CN>探测业务表是否可用。</zh-CN><en>Probe whether the business table is available.</en></lang></summary>
        bool IsSchemaAvailable();
        /// <summary><lang><zh-CN>创建一条费用报销。</zh-CN><en>Create an expense reimbursement.</en></lang></summary>
        /// <param name="request"><l><zh-CN>创建请求。</zh-CN><en>Creation request.</en></l></param>
        ExpenseReimbursementResult CreateRequest(ExpenseReimbursementCreateRequest request);
        /// <summary><lang><zh-CN>取申请人的最近报销列表。</zh-CN><en>Get the applicant's recent reimbursements.</en></lang></summary>
        /// <param name="userId"><l><zh-CN>用户标识。</zh-CN><en>User identifier.</en></l></param>
        /// <param name="limit"><l><zh-CN>上限。</zh-CN><en>Limit.</en></l></param>
        IReadOnlyList<ExpenseReimbursementInfo> GetRecentByUser(int userId, int limit);
    }
}
