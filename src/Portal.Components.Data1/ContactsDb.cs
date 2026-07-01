using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Reflection;

namespace ASPNET.StarterKit.Portal
{
    public class ContactsDb : IContactsDb
    {
        private readonly PortalDbContext _context;

        public ContactsDb(PortalDbContext context)
        {
            _context = context;
        }

        #region IContactsDb Members

        /// <summary>
        /// 获取指定模块ID下的所有联系人。
        /// </summary>
        /// <param name="moduleId">模块标识符。</param>
        /// <returns>联系人集合。</returns>
        public IEnumerable<IContactItem> GetContacts(int moduleId)
        {
            // 使用 LINQ 查询获取指定模块ID下的所有联系人
            return _context.Contacts.Where(i => i.ModuleId == moduleId).ToList<IContactItem>();
        }

        /// <summary>
        /// 获取单个联系人。
        /// </summary>
        /// <param name="itemId">联系人标识符。</param>
        /// <returns>指定ID的联系人对象。</returns>
        public IContactItem GetSingleContact(int itemId)
        {
            // 使用 Single 方法获取指定ID的联系人
            return _context.Contacts.Single(i => i.ItemId == itemId);
        }

        /// <summary>
        /// 删除指定ID的联系人。
        /// </summary>
        /// <param name="itemId">联系人标识符。</param>
        public void DeleteContact(int itemId)
        {
            // 获取指定ID的联系人对象
            var item = _context.Contacts.Single(i => i.ItemId == itemId);
            // 从上下文中移除联系人对象
            _context.Contacts.Remove(item);
            // 提交更改到数据库
            _context.SaveChanges();
        }

        /// <summary>
        /// 添加一个新的联系人。
        /// </summary>
        /// <param name="moduleId">模块标识符。</param>
        /// <param name="userName">用户名。</param>
        /// <param name="name">联系人姓名。</param>
        /// <param name="role">联系人角色。</param>
        /// <param name="email">电子邮件地址。</param>
        /// <param name="contact1">主要联系方式。</param>
        /// <param name="contact2">次要联系方式。</param>
        /// <returns>新联系人的ID。</returns>
        public int AddContact(int moduleId, string userName, string name, string role, string email,
                              string contact1, string contact2)
        {
            // 如果用户名为空，则设置为 'unknown'
            userName = string.IsNullOrEmpty(userName) ? "unknown" : userName;

            // 创建新的联系人对象
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

            // 将新联系人添加到上下文
            _context.Contacts.Add(item);
            // 提交更改到数据库
            _context.SaveChanges();

            // 返回新联系人的ID
            return item.ItemId;
        }

        /// <summary>
        /// 更新指定ID的联系人信息。
        /// </summary>
        /// <param name="itemId">联系人标识符。</param>
        /// <param name="userName">用户名。</param>
        /// <param name="name">联系人姓名。</param>
        /// <param name="role">联系人角色。</param>
        /// <param name="email">电子邮件地址。</param>
        /// <param name="contact1">主要联系方式。</param>
        /// <param name="contact2">次要联系方式。</param>
        public void UpdateContact(int itemId, string userName, string name, string role, string email,
                                  string contact1, string contact2)
        {
            // 如果用户名为空，则设置为 'unknown'
            userName = string.IsNullOrEmpty(userName) ? "unknown" : userName;

            // 获取指定ID的联系人对象
            var item = _context.Contacts.Single(i => i.ItemId == itemId);

            // 更新联系人的各项属性
            item.ItemId = itemId;
            item.CreatedByUser = userName;
            item.CreatedDate = item.CreatedDate; // 保持不变
            item.Name = name;
            item.Role = role;
            item.Email = email;
            item.Contact1 = contact1;
            item.Contact2 = contact2;

            // 提交更改到数据库
            _context.SaveChanges();
        }

        #endregion
    }
}