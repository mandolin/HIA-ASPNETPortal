using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户账号与员工绑定的只读视图。</zh-CN>
    ///   <en>Read-only view of a Portal-user to employee binding.</en>
    /// </lang>
    /// </summary>
    public interface IUserEmployeeBindingInfo
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定记录数值标识。</zh-CN>
        ///   <en>Numeric binding identifier.</en>
        /// </lang>
        /// </summary>
        int BindingId { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>门户账号标识。</zh-CN>
        ///   <en>Portal user identifier.</en>
        /// </lang>
        /// </summary>
        int UserId { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>旧门户用户名。</zh-CN>
        ///   <en>Legacy Portal user name.</en>
        /// </lang>
        /// </summary>
        string UserName { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工标识。</zh-CN>
        ///   <en>Employee identifier.</en>
        /// </lang>
        /// </summary>
        int EmployeeId { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工号。</zh-CN>
        ///   <en>Employee code.</en>
        /// </lang>
        /// </summary>
        string EmployeeCode { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工显示名。</zh-CN>
        ///   <en>Employee display name.</en>
        /// </lang>
        /// </summary>
        string EmployeeDisplayName { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定生命周期状态。</zh-CN>
        ///   <en>Binding lifecycle status.</en>
        /// </lang>
        /// </summary>
        string BindingStatus { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定创建时间 UTC。</zh-CN>
        ///   <en>Binding creation time in UTC.</en>
        /// </lang>
        /// </summary>
        DateTime BoundUtc { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定创建人标识。</zh-CN>
        ///   <en>Binding creator identifier.</en>
        /// </lang>
        /// </summary>
        string BoundBy { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定结束时间 UTC，可为空。</zh-CN>
        ///   <en>Binding end time in UTC, when known.</en>
        /// </lang>
        /// </summary>
        DateTime? EndedUtc { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定结束人标识。</zh-CN>
        ///   <en>Binding ending-operator identifier.</en>
        /// </lang>
        /// </summary>
        string EndedBy { get; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>非敏感绑定说明。</zh-CN>
        ///   <en>Non-sensitive binding reason.</en>
        /// </lang>
        /// </summary>
        string Reason { get; }
    }
}
