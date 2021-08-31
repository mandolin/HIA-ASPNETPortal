using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    public interface IAnnouncementsDb
    {
        IEnumerable<IAnnouncementItem> GetAnnouncements(int moduleId);
        IAnnouncementItem GetSingleAnnouncement(int itemId);
        void DeleteAnnouncement(int itemId);

        int AddAnnouncement(int moduleId, string userName, string title, DateTime expireDate,
                            string description, string moreLink, string mobileMoreLink);

        void UpdateAnnouncement(int itemId, string userName, string title, DateTime expireDate,
                                string description, string moreLink, string mobileMoreLink);
    }
}