namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>受信任 HTML 模块内容项契约。</zh-CN>
    ///     <en>Contract for a trusted HTML module content item.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该接口服务于旧 HTML 模块编辑与展示路径。`DesktopHtml` 可以包含受信任管理员保存的原始 HTML；
    ///       允许输入原始 HTML 的权限、请求验证例外和未来净化策略必须在页面/权限层显式处理。
    ///     </zh-CN>
    ///     <en>
    ///       This interface serves the legacy HTML module editing and display paths. `DesktopHtml` may contain raw HTML saved by
    ///       trusted administrators; permission to enter raw HTML, request-validation exceptions, and future sanitization policy must be handled explicitly in the page or authorization layer.
    ///     </en>
    ///   </lang>
    /// </remarks>
    public interface IHtmlTextItem
    {
        /// <summary>
        ///   <l>
        ///     <zh-CN>拥有该 HTML 内容的模块实例标识。</zh-CN>
        ///     <en>Module instance identifier that owns this HTML content.</en>
        ///   </l>
        /// </summary>
        int ModuleId { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>桌面端原始 HTML 内容；展示层按受信任 HTML 策略输出。</zh-CN>
        ///     <en>Desktop raw HTML content; the presentation layer emits it according to the trusted-HTML policy.</en>
        ///   </l>
        /// </summary>
        string DesktopHtml { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>历史移动端摘要字段，当前主要作为兼容数据保留。</zh-CN>
        ///     <en>Legacy mobile summary field, currently retained mainly for data compatibility.</en>
        ///   </l>
        /// </summary>
        string MobileSummary { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>历史移动端详情字段，当前主要作为兼容数据保留。</zh-CN>
        ///     <en>Legacy mobile detail field, currently retained mainly for data compatibility.</en>
        ///   </l>
        /// </summary>
        string MobileDetails { get; set; }
    }
}
