using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    public class PortalCfgDbContext : DbContext
    {
        public PortalCfgDbContext(string connectionString) :
            base(connectionString)
        {
        }

        public DbSet<ModuleDefinitionItem> ModuleDefinitions { get; set; }
        public DbSet<GlobalItem> Globals { get; set; }
        public DbSet<TabItem> Tabs { get; set; }
        public DbSet<ModuleItem> Modules { get; set; }
        public DbSet<ModuleSettingItem> Settings { get; set; }
    }
}