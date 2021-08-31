using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    public class AnnouncementsDb : IAnnouncementsDb
    {
        private readonly PortalDbContext _context;

        public AnnouncementsDb(PortalDbContext context)
        {
            _context = context;
        }

        #region IAnnouncementsDb Members

        public IEnumerable<IAnnouncementItem> GetAnnouncements(int moduleId)
        {
            return _context.Announcements.
                Where(i => i.ModuleId == moduleId).
                Where(i => i.ExpireDate > DateTime.Now).
                ToList<IAnnouncementItem>();
        }

        public IAnnouncementItem GetSingleAnnouncement(int itemId)
        {
            return _context.Announcements.Single(i => i.ItemId == itemId);
        }

        public void DeleteAnnouncement(int itemId)
        {
            AnnouncementItem item = _context.Announcements.Single(i => i.ItemId == itemId);
            _context.Announcements.Remove(item);
            _context.SaveChanges();
        }


        public int AddAnnouncement(int moduleId, string userName, string title, DateTime expireDate,
                                   string description, string moreLink, string mobileMoreLink)
        {
            if (userName.Length < 1)
            {
                userName = "unknown";
            }

            var item = new AnnouncementItem
                           {
                               ModuleId = moduleId,
                               CreatedByUser = userName,
                               CreatedDate = DateTime.Now,
                               Description = description,
                               ExpireDate = expireDate,
                               MoreLink = moreLink,
                               MobileMoreLink = mobileMoreLink,
                               Title = title
                           };

            _context.Announcements.Add(item);
            _context.SaveChanges();

            return item.ItemId;
        }


        public void UpdateAnnouncement(int itemId, string userName, string title, DateTime expireDate,
                                       string description, string moreLink, string mobileMoreLink)
        {
            if (userName.Length < 1)
            {
                userName = "unknown";
            }

            AnnouncementItem item = _context.Announcements.Single(i => i.ItemId == itemId);
            item.ItemId = itemId;
            item.CreatedByUser = userName;
            item.CreatedDate = DateTime.Now;
            item.Description = description;
            item.ExpireDate = expireDate;
            item.MoreLink = moreLink;
            item.MobileMoreLink = mobileMoreLink;
            item.Title = title;

            _context.SaveChanges();
        }

        #endregion
    }
}