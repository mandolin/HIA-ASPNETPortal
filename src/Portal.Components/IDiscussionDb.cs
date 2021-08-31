using System;
using System.Data;

namespace ASPNET.StarterKit.Portal
{
    public interface IDiscussionsDb
    {
        IDataReader GetTopLevelMessages(int moduleId);
        IDataReader GetThreadMessages(String parent);
        IDataReader GetSingleMessage(int itemId);
        int AddMessage(int moduleId, int parentId, string userName, string title, string body);
    }
}