using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>联系人模块条目的 Entity Framework 投影。</zh-CN>
    ///     <en>Entity Framework projection for a contact module item.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该类型映射旧表 <c>Portal_Contacts</c>，承载展示型联系人资料。邮箱和联系方式都是业务展示信息，
    ///       不应被解释为登录凭据或已验证身份；展示层负责 HTML 编码和邮件链接安全处理。
    ///     </zh-CN>
    ///     <en>
    ///       This type maps the legacy <c>Portal_Contacts</c> table and carries display-oriented contact data.
    ///       Email and contact fields are business display information and must not be interpreted as login
    ///       credentials or verified identity; the presentation layer owns HTML encoding and safe mail-link handling.
    ///     </en>
    ///   </lang>
    /// </remarks>
    [Table("Portal_Contacts")]
    public class ContactItem : IContactItem
    {
        #region IContactItem Members

        /// <summary>
        ///   <l>
        ///     <zh-CN>联系人条目的数据库主键。</zh-CN>
        ///     <en>Database primary key for the contact item.</en>
        ///   </l>
        /// </summary>
        [Key]
        public int ItemId { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>拥有该联系人条目的模块实例标识。</zh-CN>
        ///     <en>Module instance identifier that owns this contact item.</en>
        ///   </l>
        /// </summary>
        public int ModuleId { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>联系人条目创建时间；旧数据可能为空。</zh-CN>
        ///     <en>Creation time for the contact item; legacy rows may be null.</en>
        ///   </l>
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>创建人显示名称；不是授权依据。</zh-CN>
        ///     <en>Display name of the creator; this is not an authorization source.</en>
        ///   </l>
        /// </summary>
        public string CreatedByUser { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>联系人姓名，展示层输出前必须编码。</zh-CN>
        ///     <en>Contact name; presentation code must encode it before output.</en>
        ///   </l>
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>联系人岗位或角色说明，展示层输出前必须编码。</zh-CN>
        ///     <en>Contact role or job description; presentation code must encode it before output.</en>
        ///   </l>
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>联系人邮箱；仅作为展示和邮件链接输入，不作为登录身份。</zh-CN>
        ///     <en>Contact email; only used for display and mail-link input, not as login identity.</en>
        ///   </l>
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>第一联系方式文本，通常是电话或办公信息。</zh-CN>
        ///     <en>First contact information text, typically phone or office details.</en>
        ///   </l>
        /// </summary>
        public string Contact1 { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>第二联系方式文本，通常是备用电话、传真或地址。</zh-CN>
        ///     <en>Second contact information text, typically alternate phone, fax, or address.</en>
        ///   </l>
        /// </summary>
        public string Contact2 { get; set; }

        #endregion
    }
}
