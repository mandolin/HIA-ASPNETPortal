using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>门户结构配置表使用的 EF 数据上下文。</zh-CN>
    ///   <en>Entity Framework data context used by portal structural configuration tables.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该上下文负责旧 `PortalCfg_*` 模块定义、全局配置、Tab、模块实例和模块设置数据。业务数据、运行时日志和系统设置扩展表不通过此上下文维护。</zh-CN>
    ///   <en>This context owns legacy `PortalCfg_*` module definitions, global settings, tabs, module instances and module setting data. Business data, runtime logs and system-setting extension tables are maintained outside this context.</en>
    /// </lang>
    /// </remarks>
    public class PortalCfgDbContext : DbContext
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>按外置配置解析出的连接串初始化门户结构配置上下文。</zh-CN>
        ///   <en>Initializes the portal structural configuration context with the connection string resolved from external configuration.</en>
        /// </lang>
        /// </summary>
        /// <param name="connectionString">
        /// <l>
        ///   <zh-CN>指向门户配置数据库的完整连接串。</zh-CN>
        ///   <en>Full connection string for the portal configuration database.</en>
        /// </l>
        /// </param>
        public PortalCfgDbContext(string connectionString) :
            base(connectionString)
        {
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>模块类型定义集合，对应旧模块定义维护入口。</zh-CN>
        ///   <en>Module type definition set corresponding to the legacy module-definition maintenance entry.</en>
        /// </lang>
        /// </summary>
        public DbSet<ModuleDefinitionItem> ModuleDefinitions { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>旧门户全局配置项集合。</zh-CN>
        ///   <en>Legacy portal global setting set.</en>
        /// </lang>
        /// </summary>
        public DbSet<GlobalItem> Globals { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>门户 Tab 配置集合。</zh-CN>
        ///   <en>Portal tab configuration set.</en>
        /// </lang>
        /// </summary>
        public DbSet<TabItem> Tabs { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>Tab 内模块实例配置集合。</zh-CN>
        ///   <en>Module instance configuration set mounted inside tabs.</en>
        /// </lang>
        /// </summary>
        public DbSet<ModuleItem> Modules { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>模块实例键值设置集合。</zh-CN>
        ///   <en>Key-value setting set for module instances.</en>
        /// </lang>
        /// </summary>
        public DbSet<ModuleSettingItem> Settings { get; set; }
    }
}
