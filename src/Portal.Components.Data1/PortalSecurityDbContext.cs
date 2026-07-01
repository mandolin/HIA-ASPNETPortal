using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 数据库上下文类用于处理与安全相关的数据操作。
    /// </summary>
    public class PortalSecurityDbContext : DbContext
    {
        /// <summary>
        /// 使用提供的连接字符串初始化一个新的数据库上下文实例。
        /// </summary>
        /// <param name="connectionString">数据库连接字符串。</param>
        public PortalSecurityDbContext(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// 用户表的数据集。
        /// </summary>
        public DbSet<UserItem> Users { get; set; }

        /// <summary>
        /// 角色表的数据集。
        /// </summary>
        public DbSet<RoleItem> Roles { get; set; }

        /// <summary>
        /// 在模型构建时进行配置。
        /// </summary>
        /// <param name="modelBuilder">模型构建器。</param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // 配置用户与角色之间的多对多关系
            modelBuilder.Entity<RoleItem>()
                .HasMany(role => role.Users)
                .WithMany(user => user.Roles)
                .Map(map =>
                {
                    // 指定关联表名
                    map.ToTable("Portal_UserRoles");

                    // 设置用户ID作为外键
                    map.MapLeftKey("RoleId");

                    // 设置角色ID作为外键
                    map.MapRightKey("UserId");
                });
        }
    }
}