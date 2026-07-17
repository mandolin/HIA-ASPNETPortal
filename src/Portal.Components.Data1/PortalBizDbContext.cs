using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：P6.3 企业业务基础数据上下文。
    ///
    /// English: P6.3 enterprise business foundation data context.
    /// </summary>
    /// <remarks>
    /// 中文：当前上下文仍使用门户主连接串。未来如接入外部 HR 或独立业务库，应通过 ADR 重新定义连接和同步边界。
    ///
    /// English: This context currently uses the main Portal connection string. Future external HR or separate business
    /// database integration must redefine connection and synchronization boundaries through an ADR.
    /// </remarks>
    public class PortalBizDbContext : DbContext
    {
        /// <summary>
        /// 中文：使用门户连接串初始化业务基础数据上下文。
        ///
        /// English: Initializes the business foundation data context with the Portal connection string.
        /// </summary>
        /// <param name="connectionString">中文：门户数据库连接字符串。English: Portal database connection string.</param>
        public PortalBizDbContext(string connectionString)
            : base(connectionString)
        {
        }

        /// <summary>
        /// 中文：组织单元记录。
        ///
        /// English: Organization-unit rows.
        /// </summary>
        public DbSet<OrganizationUnitItem> OrganizationUnits { get; set; }

        /// <summary>
        /// 中文：员工主数据记录。
        ///
        /// English: Employee master-data rows.
        /// </summary>
        public DbSet<EmployeeItem> Employees { get; set; }

        /// <summary>
        /// 中文：门户账号与员工绑定记录。
        ///
        /// English: Portal-user to employee binding rows.
        /// </summary>
        public DbSet<UserEmployeeBindingItem> UserEmployeeBindings { get; set; }

        /// <summary>
        /// 中文：配置最小主键映射，不建立导航关系。
        ///
        /// English: Configures minimal key mappings without navigation relationships.
        /// </summary>
        /// <param name="modelBuilder">中文：EF 模型构建器。English: EF model builder.</param>
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
