using System.Data.Entity;

namespace ASPNET.StarterKit.Portal
{
    public class PortalDbContext : DbContext
    {
        public PortalDbContext(string connectionString) :
            base(connectionString)
        {
        }

        public DbSet<AnnouncementItem> Announcements { get; set; }
        public DbSet<ContactItem> Contacts { get; set; }

        public DbSet<EventItem> Events { get; set; }
        public DbSet<HtmlTextItem> HtmlTexts { get; set; }
        public DbSet<DocumentItem> Documents { get; set; }
        public DbSet<LinkItem> Links { get; set; }
    }
}