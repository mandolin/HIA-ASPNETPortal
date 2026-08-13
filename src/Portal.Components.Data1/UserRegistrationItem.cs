using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>用户注册审核的持久化记录；它保存审核事实和时间线，不直接授予登录或员工权限。</zh-CN>
    ///   <en>Persistent record for user-registration review; it stores review facts and timeline data without granting login or employee permissions.</en>
    /// </lang>
    /// </summary>
    [Table("PortalCfg_UserRegistrations")]
    public class UserRegistrationItem
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>注册审核记录的数据库主键。</zh-CN>
        ///   <en>Database primary key of the registration-review record.</en>
        /// </lang>
        /// </summary>
        [Key]
        public int RegistrationId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>关联用户的稳定标识；它只表达记录归属，不替代当前请求的身份复核。</zh-CN>
        ///   <en>Stable identifier of the associated user; it expresses record ownership and does not replace request-time identity checks.</en>
        /// </lang>
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>注册审核状态；调用方必须按既有状态契约解释它，不能将任意文本直接当作授权结果。</zh-CN>
        ///   <en>Registration-review status; callers must interpret it through the existing status contract rather than treating arbitrary text as an authorization result.</en>
        /// </lang>
        /// </summary>
        [StringLength(30)]
        public string Status { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前注册是否需要人工审核的持久化策略标记。</zh-CN>
        ///   <en>Persisted policy flag indicating whether the registration requires manual review.</en>
        /// </lang>
        /// </summary>
        public bool RequiresApproval { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交时关联的员工号；它是受限查询键，不是员工身份或角色授权证明。</zh-CN>
        ///   <en>Employee code associated at submission time; it is a bounded lookup key, not proof of employee identity or role authorization.</en>
        /// </lang>
        /// </summary>
        [StringLength(100)]
        public string EmployeeCode { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>注册邀请代码；它属于敏感关联值，不应进入日志、公开提示或无关响应。</zh-CN>
        ///   <en>Registration invitation code; it is a sensitive association value and must not be copied to logs, public messages, or unrelated responses.</en>
        /// </lang>
        /// </summary>
        [StringLength(64)]
        public string InviteCode { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>注册提交时间，按 UTC 持久化，供跨服务器排序和审核时间线比较。</zh-CN>
        ///   <en>Registration-submission timestamp persisted as UTC for cross-server ordering and review-timeline comparison.</en>
        /// </lang>
        /// </summary>
        public DateTime RegisteredUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>批准完成时间，按 UTC 持久化；为空表示当前记录尚未形成批准事实。</zh-CN>
        ///   <en>Approval-completion timestamp persisted as UTC; null means the record has no approval fact yet.</en>
        /// </lang>
        /// </summary>
        public DateTime? ApprovedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>形成批准事实的操作人标识；它用于审计关联，不替代权限检查。</zh-CN>
        ///   <en>Operator identifier that created the approval fact; it supports audit correlation and does not replace permission checks.</en>
        /// </lang>
        /// </summary>
        [StringLength(100)]
        public string ApprovedBy { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>拒绝完成时间，按 UTC 持久化；为空表示当前记录尚未形成拒绝事实。</zh-CN>
        ///   <en>Rejection-completion timestamp persisted as UTC; null means the record has no rejection fact yet.</en>
        /// </lang>
        /// </summary>
        public DateTime? RejectedUtc { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>形成拒绝事实的操作人标识；它用于审计关联，不代表调用方可跳过当前权限门禁。</zh-CN>
        ///   <en>Operator identifier that created the rejection fact; it supports audit correlation and does not let callers bypass current permission gates.</en>
        /// </lang>
        /// </summary>
        [StringLength(100)]
        public string RejectedBy { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>有界审核备注；展示或写入时仍须遵循既有长度、编码和敏感信息策略。</zh-CN>
        ///   <en>Bounded review note; display and write paths must still follow the existing length, encoding, and sensitive-data policy.</en>
        /// </lang>
        /// </summary>
        [StringLength(500)]
        public string ReviewNote { get; set; }
    }
}
