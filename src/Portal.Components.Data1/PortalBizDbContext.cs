using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>P6.3 企业业务基础数据上下文。</zh-CN>
    ///   <en>P6.3 enterprise business foundation data context.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>当前上下文仍使用门户主连接串。未来如接入外部 HR 或独立业务库，应通过 ADR 重新定义连接和同步边界。</zh-CN>
    ///   <en>This context currently uses the main Portal connection string. Future external HR or separate business database integration must redefine connection and synchronization boundaries through an ADR.</en>
    /// </lang>
    /// </remarks>
    public class PortalBizDbContext : DbContext
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>使用门户连接串初始化业务基础数据上下文。</zh-CN>
        ///   <en>Initializes the business foundation data context with the Portal connection string.</en>
        /// </lang>
        /// </summary>
        /// <param name="connectionString">
        /// <l>
        ///   <zh-CN>门户数据库连接字符串。</zh-CN>
        ///   <en>Portal database connection string.</en>
        /// </l>
        /// </param>
        public PortalBizDbContext(string connectionString)
            : base(connectionString)
        {
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>组织单元记录。</zh-CN>
        ///   <en>Organization-unit rows.</en>
        /// </lang>
        /// </summary>
        public DbSet<OrganizationUnitItem> OrganizationUnits { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工主数据记录。</zh-CN>
        ///   <en>Employee master-data rows.</en>
        /// </lang>
        /// </summary>
        public DbSet<EmployeeItem> Employees { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>门户账号与员工绑定记录。</zh-CN>
        ///   <en>Portal-user to employee binding rows.</en>
        /// </lang>
        /// </summary>
        public DbSet<UserEmployeeBindingItem> UserEmployeeBindings { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>配置最小主键映射，不建立导航关系。</zh-CN>
        ///   <en>Configures minimal key mappings without navigation relationships.</en>
        /// </lang>
        /// </summary>
        /// <param name="modelBuilder">
        /// <l>
        ///   <zh-CN>EF 模型构建器。</zh-CN>
        ///   <en>EF model builder.</en>
        /// </l>
        /// </param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrganizationUnitItem>()
                .HasKey(item => item.OrganizationUnitId);

            modelBuilder.Entity<EmployeeItem>()
                .HasKey(item => item.EmployeeId);

            modelBuilder.Entity<UserEmployeeBindingItem>()
                .HasKey(item => item.BindingId);
        }
    }
}
