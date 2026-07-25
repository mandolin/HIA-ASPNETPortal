using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>安全、账号、角色和注册相关 EF 数据上下文。</zh-CN>
    ///   <en>EF data context for security, accounts, roles and registration.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>该上下文仍映射旧 Portal 用户/角色表，同时承载新增凭据、安全版本和企业用户资料扩展表。新表尽量不建立导航关系，避免扩大旧实体跟踪范围。</zh-CN>
    ///   <en>This context still maps the legacy Portal user and role tables while carrying new credential, security-version and enterprise profile extension tables. New tables intentionally avoid navigation properties where possible so legacy entity tracking stays narrow.</en>
    /// </lang>
    /// </remarks>
    public class PortalSecurityDbContext : DbContext
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>使用提供的连接字符串初始化安全数据上下文。</zh-CN>
        ///   <en>Initializes the security data context with the supplied connection string.</en>
        /// </lang>
        /// </summary>
        /// <param name="connectionString">
        /// <l>
        ///   <zh-CN>数据库连接字符串；由外置配置加载，不应写入源码。</zh-CN>
        ///   <en>Database connection string loaded from external configuration and not written into source code.</en>
        /// </l>
        /// </param>
        public PortalSecurityDbContext(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>旧门户用户表数据集。</zh-CN>
        ///   <en>Legacy Portal user-table set.</en>
        /// </lang>
        /// </summary>
        public DbSet<UserItem> Users { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>旧门户角色表数据集。</zh-CN>
        ///   <en>Legacy Portal role-table set.</en>
        /// </lang>
        /// </summary>
        public DbSet<RoleItem> Roles { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>用户注册审核记录。</zh-CN>
        ///   <en>User registration review records.</en>
        /// </lang>
        /// </summary>
        public DbSet<UserRegistrationItem> UserRegistrations { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>临时注册链接记录。</zh-CN>
        ///   <en>Temporary registration invite records.</en>
        /// </lang>
        /// </summary>
        public DbSet<RegistrationInviteItem> RegistrationInvites { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>用户强哈希凭据记录。</zh-CN>
        ///   <en>User strong-hash credential records.</en>
        /// </lang>
        /// </summary>
        public DbSet<UserCredentialItem> UserCredentials { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>用户会话安全版本记录。</zh-CN>
        ///   <en>User session security-version records.</en>
        /// </lang>
        /// </summary>
        public DbSet<UserSecurityStateItem> UserSecurityStates { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>P6.2 企业用户资料扩展记录。</zh-CN>
        ///   <en>P6.2 enterprise user-profile extension records.</en>
        /// </lang>
        /// </summary>
        public DbSet<UserProfileItem> UserProfiles { get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>配置旧表与新增安全扩展表之间的 EF 映射。</zh-CN>
        ///   <en>Configures EF mappings between legacy tables and new security extension tables.</en>
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
            // <lang>
            //   <zh-CN>旧用户和角色仍使用 `Portal_UserRoles` 多对多表；这里只声明现有结构，不改变表名或键名。</zh-CN>
            //   <en>Legacy users and roles still use the `Portal_UserRoles` many-to-many table; this declares the existing structure without changing table or key names.</en>
            // </lang>
            modelBuilder.Entity<RoleItem>()
                .HasMany(role => role.Users)
                .WithMany(user => user.Roles)
                .Map(map =>
                {
                    // <lang>
                    //   <zh-CN>指定旧关联表名。</zh-CN>
                    //   <en>Specify the legacy join-table name.</en>
                    // </lang>
                    map.ToTable("Portal_UserRoles");

                    // <lang>
                    //   <zh-CN>保持旧映射方向：左键为 RoleId。</zh-CN>
                    //   <en>Keep the legacy mapping direction: the left key is RoleId.</en>
                    // </lang>
                    map.MapLeftKey("RoleId");

                    // <lang>
                    //   <zh-CN>保持旧映射方向：右键为 UserId。</zh-CN>
                    //   <en>Keep the legacy mapping direction: the right key is UserId.</en>
                    // </lang>
                    map.MapRightKey("UserId");
                });

            // <lang>
            //   <zh-CN>注册审核表不建立 EF 导航关系，保持对旧 `Portal_Users` 映射的低侵入扩展。</zh-CN>
            //   <en>Registration review tables intentionally avoid EF navigation properties to keep legacy `Portal_Users` mapping stable.</en>
            // </lang>
            modelBuilder.Entity<RegistrationInviteItem>()
                .HasKey(invite => invite.InviteCode);

            // <lang>
            //   <zh-CN>凭据和安全版本表以 UserId 为主键，不建立 EF 导航关系，避免旧 UserItem 跟踪状态被扩大。</zh-CN>
            //   <en>Credential and security-version tables use UserId as their key and avoid EF navigation relationships so legacy UserItem tracking remains narrow.</en>
            // </lang>
            modelBuilder.Entity<UserCredentialItem>()
                .HasKey(credential => credential.UserId);

            modelBuilder.Entity<UserSecurityStateItem>()
                .HasKey(state => state.UserId);

            // <lang>
            //   <zh-CN>用户资料扩展同样以 UserId 为主键，不建立导航关系，避免旧账号实体承担新生命周期状态。</zh-CN>
            //   <en>User-profile extensions also use UserId as the key and avoid navigation properties so the legacy user entity does not own the new lifecycle status.</en>
            // </lang>
            modelBuilder.Entity<UserProfileItem>()
                .HasKey(profile => profile.UserId);
        }
    }
}
