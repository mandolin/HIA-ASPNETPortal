using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>旧内容模块使用的 EF 数据上下文。</zh-CN>
    ///   <en>Entity Framework data context used by legacy content modules.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该上下文覆盖公告、联系人、事件、HTML、文档和链接等旧 `Portal_*` 内容表。它不负责系统配置、Tab/模块定义或业务域表，那些职责分别由配置上下文和业务上下文承担。</zh-CN>
    ///   <en>This context covers legacy `Portal_*` content tables such as announcements, contacts, events, HTML, documents and links. It does not own system configuration, tab/module definitions or business-domain tables; those responsibilities belong to configuration and business contexts.</en>
    /// </lang>
    /// </remarks>
    public class PortalDbContext : DbContext
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>按外置配置解析出的连接串初始化旧内容模块上下文。</zh-CN>
        ///   <en>Initializes the legacy content-module context with the connection string resolved from external configuration.</en>
        /// </lang>
        /// </summary>
        /// <param name="connectionString">
        /// <l>
        ///   <zh-CN>指向门户内容数据库的完整连接串。</zh-CN>
        ///   <en>Full connection string for the portal content database.</en>
        /// </l>
        /// </param>
        public PortalDbContext(string connectionString) :
            base(connectionString)
        {
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>公告模块条目集合。</zh-CN>
        ///   <en>Announcement module item set.</en>
        /// </lang>
        /// </summary>
        public DbSet<AnnouncementItem> Announcements { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>联系人模块条目集合。</zh-CN>
        ///   <en>Contact module item set.</en>
        /// </lang>
        /// </summary>
        public DbSet<ContactItem> Contacts { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>事件模块条目集合。</zh-CN>
        ///   <en>Event module item set.</en>
        /// </lang>
        /// </summary>
        public DbSet<EventItem> Events { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>受信任 HTML 模块条目集合。</zh-CN>
        ///   <en>Trusted HTML module item set.</en>
        /// </lang>
        /// </summary>
        public DbSet<HtmlTextItem> HtmlTexts { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>文档模块条目集合，文件读取和上传限制由调用层处理。</zh-CN>
        ///   <en>Document module item set; file reading and upload limits are handled by callers.</en>
        /// </lang>
        /// </summary>
        public DbSet<DocumentItem> Documents { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>链接模块条目集合，URL 安全策略由模块服务和页面层执行。</zh-CN>
        ///   <en>Link module item set; URL safety policy is enforced by module services and page code.</en>
        /// </lang>
        /// </summary>
        public DbSet<LinkItem> Links { get; set; }
    }
}
