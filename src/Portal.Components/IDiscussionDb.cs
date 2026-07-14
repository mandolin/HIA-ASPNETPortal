using System;
using System.Collections.Generic;
using System.Data;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：讨论模块的数据访问契约。
    ///
    /// English: Data-access contract for the discussion module.
    /// </summary>
    public interface IDiscussionsDb
    {
        /// <summary>
        /// 中文：读取模块的顶级主题。English: Reads top-level topics for a module.
        /// </summary>
        List<IDiscussionItem> GetTopLevelMessages(int moduleId);

        /// <summary>
        /// 中文：读取指定父级路径的讨论线程。English: Reads a discussion thread by its parent path.
        /// </summary>
        List<IDiscussionItem> GetThreadMessages(String parent);

        /// <summary>
        /// 中文：按标识读取消息；不存在时返回 <c>null</c>。English: Reads a message by identifier, returning <c>null</c> when it does not exist.
        /// </summary>
        IDiscussionItem GetSingleMessage(int itemId);

        /// <summary>
        /// 中文：在模块中创建主题或回复。调用方必须先完成模块编辑权限与父消息归属核验。
        /// English: Creates a topic or reply in a module. The caller must verify module-edit permission and parent-message ownership first.
        /// </summary>
        int AddMessage(int moduleId, int parentId, string userName, string title, string body);
    }
}
