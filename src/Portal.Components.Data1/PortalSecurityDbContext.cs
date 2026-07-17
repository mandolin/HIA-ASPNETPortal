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
        /// 用户注册审核记录。
        /// </summary>
        public DbSet<UserRegistrationItem> UserRegistrations { get; set; }

        /// <summary>
        /// 临时注册链接记录。
        /// </summary>
        public DbSet<RegistrationInviteItem> RegistrationInvites { get; set; }

        /// <summary>
        /// 中文：用户强哈希凭据记录。
        ///
        /// English: User strong-hash credential records.
        /// </summary>
        public DbSet<UserCredentialItem> UserCredentials { get; set; }

        /// <summary>
        /// 中文：用户会话安全版本记录。
        ///
        /// English: User session security-version records.
        /// </summary>
        public DbSet<UserSecurityStateItem> UserSecurityStates { get; set; }

        /// <summary>
        /// 中文：P6.2 企业用户资料扩展记录。
        ///
        /// English: P6.2 enterprise user-profile extension records.
        /// </summary>
        public DbSet<UserProfileItem> UserProfiles { get; set; }

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

            // 注册审核表不建立 EF 导航关系，保持对旧 Portal_Users 映射的低侵入扩展。
            // The review tables intentionally avoid EF navigation properties to keep legacy user mapping stable.
            modelBuilder.Entity<RegistrationInviteItem>()
                .HasKey(invite => invite.InviteCode);

            // 中文：凭据和安全版本表以 UserId 为主键，不建立 EF 导航关系，避免旧 UserItem 跟踪状态被扩大。
            // English: Credential and security-version tables use UserId as their key and avoid EF navigation
            // relationships so legacy UserItem tracking remains narrow.
            modelBuilder.Entity<UserCredentialItem>()
                .HasKey(credential => credential.UserId);

            modelBuilder.Entity<UserSecurityStateItem>()
                .HasKey(state => state.UserId);

            // 中文：用户资料扩展同样以 UserId 为主键，不建立导航关系，避免旧账号实体承担新生命周期状态。
            // English: User-profile extensions also use UserId as the key and avoid navigation properties so the
            // legacy user entity does not own the new lifecycle status.
            modelBuilder.Entity<UserProfileItem>()
                .HasKey(profile => profile.UserId);
        }
    }
}
