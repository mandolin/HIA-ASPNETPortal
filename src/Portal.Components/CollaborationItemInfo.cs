using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>企业协同事项的列表和详情投影。</zh-CN>
    ///   <en>List and detail projection for an enterprise collaboration item.</en>
    /// </lang>
    /// </summary>
    public sealed class CollaborationItemInfo
    {
        /// <summary><lang><zh-CN>协同事项主键。</zh-CN><en>Collaboration-item primary key.</en></lang></summary>
        public long ItemId { get; set; }

        /// <summary><lang><zh-CN>人工可读事项编号。</zh-CN><en>Human-readable item code.</en></lang></summary>
        public string ItemCode { get; set; }

        /// <summary><lang><zh-CN>事项类型键。</zh-CN><en>Item type key.</en></lang></summary>
        public string ItemTypeKey { get; set; }

        /// <summary><lang><zh-CN>事项标题。</zh-CN><en>Item title.</en></lang></summary>
        public string Title { get; set; }

        /// <summary><lang><zh-CN>低敏摘要。</zh-CN><en>Low-sensitivity summary.</en></lang></summary>
        public string Summary { get; set; }

        /// <summary><lang><zh-CN>纯文本事项说明。</zh-CN><en>Plain-text item description.</en></lang></summary>
        public string Description { get; set; }

        /// <summary><lang><zh-CN>事项状态。</zh-CN><en>Item status.</en></lang></summary>
        public string ItemStatus { get; set; }

        /// <summary><lang><zh-CN>发起人门户用户标识。</zh-CN><en>Initiator Portal user identifier.</en></lang></summary>
        public int InitiatorUserId { get; set; }

        /// <summary><lang><zh-CN>发起人用户名快照。</zh-CN><en>Initiator user-name snapshot.</en></lang></summary>
        public string InitiatorUserName { get; set; }

        /// <summary><lang><zh-CN>可选发起人员工标识。</zh-CN><en>Optional initiator employee identifier.</en></lang></summary>
        public int? InitiatorEmployeeId { get; set; }

        /// <summary><lang><zh-CN>可选负责人门户用户标识。</zh-CN><en>Optional owner Portal user identifier.</en></lang></summary>
        public int? OwnerUserId { get; set; }

        /// <summary><lang><zh-CN>负责人用户名快照。</zh-CN><en>Owner user-name snapshot.</en></lang></summary>
        public string OwnerUserName { get; set; }

        /// <summary><lang><zh-CN>可选负责人角色键。</zh-CN><en>Optional owner role key.</en></lang></summary>
        public string OwnerRoleKey { get; set; }

        /// <summary><lang><zh-CN>可选组织单元标识。</zh-CN><en>Optional organization-unit identifier.</en></lang></summary>
        public int? OrganizationUnitId { get; set; }

        /// <summary><lang><zh-CN>优先级键。</zh-CN><en>Priority key.</en></lang></summary>
        public string PriorityKey { get; set; }

        /// <summary><lang><zh-CN>期限 UTC 时间。</zh-CN><en>Due UTC time.</en></lang></summary>
        public DateTime? DueUtc { get; set; }

        /// <summary><lang><zh-CN>在当前 UTC 时刻按 P23.5 规则计算的只读超期标记；不会改变状态或期限。</zh-CN><en>Read-only overdue flag computed at the current UTC time under P23.5 rules; it does not change state or due date.</en></lang></summary>
        public bool IsOverdue { get; set; }

        /// <summary><lang><zh-CN>提交 UTC 时间。</zh-CN><en>Submission UTC time.</en></lang></summary>
        public DateTime? SubmittedUtc { get; set; }

        /// <summary><lang><zh-CN>完成 UTC 时间。</zh-CN><en>Completion UTC time.</en></lang></summary>
        public DateTime? CompletedUtc { get; set; }

        /// <summary><lang><zh-CN>关闭 UTC 时间。</zh-CN><en>Closure UTC time.</en></lang></summary>
        public DateTime? ClosedUtc { get; set; }

        /// <summary><lang><zh-CN>最近动作 UTC 时间。</zh-CN><en>Latest action UTC time.</en></lang></summary>
        public DateTime? LastActionUtc { get; set; }

        /// <summary><lang><zh-CN>最近动作人门户用户标识。</zh-CN><en>Latest actor Portal user identifier.</en></lang></summary>
        public int? LastActionByUserId { get; set; }

        /// <summary><lang><zh-CN>最近办理意见。</zh-CN><en>Latest handling comment.</en></lang></summary>
        public string LastActionComment { get; set; }
    }
}
