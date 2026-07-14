using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：联系人模块的数据访问契约。
    ///
    /// English: Data-access contract for the contacts module.
    /// </summary>
    public interface IContactsDb
    {
        /// <summary>
        /// 中文：读取模块联系人。English: Reads contacts for a module.
        /// </summary>
        IEnumerable<IContactItem> GetContacts(int moduleId);

        /// <summary>
        /// 中文：按标识读取联系人；不存在时返回 <c>null</c>。English: Reads a contact by identifier, returning <c>null</c> when it does not exist.
        /// </summary>
        IContactItem GetSingleContact(int itemId);

        /// <summary>
        /// 中文：删除已验证归属的联系人。English: Deletes a contact whose ownership has already been verified.
        /// </summary>
        void DeleteContact(int itemId);

        /// <summary>
        /// 中文：新增联系人。English: Creates a contact.
        /// </summary>
        int AddContact(int moduleId, string userName, string name, string role, string email,
                       string contact1, string contact2);

        /// <summary>
        /// 中文：更新已验证归属的联系人。English: Updates a contact whose ownership has already been verified.
        /// </summary>
        void UpdateContact(int itemId, string userName, string name, string role, string email,
                           string contact1, string contact2);
    }
}
