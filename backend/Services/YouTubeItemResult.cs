using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class YouTubeItemResult : IYouTubeItemResult
    {
        private readonly PiTunesDbContext _context;
        private IYouTubeItemResult _youTubeItemResultImplementation;

        public YouTubeItemResult(PiTunesDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<YouTubeItem>> GetAllAsync()
        {
            return await _context.YouTubeItem.ToListAsync();
        }

        public async Task<YouTubeItem?> GetByIdAsync(string id)
        {
            return await _context.YouTubeItem.FindAsync(id);
        }
        
        public async Task<List<YouTubeItem>> GetByIdsAsync(IEnumerable<string> ids)
        {
            return await _context.YouTubeItem
                .Where(item => ids.Contains(item.Id))
                .ToListAsync();
        }

        public async Task AddAsync(YouTubeItem entity)
        {
            _context.YouTubeItem.Add(entity);
            await _context.SaveChangesAsync();
        }
        
        public async Task AddMultipleAsync(IList<YouTubeItem> entities)
        {
            var ids = entities.Select(e => e.Id).ToList();

            // Get existing IDs from the database
            var existingIds = await _context.YouTubeItem
                .Where(e => ids.Contains(e.Id))
                .Select(e => e.Id)
                .ToListAsync();

            // Filter out entities that already exist
            var newEntities = entities
                .Where(e => !existingIds.Contains(e.Id))
                .ToList();

            if (newEntities.Count > 0)
            {
                await _context.YouTubeItem.AddRangeAsync(newEntities);
                await _context.SaveChangesAsync();
            }
        }


        public async Task UpdateAsync(YouTubeItem entity)
        {
            _context.YouTubeItem.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.YouTubeItem.FindAsync(id);
            if (entity != null)
            {
                _context.YouTubeItem.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}
