using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace backend.Models
{
    public class PiTunesDbContextFactory : IDesignTimeDbContextFactory<PiTunesDbContext>
    {
        public PiTunesDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PiTunesDbContext>();
            optionsBuilder.UseNpgsql("Host=postgres;Port=5432;Database=pitunes_db;Username=pitunes;Password=pitunes_pw");

            return new PiTunesDbContext(optionsBuilder.Options);
        }
    }
}