using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    public class PortalSecurityDbContext : DbContext
    {
        public PortalSecurityDbContext(string connectionString) :
            base(connectionString)
        {
        }

        public DbSet<UserItem> Users { get; set; }
        public DbSet<RoleItem> Roles { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<UserItem>().
            //    HasKey(k => k.UserId).
            //    HasMany(c => c.Roles).
            //    WithMany(p => p.Users).
            //    Map(m =>
            //            {
            //                m.MapLeftKey("UserId");
            //                m.MapRightKey("RoleId");
            //                m.ToTable("Portal_UserRoles");
            //            });

            modelBuilder.Entity<RoleItem>().
                HasKey(k => k.RoleId).
                HasMany(c => c.Users).
                WithMany(p => p.Roles).
                Map(m =>
                        {
                            m.MapLeftKey("RoleId");
                            m.MapRightKey("UserId");
                            m.ToTable("Portal_UserRoles");
                        });
        }
    }
}