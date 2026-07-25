using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>讨论内容模块的数据访问契约。</zh-CN>
    ///   <en>Data-access contract for discussion content modules.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该接口沿用旧讨论模块的线程模型：顶级主题按模块读取，回复线程按 <c>DisplayOrder</c> 父级路径读取。调用方负责模块访问、模块编辑权限、父消息归属和展示层 HTML 编码。</zh-CN>
    ///   <en>This interface keeps the legacy discussion threading model: top-level topics are read by module, and reply threads are read by the <c>DisplayOrder</c> parent path. Callers own module access, module-edit authorization, parent-message ownership checks, and presentation-layer HTML encoding.</en>
    /// </lang>
    /// </remarks>
    public interface IDiscussionsDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定讨论模块的顶级主题。</zh-CN>
        ///   <en>Reads top-level topics for the specified discussion module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>讨论模块实例标识。</zh-CN>
        ///   <en>The discussion module instance identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>顶级讨论主题集合；正文和标题输出前仍需展示层编码。</zh-CN>
        ///   <en>The top-level discussion topics; body and title text still require presentation-layer encoding before output.</en>
        /// </l>
        /// </returns>
        List<IDiscussionItem> GetTopLevelMessages(int moduleId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>按父级线程路径读取讨论回复。</zh-CN>
        ///   <en>Reads discussion replies by parent thread path.</en>
        /// </lang>
        /// </summary>
        /// <param name="parent">
        /// <l>
        ///   <zh-CN>旧 <c>DisplayOrder</c> 父级路径；调用方应来自已读取消息或受控请求参数。</zh-CN>
        ///   <en>The legacy <c>DisplayOrder</c> parent path; callers should obtain it from an already-read message or controlled request parameter.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>父级路径下的回复集合。</zh-CN>
        ///   <en>The replies under the parent path.</en>
        /// </l>
        /// </returns>
        List<IDiscussionItem> GetThreadMessages(string parent);

        /// <summary>
        /// <lang>
        ///   <zh-CN>按消息标识读取单条讨论消息。</zh-CN>
        ///   <en>Reads a single discussion message by identifier.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>讨论消息标识。</zh-CN>
        ///   <en>Discussion message identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配消息；不存在时返回 <c>null</c>，调用方随后必须检查模块归属和访问权限。</zh-CN>
        ///   <en>The matching message, or <c>null</c> when it does not exist; callers must then check module ownership and access permission.</en>
        /// </l>
        /// </returns>
        IDiscussionItem GetSingleMessage(int itemId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>在指定模块中创建顶级主题或回复。</zh-CN>
        ///   <en>Creates a top-level topic or reply in the specified module.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>调用方必须先完成模块编辑权限与父消息归属核验。<paramref name="parentId"/> 为 0 时表示顶级主题；非 0 时应指向同模块内的父消息。</zh-CN>
        ///   <en>Callers must first verify module-edit permission and parent-message ownership. A <paramref name="parentId"/> of 0 creates a top-level topic; a non-zero value should reference a parent message in the same module.</en>
        /// </lang>
        /// </remarks>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>讨论模块实例标识。</zh-CN>
        ///   <en>The discussion module instance identifier.</en>
        /// </l>
        /// </param>
        /// <param name="parentId">
        /// <l>
        ///   <zh-CN>父消息标识；0 表示创建顶级主题。</zh-CN>
        ///   <en>The parent message identifier; 0 creates a top-level topic.</en>
        /// </l>
        /// </param>
        /// <param name="userName">
        /// <l>
        ///   <zh-CN>服务器端确认的发帖人名称；用于历史创建人字段。</zh-CN>
        ///   <en>The server-confirmed poster name used for the legacy created-by field.</en>
        /// </l>
        /// </param>
        /// <param name="title">
        /// <l>
        ///   <zh-CN>主题或回复标题。</zh-CN>
        ///   <en>The topic or reply title.</en>
        /// </l>
        /// </param>
        /// <param name="body">
        /// <l>
        ///   <zh-CN>正文文本；数据层不执行 HTML 净化或展示编码。</zh-CN>
        ///   <en>The body text; the data layer does not perform HTML sanitization or output encoding.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>新建讨论消息的数据库标识。</zh-CN>
        ///   <en>The database identifier of the newly created discussion message.</en>
        /// </l>
        /// </returns>
        int AddMessage(int moduleId, int parentId, string userName, string title, string body);
    }
}
