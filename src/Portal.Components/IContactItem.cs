using System;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>联系人模块条目的跨层只读/可写契约。</zh-CN>
    ///     <en>Cross-layer readable/writable contract for a contact module item.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该接口用于旧联系人数据访问和展示控件之间传递展示型资料。邮箱和联系方式不代表登录身份；
    ///       展示层必须负责 HTML 编码和邮件链接安全处理。
    ///     </zh-CN>
    ///     <en>
    ///       This interface passes display-oriented contact data between legacy contact data access and display
    ///       controls. Email and contact fields do not represent login identity; presentation code must perform
    ///       HTML encoding and safe mail-link handling.
    ///     </en>
    ///   </lang>
    /// </remarks>
    public interface IContactItem
    {
        /// <summary>
        ///   <l zh-CN="联系人条目的数据库主键。" en="Database primary key for the contact item." />
        /// </summary>
        int ItemId { get; set; }

        /// <summary>
        ///   <l zh-CN="拥有该联系人条目的模块实例标识。" en="Module instance identifier that owns this contact item." />
        /// </summary>
        int ModuleId { get; set; }

        /// <summary>
        ///   <l zh-CN="联系人条目创建时间；旧数据可能为空。" en="Creation time for the contact item; legacy rows may be null." />
        /// </summary>
        DateTime? CreatedDate { get; set; }

        /// <summary>
        ///   <l zh-CN="创建人显示名称；不是授权依据。" en="Display name of the creator; this is not an authorization source." />
        /// </summary>
        string CreatedByUser { get; set; }

        /// <summary>
        ///   <l zh-CN="联系人姓名，展示层输出前必须编码。" en="Contact name; presentation code must encode it before output." />
        /// </summary>
        string Name { get; set; }

        /// <summary>
        ///   <l zh-CN="联系人岗位或角色说明，展示层输出前必须编码。" en="Contact role or job description; presentation code must encode it before output." />
        /// </summary>
        string Role { get; set; }

        /// <summary>
        ///   <l zh-CN="联系人邮箱；仅作为展示和邮件链接输入，不作为登录身份。" en="Contact email; only used for display and mail-link input, not as login identity." />
        /// </summary>
        string Email { get; set; }

        /// <summary>
        ///   <l zh-CN="第一联系方式文本，通常是电话或办公信息。" en="First contact information text, typically phone or office details." />
        /// </summary>
        string Contact1 { get; set; }

        /// <summary>
        ///   <l zh-CN="第二联系方式文本，通常是备用电话、传真或地址。" en="Second contact information text, typically alternate phone, fax, or address." />
        /// </summary>
        string Contact2 { get; set; }
    }
}
