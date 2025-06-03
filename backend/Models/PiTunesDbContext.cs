using Microsoft.EntityFrameworkCore;

namespace backend.Models
{
    public class PiTunesDbContext : DbContext
    {
        public PiTunesDbContext(DbContextOptions<PiTunesDbContext> options)
            : base(options)
        {
        }

        public DbSet<YouTubeItem> YouTubeItem { get; set; }
        public DbSet<QueueItem> QueueItem { get; set; }

        // We will add the other tables here later
    }
}