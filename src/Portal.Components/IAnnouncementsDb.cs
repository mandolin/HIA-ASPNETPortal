using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>公告内容模块的数据访问契约。</zh-CN>
    ///   <en>Data-access contract for announcement content modules.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该接口只负责旧公告表的读取和写入。模块编辑权限、条目归属校验、站内回跳和链接地址策略应在页面层或服务层完成，避免数据访问实现隐式承担授权职责。</zh-CN>
    ///   <en>This interface only reads and writes the legacy announcements table. Module-edit authorization, item ownership checks, safe return navigation, and link policy validation should be completed by the page or service layer so the data-access implementation does not implicitly own authorization.</en>
    /// </lang>
    /// </remarks>
    public interface IAnnouncementsDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定模块当前仍有效的公告列表。</zh-CN>
        ///   <en>Reads currently active announcements for the specified module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>公告模块实例标识；调用方应来自当前 Tab 的已解析模块。</zh-CN>
        ///   <en>The announcement module instance identifier; callers should obtain it from the resolved module on the current Tab.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>未过期公告集合；展示层仍负责 HTML 编码和链接渲染策略。</zh-CN>
        ///   <en>The non-expired announcement collection; the presentation layer still owns HTML encoding and link rendering policy.</en>
        /// </l>
        /// </returns>
        IEnumerable<IAnnouncementItem> GetAnnouncements(int moduleId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>按公告标识读取单条公告。</zh-CN>
        ///   <en>Reads a single announcement by identifier.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>公告条目标识。</zh-CN>
        ///   <en>Announcement item identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配公告；不存在时返回 <c>null</c>，调用方随后必须判断其是否属于当前可编辑模块。</zh-CN>
        ///   <en>The matching announcement, or <c>null</c> when it does not exist; callers must then verify that it belongs to the currently editable module.</en>
        /// </l>
        /// </returns>
        IAnnouncementItem GetSingleAnnouncement(int itemId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除一条已通过外层归属校验的公告。</zh-CN>
        ///   <en>Deletes an announcement whose ownership has already been verified by the outer layer.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>要删除的公告条目标识。</zh-CN>
        ///   <en>The announcement item identifier to delete.</en>
        /// </l>
        /// </param>
        void DeleteAnnouncement(int itemId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>在指定模块下新增公告。</zh-CN>
        ///   <en>Creates an announcement under the specified module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>所属公告模块实例标识。</zh-CN>
        ///   <en>The owning announcement module instance identifier.</en>
        /// </l>
        /// </param>
        /// <param name="userName">
        /// <l>
        ///   <zh-CN>服务器端确认的操作者名称；用于历史创建人字段，不作为授权依据。</zh-CN>
        ///   <en>The server-confirmed operator name; used for the legacy created-by field and not as an authorization source.</en>
        /// </l>
        /// </param>
        /// <param name="title">
        /// <l>
        ///   <zh-CN>公告标题；调用方应先完成长度和显示层安全处理。</zh-CN>
        ///   <en>The announcement title; callers should complete length validation and presentation-safety handling first.</en>
        /// </l>
        /// </param>
        /// <param name="expireDate">
        /// <l>
        ///   <zh-CN>公告过期时间；读取列表时用于过滤当前有效公告。</zh-CN>
        ///   <en>The announcement expiration time; list reads use it to filter currently active announcements.</en>
        /// </l>
        /// </param>
        /// <param name="description">
        /// <l>
        ///   <zh-CN>公告正文或摘要文本；数据层不执行 HTML 净化。</zh-CN>
        ///   <en>The announcement body or summary text; the data layer does not perform HTML sanitization.</en>
        /// </l>
        /// </param>
        /// <param name="moreLink">
        /// <l>
        ///   <zh-CN>桌面端更多链接；写入前应由调用方套用当前 URL 策略。</zh-CN>
        ///   <en>The desktop more link; callers should apply the current URL policy before writing it.</en>
        /// </l>
        /// </param>
        /// <param name="mobileMoreLink">
        /// <l>
        ///   <zh-CN>旧移动端更多链接；保留为空表示不提供旧移动链接。</zh-CN>
        ///   <en>The legacy mobile more link; leave empty when no legacy mobile link should be provided.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>新建公告的数据库标识。</zh-CN>
        ///   <en>The database identifier of the newly created announcement.</en>
        /// </l>
        /// </returns>
        int AddAnnouncement(int moduleId, string userName, string title, DateTime expireDate,
                            string description, string moreLink, string mobileMoreLink);

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新一条已通过外层归属校验的公告。</zh-CN>
        ///   <en>Updates an announcement whose ownership has already been verified by the outer layer.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>要更新的公告条目标识。</zh-CN>
        ///   <en>The announcement item identifier to update.</en>
        /// </l>
        /// </param>
        /// <param name="userName">
        /// <l>
        ///   <zh-CN>服务器端确认的操作者名称；用于历史更新字段，不作为授权依据。</zh-CN>
        ///   <en>The server-confirmed operator name; used for legacy update metadata and not as an authorization source.</en>
        /// </l>
        /// </param>
        /// <param name="title">
        /// <l>
        ///   <zh-CN>公告标题。</zh-CN>
        ///   <en>The announcement title.</en>
        /// </l>
        /// </param>
        /// <param name="expireDate">
        /// <l>
        ///   <zh-CN>公告过期时间。</zh-CN>
        ///   <en>The announcement expiration time.</en>
        /// </l>
        /// </param>
        /// <param name="description">
        /// <l>
        ///   <zh-CN>公告正文或摘要文本。</zh-CN>
        ///   <en>The announcement body or summary text.</en>
        /// </l>
        /// </param>
        /// <param name="moreLink">
        /// <l>
        ///   <zh-CN>桌面端更多链接；调用方负责 URL 策略和安全回退。</zh-CN>
        ///   <en>The desktop more link; callers own URL policy validation and safe fallback.</en>
        /// </l>
        /// </param>
        /// <param name="mobileMoreLink">
        /// <l>
        ///   <zh-CN>旧移动端更多链接。</zh-CN>
        ///   <en>The legacy mobile more link.</en>
        /// </l>
        /// </param>
        void UpdateAnnouncement(int itemId, string userName, string title, DateTime expireDate,
                                string description, string moreLink, string mobileMoreLink);
    }
}
