using System;
using System.Collections.Generic;

namespace Portal.Components
{
    /// <summary><lang>
    ///   <zh-CN>费用报销创建请求（P20 新增模块）。</zh-CN>
    ///   <en>Expense-reimbursement creation request (added in P20).</en>
    /// </lang></summary>
    public sealed class ExpenseReimbursementCreateRequest
    {
        /// <summary><lang><zh-CN>申请人门户用户标识。</zh-CN><en>Submitting portal user identifier.</en></lang></summary>
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
        public long RequestId { get; set; }
        public string Category { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; }
        public string AttachmentName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    /// <summary><lang><zh-CN>费用报销创建结果。</zh-CN><en>Expense-reimbursement creation result.</en></lang></summary>
    public sealed class ExpenseReimbursementResult
    {
        public bool Ok { get; set; }
        public string ErrorCode { get; set; }
        public long RequestId { get; set; }
    }

    /// <summary><lang><zh-CN>费用报销数据访问契约。</zh-CN><en>Expense-reimbursement data-access contract.</en></lang></summary>
    public interface IExpenseReimbursementDb
    {
        bool IsSchemaAvailable();
        ExpenseReimbursementResult CreateRequest(ExpenseReimbursementCreateRequest request);
        IReadOnlyList<ExpenseReimbursementInfo> GetRecentByUser(int userId, int limit);
    }
}
