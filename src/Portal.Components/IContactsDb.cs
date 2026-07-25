using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>联系人内容模块的数据访问契约。</zh-CN>
    ///   <en>Data-access contract for contact content modules.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>联系人模块保存的是展示资料，不是门户登录身份。调用方应在写入前完成模块编辑权限、条目归属和字段长度校验；展示层负责 HTML 编码和邮件链接生成。</zh-CN>
    ///   <en>The contacts module stores display information, not portal login identity. Callers should complete module-edit authorization, item ownership checks, and field-length validation before writes; the presentation layer owns HTML encoding and mail-link creation.</en>
    /// </lang>
    /// </remarks>
    public interface IContactsDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定模块下的联系人列表。</zh-CN>
        ///   <en>Reads contacts under the specified module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>联系人模块实例标识。</zh-CN>
        ///   <en>The contact module instance identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>联系人集合；字段可能为空，调用方和展示层应按旧模块兼容规则处理。</zh-CN>
        ///   <en>The contact collection; fields may be empty, and callers plus the presentation layer should handle them under legacy module compatibility rules.</en>
        /// </l>
        /// </returns>
        IEnumerable<IContactItem> GetContacts(int moduleId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>按联系人标识读取单条联系人资料。</zh-CN>
        ///   <en>Reads one contact record by identifier.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>联系人条目标识。</zh-CN>
        ///   <en>Contact item identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配联系人；不存在时返回 <c>null</c>，调用方随后必须完成模块归属校验。</zh-CN>
        ///   <en>The matching contact, or <c>null</c> when it does not exist; callers must then complete module ownership validation.</en>
        /// </l>
        /// </returns>
        IContactItem GetSingleContact(int itemId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除一条已通过外层归属校验的联系人资料。</zh-CN>
        ///   <en>Deletes a contact record whose ownership has already been verified by the outer layer.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>要删除的联系人条目标识。</zh-CN>
        ///   <en>The contact item identifier to delete.</en>
        /// </l>
        /// </param>
        void DeleteContact(int itemId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>在指定模块下新增联系人资料。</zh-CN>
        ///   <en>Creates a contact record under the specified module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>所属联系人模块实例标识。</zh-CN>
        ///   <en>The owning contact module instance identifier.</en>
        /// </l>
        /// </param>
        /// <param name="userName">
        /// <l>
        ///   <zh-CN>服务器端确认的操作者名称；用于历史创建人字段。</zh-CN>
        ///   <en>The server-confirmed operator name used for the legacy created-by field.</en>
        /// </l>
        /// </param>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>联系人显示名称。</zh-CN>
        ///   <en>The contact display name.</en>
        /// </l>
        /// </param>
        /// <param name="role">
        /// <l>
        ///   <zh-CN>联系人角色、职位或业务说明文本。</zh-CN>
        ///   <en>The contact role, position, or business description text.</en>
        /// </l>
        /// </param>
        /// <param name="email">
        /// <l>
        ///   <zh-CN>联系人邮箱；仅作为展示联系方式，不参与账号认证。</zh-CN>
        ///   <en>The contact email address; it is display contact information only and is not used for account authentication.</en>
        /// </l>
        /// </param>
        /// <param name="contact1">
        /// <l>
        ///   <zh-CN>第一组扩展联系方式。</zh-CN>
        ///   <en>The first extended contact-information field.</en>
        /// </l>
        /// </param>
        /// <param name="contact2">
        /// <l>
        ///   <zh-CN>第二组扩展联系方式。</zh-CN>
        ///   <en>The second extended contact-information field.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>新建联系人条目的数据库标识。</zh-CN>
        ///   <en>The database identifier of the newly created contact record.</en>
        /// </l>
        /// </returns>
        int AddContact(int moduleId, string userName, string name, string role, string email,
                       string contact1, string contact2);

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新一条已通过外层归属校验的联系人资料。</zh-CN>
        ///   <en>Updates a contact record whose ownership has already been verified by the outer layer.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>要更新的联系人条目标识。</zh-CN>
        ///   <en>The contact item identifier to update.</en>
        /// </l>
        /// </param>
        /// <param name="userName">
        /// <l>
        ///   <zh-CN>服务器端确认的操作者名称；用于历史更新元数据。</zh-CN>
        ///   <en>The server-confirmed operator name used for legacy update metadata.</en>
        /// </l>
        /// </param>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>联系人显示名称。</zh-CN>
        ///   <en>The contact display name.</en>
        /// </l>
        /// </param>
        /// <param name="role">
        /// <l>
        ///   <zh-CN>联系人角色、职位或业务说明文本。</zh-CN>
        ///   <en>The contact role, position, or business description text.</en>
        /// </l>
        /// </param>
        /// <param name="email">
        /// <l>
        ///   <zh-CN>联系人邮箱。</zh-CN>
        ///   <en>The contact email address.</en>
        /// </l>
        /// </param>
        /// <param name="contact1">
        /// <l>
        ///   <zh-CN>第一组扩展联系方式。</zh-CN>
        ///   <en>The first extended contact-information field.</en>
        /// </l>
        /// </param>
        /// <param name="contact2">
        /// <l>
        ///   <zh-CN>第二组扩展联系方式。</zh-CN>
        ///   <en>The second extended contact-information field.</en>
        /// </l>
        /// </param>
        void UpdateContact(int itemId, string userName, string name, string role, string email,
                           string contact1, string contact2);
    }
}
