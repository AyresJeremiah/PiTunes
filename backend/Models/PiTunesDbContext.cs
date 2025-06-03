using Microsoft.EntityFrameworkCore;

namespace backend.Models
{
    public class PiTunesDbContext : DbContext
    {
        public PiTunesDbContext(DbContextOptions<PiTunesDbContext> options)
            : base(options)
        {
        }

        public DbSet<YouTubeSearchResult> YouTubeSearchResults { get; set; }

        // We will add the other tables here later
    }
}