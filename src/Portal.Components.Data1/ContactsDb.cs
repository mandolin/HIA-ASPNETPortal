using System;
using System.Collections.Generic;
using System.Linq;

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

        public IEnumerable<IContactItem> GetContacts(int moduleId)
        {
            return _context.Contacts.
                Where(i => i.ModuleId == moduleId).
                ToList<IContactItem>();
        }


        public IContactItem GetSingleContact(int itemId)
        {
            return _context.Contacts.
                Single(i => i.ItemId == itemId);
        }


        public void DeleteContact(int itemId)
        {
            ContactItem item = _context.Contacts.Single(i => i.ItemId == itemId);
            _context.Contacts.Remove(item);
            _context.SaveChanges();
        }


        public int AddContact(int moduleId, string userName, string name, string role, string email,
                              string contact1, string contact2)
        {
            if (userName.Length < 1)
            {
                userName = "unknown";
            }

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


        public void UpdateContact(int itemId, string userName, string name, string role, string email,
                                  string contact1, string contact2)
        {
            if (userName.Length < 1)
            {
                userName = "unknown";
            }

            ContactItem item = _context.Contacts.Single(i => i.ItemId == itemId);
            item.ItemId = itemId;
            item.CreatedByUser = userName;
            item.CreatedDate = DateTime.Now;
            item.Name = name;
            item.Role = role;
            item.Contact1 = contact1;
            item.Contact2 = contact2;
            item.Email = email;

            _context.SaveChanges();
        }

        #endregion
    }
}