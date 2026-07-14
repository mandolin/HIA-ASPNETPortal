using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：链接模块的数据访问契约。
    ///
    /// English: Data-access contract for the links module.
    /// </summary>
    public interface ILinksDb
    {
        /// <summary>
        /// 中文：读取模块链接。English: Reads links for a module.
        /// </summary>
        IEnumerable<ILinkItem> GetLinks(int moduleId);

        /// <summary>
        /// 中文：按标识读取链接；不存在时返回 <c>null</c>。English: Reads a link by identifier, returning <c>null</c> when it does not exist.
        /// </summary>
        ILinkItem GetSingleLink(int itemId);

        /// <summary>
        /// 中文：删除已验证归属的链接。English: Deletes a link whose ownership has already been verified.
        /// </summary>
        void DeleteLink(int itemId);

        /// <summary>
        /// 中文：新增链接。English: Creates a link.
        /// </summary>
        int AddLink(int moduleId, string userName, string title, string url, string mobileUrl, int viewOrder,
                    string description);

        /// <summary>
        /// 中文：更新已验证归属的链接。English: Updates a link whose ownership has already been verified.
        /// </summary>
        void UpdateLink(int itemId, string userName, string title, string url, string mobileUrl,
                        int viewOrder, string description);
    }
}
