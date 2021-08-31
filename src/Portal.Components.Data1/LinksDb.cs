using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    public class LinksDb : ILinksDb
    {
        private readonly PortalDbContext _context;

        public LinksDb(PortalDbContext context)
        {
            _context = context;
        }

        #region ILinksDb Members

        public IEnumerable<ILinkItem> GetLinks(int moduleId)
        {
            return _context.Links.
                Where(i => i.ModuleId == moduleId).ToList<ILinkItem>();
        }

        public ILinkItem GetSingleLink(int itemId)
        {
            return _context.Links.
                Single(i => i.ItemId == itemId);
        }

        public void DeleteLink(int itemId)
        {
            LinkItem item = _context.Links.
                Single(i => i.ItemId == itemId);

            _context.Links.Remove(item);
            _context.SaveChanges();
        }

        public int AddLink(int moduleId, string userName, string title, string url, string mobileUrl,
                           int viewOrder, string description)
        {
            if (userName.Length < 1)
            {
                userName = "unknown";
            }

            var item = new LinkItem
                           {
                               ModuleId = moduleId,
                               CreatedByUser = userName,
                               CreatedDate = DateTime.Now,
                               Title = title,
                               Url = url,
                               MobileUrl = mobileUrl,
                               ViewOrder = viewOrder,
                               Description = description
                           };

            _context.Links.Add(item);
            _context.SaveChanges();

            return item.ItemId;
        }

        public void UpdateLink(int itemId, string userName, string title, string url, string mobileUrl,
                               int viewOrder, string description)
        {
            if (userName.Length < 1)
            {
                userName = "unknown";
            }

            LinkItem item = _context.Links.
                Single(i => i.ItemId == itemId);

            item.CreatedByUser = userName;
            item.Title = title;
            item.Url = url;
            item.MobileUrl = mobileUrl;
            item.ViewOrder = viewOrder;
            item.Description = description;

            _context.SaveChanges();
        }

        #endregion
    }
}