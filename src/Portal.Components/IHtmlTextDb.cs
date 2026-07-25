namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>受信任 HTML 内容模块的数据访问契约。</zh-CN>
    ///   <en>Data-access contract for trusted HTML content modules.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该接口只负责存取 HTML 模块文本，不执行 HTML 白名单、净化或授权判断。当前阶段允许受信任管理员维护原始 HTML；未来若引入更细权限或净化策略，应在调用层统一接入。</zh-CN>
    ///   <en>This interface only stores and retrieves HTML module text; it does not perform HTML allow-listing, sanitization, or authorization. The current stage allows trusted administrators to maintain raw HTML; if later fine-grained permissions or sanitization policies are introduced, they should be attached at the calling layer.</en>
    /// </lang>
    /// </remarks>
    public interface IHtmlTextsDb
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定模块的 HTML 文本记录。</zh-CN>
        ///   <en>Reads the HTML text record for the specified module.</en>
        /// </lang>
        /// </summary>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>HTML 模块实例标识。</zh-CN>
        ///   <en>The HTML module instance identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配的 HTML 文本记录；模块尚未保存内容时返回 <c>null</c>。</zh-CN>
        ///   <en>The matching HTML text record, or <c>null</c> before the module has saved content.</en>
        /// </l>
        /// </returns>
        IHtmlTextItem GetHtmlText(int moduleId);

        /// <summary>
        /// <lang>
        ///   <zh-CN>写入指定模块的桌面和旧移动端 HTML 文本。</zh-CN>
        ///   <en>Writes desktop and legacy mobile HTML text for the specified module.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>调用方必须先确认模块编辑权限和“受信任原始 HTML”边界，并按当前兼容约定对桌面 HTML、移动摘要和移动详情做编码存储。展示层在输出前再按旧模块约定解码或直接输出。</zh-CN>
        ///   <en>Callers must first confirm module-edit authorization and the trusted raw-HTML boundary, then encode desktop HTML, mobile summary, and mobile details for storage under the current compatibility convention. The presentation layer later decodes or emits content according to legacy module rules.</en>
        /// </lang>
        /// </remarks>
        /// <param name="moduleId">
        /// <l>
        ///   <zh-CN>HTML 模块实例标识。</zh-CN>
        ///   <en>The HTML module instance identifier.</en>
        /// </l>
        /// </param>
        /// <param name="desktopHtml">
        /// <l>
        ///   <zh-CN>桌面端 HTML 正文；当前仅允许受信任管理员提交原始 HTML。</zh-CN>
        ///   <en>The desktop HTML body; currently only trusted administrators may submit raw HTML.</en>
        /// </l>
        /// </param>
        /// <param name="mobileSummary">
        /// <l>
        ///   <zh-CN>旧移动端摘要 HTML 或文本。</zh-CN>
        ///   <en>The legacy mobile summary HTML or text.</en>
        /// </l>
        /// </param>
        /// <param name="mobileDetails">
        /// <l>
        ///   <zh-CN>旧移动端详情 HTML 或文本。</zh-CN>
        ///   <en>The legacy mobile details HTML or text.</en>
        /// </l>
        /// </param>
        void UpdateHtmlText(int moduleId, string desktopHtml, string mobileSummary, string mobileDetails);
    }
}
