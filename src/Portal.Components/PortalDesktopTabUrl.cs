using System.Globalization;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户桌面页签地址的唯一构造点。</zh-CN>
    ///   <en>The single construction point for Portal desktop tab URLs.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>页签地址形状（<c>DesktopDefault.aspx?tabindex=&amp;tabid=</c>）此前在多处各自拼接，形态一致但无单一出处；集中到此使"地址形状"只有一个定义，避免各处随时间漂移出细微差异。**<c>tabid</c> 是授权真源**：目标页只校验 <c>tabid</c>，<c>tabindex</c> 仅用于页面内匿名登录模块的注入启发式，因此即使 <c>tabindex</c> 取值口径不同（过滤前或过滤后的下标）也不会改变授权结论。</zh-CN>
    ///   <en>The tab URL shape (<c>DesktopDefault.aspx?tabindex=&amp;tabid=</c>) had been assembled in several places with the same shape but no single origin; centralizing it gives the shape one definition so the copies cannot drift into subtle differences over time. **<c>tabid</c> is the authorization source of truth**: the target page validates <c>tabid</c> only, while <c>tabindex</c> merely feeds the in-page anonymous-login injection heuristic, so a differing <c>tabindex</c> convention (filtered or unfiltered index) cannot change the authorization outcome.</en>
    /// </lang>
    /// </remarks>
    public static class PortalDesktopTabUrl
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>构造指向指定页签的桌面地址。</zh-CN>
        ///   <en>Builds a desktop URL pointing at the specified tab.</en>
        /// </lang>
        /// </summary>
        /// <param name="applicationPath">
        /// <l>
        ///   <zh-CN>应用相对路径前缀（可含虚拟目录）；为空时按根站点处理。</zh-CN>
        ///   <en>The application-relative path prefix (which may include a virtual directory); blank means the site root.</en>
        /// </l>
        /// </param>
        /// <param name="tabIndex">
        /// <l>
        ///   <zh-CN>页签在门户桌面列表中的下标，仅影响匿名登录模块的注入启发式。</zh-CN>
        ///   <en>The tab's index in the portal desktop list; it only affects the anonymous-login injection heuristic.</en>
        /// </l>
        /// </param>
        /// <param name="tabId">
        /// <l>
        ///   <zh-CN>页签标识，目标页校验的授权真源。</zh-CN>
        ///   <en>The tab identifier, the authorization source of truth validated by the target page.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>应用相对的可导航地址；数值一律用固定区域性格式化，避免本地化数字分隔符进入 URL。</zh-CN>
        ///   <en>An application-relative navigable URL; numbers always use invariant formatting so localized digit separators never enter the URL.</en>
        /// </l>
        /// </returns>
        public static string Build(string applicationPath, int tabIndex, int tabId)
        {
            return (applicationPath ?? string.Empty) + "/DesktopDefault.aspx?tabindex=" +
                tabIndex.ToString(CultureInfo.InvariantCulture) + "&tabid=" +
                tabId.ToString(CultureInfo.InvariantCulture);
        }
    }
}
