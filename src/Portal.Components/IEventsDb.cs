using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    public interface IEventsDb
    {
        IEnumerable<IEventItem> GetEvents(int moduleId);
        IEventItem GetSingleEvent(int itemId);
        void DeleteEvent(int itemId);

        int AddEvent(int moduleId, string userName, string title, DateTime expireDate, string description,
                     string wherewhen);

        void UpdateEvent(int itemId, string userName, string title, DateTime expireDate,
                         string description, string wherewhen);
    }
}