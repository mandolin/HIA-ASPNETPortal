using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    public class EventsDb : IEventsDb
    {
        private readonly PortalDbContext _context;

        public EventsDb(PortalDbContext context)
        {
            _context = context;
        }

        #region IEventsDb Members

        public IEnumerable<IEventItem> GetEvents(int moduleId)
        {
            return _context.Events.
                Where(i => i.ModuleId == moduleId).
                Where(i => i.ExpireDate > DateTime.Now).ToList<IEventItem>();
        }

        public IEventItem GetSingleEvent(int itemId)
        {
            return _context.Events.Single(i => i.ItemId == itemId);
        }

        public void DeleteEvent(int itemId)
        {
            EventItem item = _context.Events.Single(i => i.ItemId == itemId);
            _context.Events.Remove(item);
            _context.SaveChanges();
        }

        public int AddEvent(int moduleId, string userName, string title, DateTime expireDate,
                            string description, string wherewhen)
        {
            if (userName.Length < 1)
            {
                userName = "unknown";
            }

            var item = new EventItem
                           {
                               ModuleId = moduleId,
                               CreatedByUser = userName,
                               CreatedDate = DateTime.Now,
                               Description = description,
                               ExpireDate = expireDate,
                               Title = title,
                               WhereWhen = wherewhen
                           };

            _context.Events.Add(item);
            _context.SaveChanges();

            return item.ItemId;
        }

        public void UpdateEvent(int itemId, string userName, string title, DateTime expireDate,
                                string description, string wherewhen)
        {
            if (userName.Length < 1)
            {
                userName = "unknown";
            }

            EventItem item = _context.Events.Single(i => i.ItemId == itemId);

            item.CreatedByUser = userName;
            item.Title = title;
            item.ExpireDate = expireDate;
            item.Description = description;
            item.WhereWhen = wherewhen;

            _context.SaveChanges();
        }

        #endregion
    }
}