using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   <lang>
    ///     <zh-CN>受信任 HTML 模块内容的 Entity Framework 投影。</zh-CN>
    ///     <en>Entity Framework projection for trusted HTML module content.</en>
    ///   </lang>
    /// </summary>
    /// <remarks>
    ///   <lang>
    ///     <zh-CN>
    ///       该类型映射旧表 <c>Portal_HtmlText</c>。HTML 内容由受信任管理员维护，实体层只承载字段值；
    ///       请求验证、原始 HTML 权限和输出策略由编辑页、展示控件和安全策略共同处理。
    ///     </zh-CN>
    ///     <en>
    ///       This type maps the legacy <c>Portal_HtmlText</c> table. HTML content is maintained by trusted
    ///       administrators and the entity only carries field values; request validation, raw HTML permission,
    ///       and output policy are handled by the edit page, display control, and security policy.
    ///     </en>
    ///   </lang>
    /// </remarks>
    [Table("Portal_HtmlText")]
    public class HtmlTextItem : IHtmlTextItem
    {
        #region IHtmlTextItem Members

        /// <summary>
        ///   <l>
        ///     <zh-CN>拥有该 HTML 内容的模块实例标识，同时也是旧表主键。</zh-CN>
        ///     <en>Module instance identifier that owns this HTML content and also serves as the legacy table key.</en>
        ///   </l>
        /// </summary>
        [Key]
        public int ModuleId { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>桌面端 HTML 正文；展示层按受信任 HTML 边界输出。</zh-CN>
        ///     <en>Desktop HTML body; presentation code emits it according to the trusted HTML boundary.</en>
        ///   </l>
        /// </summary>
        public string DesktopHtml { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>历史移动端摘要字段，当前作为兼容数据保留。</zh-CN>
        ///     <en>Legacy mobile summary field retained as compatibility data.</en>
        ///   </l>
        /// </summary>
        public string MobileSummary { get; set; }

        /// <summary>
        ///   <l>
        ///     <zh-CN>历史移动端详情字段，当前主题体系不直接依赖。</zh-CN>
        ///     <en>Legacy mobile details field not directly used by the current theme system.</en>
        ///   </l>
        /// </summary>
        public string MobileDetails { get; set; }

        #endregion
    }
}
