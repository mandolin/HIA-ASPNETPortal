using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>创建并提交企业协同事项的参数。</zh-CN>
    ///   <en>Parameters for creating and submitting an enterprise collaboration item.</en>
    /// </lang>
    /// </summary>
    public sealed class CollaborationItemCreateRequest
    {
        /// <summary><lang><zh-CN>事项类型键。</zh-CN><en>Item type key.</en></lang></summary>
        public string ItemTypeKey { get; set; }

        /// <summary><lang><zh-CN>事项标题。</zh-CN><en>Item title.</en></lang></summary>
        public string Title { get; set; }

        /// <summary><lang><zh-CN>低敏摘要。</zh-CN><en>Low-sensitivity summary.</en></lang></summary>
        public string Summary { get; set; }

        /// <summary><lang><zh-CN>纯文本事项说明。</zh-CN><en>Plain-text item description.</en></lang></summary>
        public string Description { get; set; }

        /// <summary><lang><zh-CN>发起人门户用户标识。</zh-CN><en>Initiator Portal user identifier.</en></lang></summary>
        public int InitiatorUserId { get; set; }

        /// <summary><lang><zh-CN>发起人员工标识。</zh-CN><en>Initiator employee identifier.</en></lang></summary>
        public int? InitiatorEmployeeId { get; set; }

        /// <summary><lang><zh-CN>负责人门户用户标识。</zh-CN><en>Owner Portal user identifier.</en></lang></summary>
        public int? OwnerUserId { get; set; }

        /// <summary><lang><zh-CN>负责人角色键。</zh-CN><en>Owner role key.</en></lang></summary>
        public string OwnerRoleKey { get; set; }

        /// <summary><lang><zh-CN>组织单元标识。</zh-CN><en>Organization-unit identifier.</en></lang></summary>
        public int? OrganizationUnitId { get; set; }

        /// <summary><lang><zh-CN>优先级键。</zh-CN><en>Priority key.</en></lang></summary>
        public string PriorityKey { get; set; }

        /// <summary><lang><zh-CN>期限 UTC 时间。</zh-CN><en>Due UTC time.</en></lang></summary>
        public DateTime? DueUtc { get; set; }

        /// <summary><lang><zh-CN>创建/提交 UTC 时间；为空时数据层使用当前 UTC。</zh-CN><en>Create/submit UTC time; the data layer uses current UTC when empty.</en></lang></summary>
        public DateTime? SubmittedUtc { get; set; }

        /// <summary><lang><zh-CN>提交人账号名。</zh-CN><en>Submitter account name.</en></lang></summary>
        public string SubmittedBy { get; set; }
    }
}
