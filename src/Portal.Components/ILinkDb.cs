using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>链接内容模块的数据访问契约。</zh-CN>
    ///   <en>Data-access contract for link content modules.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该接口只保存链接模块的历史字段。URL 是否允许点击、是否回退为普通文本、是否允许移动端字段，应由调用方和展示层按当前 Portal URL 策略判断。</zh-CN>
    ///   <en>This interface only stores the historical fields for link modules. Whether a URL may be clickable, should fall back to plain text, or may use the mobile field is decided by callers and the presentation layer under the current Portal URL policy.</en>
    /// </lang>
    /// </remarks>
    public interface ILinksDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定模块下的链接列表。</zh-CN>
        ///   <en>Reads links under the specified module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>链接模块实例标识。</zh-CN>
        ///   <en>The link module instance identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>按旧显示顺序返回的链接集合；展示层仍负责 URL 策略和文本编码。</zh-CN>
        ///   <en>The link collection returned in legacy display order; the presentation layer still owns URL policy and text encoding.</en>
        /// </l>
        /// </returns>
        IEnumerable<ILinkItem> GetLinks(int moduleId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>按链接标识读取单条链接。</zh-CN>
        ///   <en>Reads a single link by identifier.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>链接条目标识。</zh-CN>
        ///   <en>Link item identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配链接；不存在时返回 <c>null</c>，调用方随后必须完成模块归属校验。</zh-CN>
        ///   <en>The matching link, or <c>null</c> when it does not exist; callers must then complete module ownership validation.</en>
        /// </l>
        /// </returns>
        ILinkItem GetSingleLink(int itemId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>删除一条已通过外层归属校验的链接。</zh-CN>
        ///   <en>Deletes a link whose ownership has already been verified by the outer layer.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>要删除的链接条目标识。</zh-CN>
        ///   <en>The link item identifier to delete.</en>
        /// </l>
        /// </param>
        void DeleteLink(int itemId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>在指定模块下新增链接。</zh-CN>
        ///   <en>Creates a link under the specified module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>所属链接模块实例标识。</zh-CN>
        ///   <en>The owning link module instance identifier.</en>
        /// </l>
        /// </param>
        /// <param name="userName">
        /// <l>
        ///   <zh-CN>服务器端确认的操作者名称；用于历史创建人字段。</zh-CN>
        ///   <en>The server-confirmed operator name used for the legacy created-by field.</en>
        /// </l>
        /// </param>
        /// <param name="title">
        /// <l>
        ///   <zh-CN>链接标题。</zh-CN>
        ///   <en>The link title.</en>
        /// </l>
        /// </param>
        /// <param name="url">
        /// <l>
        ///   <zh-CN>桌面端链接地址；写入前应由调用方完成 URL 策略校验。</zh-CN>
        ///   <en>The desktop link URL; callers should apply URL policy validation before writing it.</en>
        /// </l>
        /// </param>
        /// <param name="mobileUrl">
        /// <l>
        ///   <zh-CN>旧移动端链接地址；为空表示不提供旧移动链接。</zh-CN>
        ///   <en>The legacy mobile link URL; empty means no legacy mobile link is provided.</en>
        /// </l>
        /// </param>
        /// <param name="viewOrder">
        /// <l>
        ///   <zh-CN>旧模块显示顺序。</zh-CN>
        ///   <en>The legacy module display order.</en>
        /// </l>
        /// </param>
        /// <param name="description">
        /// <l>
        ///   <zh-CN>链接说明文本；展示层负责编码。</zh-CN>
        ///   <en>The link description text; the presentation layer owns encoding.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>新建链接的数据库标识。</zh-CN>
        ///   <en>The database identifier of the newly created link.</en>
        /// </l>
        /// </returns>
        int AddLink(int moduleId, string userName, string title, string url, string mobileUrl, int viewOrder,
                    string description);

        /// <summary>
        /// <lang>
        ///   <zh-CN>更新一条已通过外层归属校验的链接。</zh-CN>
        ///   <en>Updates a link whose ownership has already been verified by the outer layer.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>要更新的链接条目标识。</zh-CN>
        ///   <en>The link item identifier to update.</en>
        /// </l>
        /// </param>
        /// <param name="userName">
        /// <l>
        ///   <zh-CN>服务器端确认的操作者名称；用于历史更新元数据。</zh-CN>
        ///   <en>The server-confirmed operator name used for legacy update metadata.</en>
        /// </l>
        /// </param>
        /// <param name="title">
        /// <l>
        ///   <zh-CN>链接标题。</zh-CN>
        ///   <en>The link title.</en>
        /// </l>
        /// </param>
        /// <param name="url">
        /// <l>
        ///   <zh-CN>桌面端链接地址；调用方负责 URL 策略和安全回退。</zh-CN>
        ///   <en>The desktop link URL; callers own URL policy validation and safe fallback.</en>
        /// </l>
        /// </param>
        /// <param name="mobileUrl">
        /// <l>
        ///   <zh-CN>旧移动端链接地址。</zh-CN>
        ///   <en>The legacy mobile link URL.</en>
        /// </l>
        /// </param>
        /// <param name="viewOrder">
        /// <l>
        ///   <zh-CN>旧模块显示顺序。</zh-CN>
        ///   <en>The legacy module display order.</en>
        /// </l>
        /// </param>
        /// <param name="description">
        /// <l>
        ///   <zh-CN>链接说明文本。</zh-CN>
        ///   <en>The link description text.</en>
        /// </l>
        /// </param>
        void UpdateLink(int itemId, string userName, string title, string url, string mobileUrl,
                        int viewOrder, string description);
    }
}
