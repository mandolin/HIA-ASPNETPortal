namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：受信任 HTML 模块的数据访问契约。
    ///
    /// English: Data-access contract for trusted HTML modules.
    /// </summary>
    public interface IHtmlTextsDb
    {
        /// <summary>
        /// 中文：读取模块 HTML；尚无记录时返回 <c>null</c>。English: Reads module HTML, returning <c>null</c> before its record exists.
        /// </summary>
        IHtmlTextItem GetHtmlText(int moduleId);

        /// <summary>
        /// 中文：写入模块 HTML。调用方负责仅允许受信任管理员提交原始 HTML，并按当前兼容约定编码存储。
        /// English: Writes module HTML. The caller must limit raw HTML submission to trusted administrators and encode it for storage under the current compatibility convention.
        /// </summary>
        void UpdateHtmlText(int moduleId, string desktopHtml, string mobileSummary, string mobileDetails);
    }
}
