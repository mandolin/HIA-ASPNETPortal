using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>通过 EF 上下文读写旧联系人模块数据。</zh-CN>
    ///   <en>Reads and writes legacy contact-module data through the EF context.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本类只做联系人持久化和模块过滤；联系人字段的展示编码、邮件链接策略、编辑权限和条目归属校验由调用页与前台模块负责。</zh-CN>
    ///   <en>This class only performs contact persistence and module filtering; display encoding, mail-link policy, edit permission, and item-ownership checks are handled by caller pages and front-end modules.</en>
    /// </lang>
    /// </remarks>
    public class ContactsDb : IContactsDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>门户业务 EF 上下文，封装旧内容模块表映射。</zh-CN>
        ///   <en>Portal business EF context that wraps legacy content-module table mappings.</en>
        /// </lang>
        /// </summary>
        private readonly PortalDbContext _context;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化联系人数据访问对象。</zh-CN>
        ///   <en>Initializes the contact data-access object.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>由 Unity 注入的门户 EF 上下文。</zh-CN>
        ///   <en>Portal EF context injected by Unity.</en>
        /// </l>
        /// </param>
        public ContactsDb(PortalDbContext context)
        {
            _context = context;
        }

        #region IContactsDb Members

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取指定模块下的联系人列表。</zh-CN>
        ///   <en>Gets contacts for the specified module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId"><l><zh-CN>联系人模块实例 ID。</zh-CN><en>Contact module instance ID.</en></l></param>
        /// <returns><l><zh-CN>属于该模块的联系人集合。</zh-CN><en>Contacts belonging to the module.</en></l></returns>
        public IEnumerable<IContactItem> GetContacts(int moduleId)
        {
            // <lang>
            //   <zh-CN>只按模块过滤，不在数据层追加排序规则；旧页面按数据库默认顺序展示。</zh-CN>
            //   <en>Filter only by module and do not add a data-layer ordering rule; legacy pages display the database default order.</en>
            // </lang>
            return _context.Contacts.Where(i => i.ModuleId == moduleId).ToList<IContactItem>();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>获取单个联系人；用户指定的不存在标识返回 <c>null</c>，由调用页决定其授权失败响应。</zh-CN>
        ///   <en>Gets one contact. A user-supplied missing identifier returns <c>null</c> so the caller can select its authorization-failure response.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>联系人标识符。</zh-CN>
        ///   <en>Contact identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>指定联系人；不存在时为 <c>null</c>。</zh-CN>
        ///   <en>The requested contact, or <c>null</c> when it does not exist.</en>
        /// </l>
        /// </returns>
        public IContactItem GetSingleContact(int itemId)
        {
            // <lang>
            //   <zh-CN>编辑入口的 ItemId 来自请求，未命中返回空值，让页面层统一输出低敏提示或拒绝访问响应。</zh-CN>
            //   <en>The editor item identifier comes from a request; misses return null so the page layer can emit a low-sensitivity message or access-denied response.</en>
            // </lang>
            return _context.Contacts.SingleOrDefault(i => i.ItemId == itemId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除指定联系人记录。</zh-CN>
        ///   <en>Deletes the specified contact record.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该方法使用 <c>Single</c> 保留旧代码的严格语义；调用页应先确认条目存在且属于当前模块。</zh-CN>
        ///   <en>This method keeps the strict legacy <c>Single</c> semantics; caller pages should first confirm that the item exists and belongs to the current module.</en>
        /// </lang>
        /// </remarks>
        /// <param name="itemId"><l><zh-CN>联系人标识符。</zh-CN><en>Contact identifier.</en></l></param>
        public void DeleteContact(int itemId)
        {
            var item = _context.Contacts.Single(i => i.ItemId == itemId);

            // <lang>
            //   <zh-CN>删除动作直接提交到旧表；审计、权限和站内回跳由编辑页完成。</zh-CN>
            //   <en>The delete action is committed directly to the legacy table; audit, permission, and safe return navigation are completed by the editor page.</en>
            // </lang>
            _context.Contacts.Remove(item);
            _context.SaveChanges();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>新增一个联系人记录。</zh-CN>
        ///   <en>Adds a new contact record.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>邮箱和联系方式按普通文本保存；格式校验、敏感信息限制和前台编码由编辑页与展示控件负责。</zh-CN>
        ///   <en>Email and contact details are stored as ordinary text; syntax validation, sensitive-information limits, and front-end encoding are the responsibility of editor pages and display controls.</en>
        /// </lang>
        /// </remarks>
        /// <param name="moduleId"><l><zh-CN>联系人模块实例 ID。</zh-CN><en>Contact module instance ID.</en></l></param>
        /// <param name="userName"><l><zh-CN>用于历史显示的创建人名称；空值会降级为旧占位值。</zh-CN><en>Creator name used for historical display; a blank value falls back to the legacy placeholder.</en></l></param>
        /// <param name="name"><l><zh-CN>联系人姓名。</zh-CN><en>Contact name.</en></l></param>
        /// <param name="role"><l><zh-CN>联系人职责或角色描述。</zh-CN><en>Contact duty or role description.</en></l></param>
        /// <param name="email"><l><zh-CN>联系人邮箱文本。</zh-CN><en>Contact email text.</en></l></param>
        /// <param name="contact1"><l><zh-CN>主要联系方式。</zh-CN><en>Primary contact detail.</en></l></param>
        /// <param name="contact2"><l><zh-CN>次要联系方式。</zh-CN><en>Secondary contact detail.</en></l></param>
        /// <returns><l><zh-CN>新增联系人的数据库标识符。</zh-CN><en>Database identifier of the new contact.</en></l></returns>
        public int AddContact(int moduleId, string userName, string name, string role, string email,
                              string contact1, string contact2)
        {
            // <lang>
            //   <zh-CN>旧内容表只有显示用创建人字段；缺失认证名称时使用占位值，不把它作为权限依据。</zh-CN>
            //   <en>The legacy content table only has a display creator field; when the authenticated name is missing, use a placeholder and do not treat it as an authorization source.</en>
            // </lang>
            userName = string.IsNullOrEmpty(userName) ? "unknown" : userName;

            var item = new ContactItem
            {
                ModuleId = moduleId,
                CreatedByUser = userName,
                CreatedDate = DateTime.Now,
                Name = name,
                Role = role,
                Email = email,
                Contact1 = contact1,
                Contact2 = contact2
            };

            _context.Contacts.Add(item);
            _context.SaveChanges();

            return item.ItemId;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新指定联系人记录的可编辑字段。</zh-CN>
        ///   <en>Updates editable fields of the specified contact record.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>更新不会改变联系人所属模块和创建时间；调用页负责确认当前用户可编辑该条目。</zh-CN>
        ///   <en>Updating does not change the contact's owning module or creation time; the caller page is responsible for confirming that the current user may edit the item.</en>
        /// </lang>
        /// </remarks>
        /// <param name="itemId"><l><zh-CN>联系人标识符。</zh-CN><en>Contact identifier.</en></l></param>
        /// <param name="userName"><l><zh-CN>用于历史显示的最后编辑人名称。</zh-CN><en>Last editor name used for historical display.</en></l></param>
        /// <param name="name"><l><zh-CN>联系人姓名。</zh-CN><en>Contact name.</en></l></param>
        /// <param name="role"><l><zh-CN>联系人职责或角色描述。</zh-CN><en>Contact duty or role description.</en></l></param>
        /// <param name="email"><l><zh-CN>联系人邮箱文本。</zh-CN><en>Contact email text.</en></l></param>
        /// <param name="contact1"><l><zh-CN>主要联系方式。</zh-CN><en>Primary contact detail.</en></l></param>
        /// <param name="contact2"><l><zh-CN>次要联系方式。</zh-CN><en>Secondary contact detail.</en></l></param>
        public void UpdateContact(int itemId, string userName, string name, string role, string email,
                                  string contact1, string contact2)
        {
            // <lang>
            //   <zh-CN>保持和新增路径一致的显示名占位策略。</zh-CN>
            //   <en>Keep the same display-name placeholder strategy as the add path.</en>
            // </lang>
            userName = string.IsNullOrEmpty(userName) ? "unknown" : userName;

            var item = _context.Contacts.Single(i => i.ItemId == itemId);

            // <lang>
            //   <zh-CN>旧表没有独立“最后编辑人”字段，当前实现沿用 CreatedByUser 保存最近一次编辑显示名；创建时间保持原值。</zh-CN>
            //   <en>The legacy table has no separate last-editor field, so the current implementation reuses CreatedByUser for the latest editor display name; creation time keeps its original value.</en>
            // </lang>
            item.CreatedByUser = userName;
            item.Name = name;
            item.Role = role;
            item.Email = email;
            item.Contact1 = contact1;
            item.Contact2 = contact2;

            _context.SaveChanges();
        }

        #endregion
    }
}
