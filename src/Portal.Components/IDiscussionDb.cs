using System;
using System.Collections.Generic;
using System.Data;

namespace ASPNET.StarterKit.Portal
{
    public interface IDiscussionsDb
    {
        List<IDiscussionItem> GetTopLevelMessages(int moduleId);
        List<IDiscussionItem> GetThreadMessages(String parent);
        IDiscussionItem GetSingleMessage(int itemId);
        int AddMessage(int moduleId, int parentId, string userName, string title, string body);
    }
}