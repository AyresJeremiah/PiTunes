using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Services
{
    public class SearchResultService : ISearchResultService
    {
        private readonly PiTunesDbContext _context;

        public SearchResultService(PiTunesDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<YouTubeSearchResult>> GetAllAsync()
        {
            return await _context.YouTubeSearchResults.ToListAsync();
        }

        public async Task<YouTubeSearchResult?> GetByIdAsync(int id)
        {
            return await _context.YouTubeSearchResults.FindAsync(id);
        }

        public async Task AddAsync(YouTubeSearchResult entity)
        {
            _context.YouTubeSearchResults.Add(entity);
            await _context.SaveChangesAsync();
        }
        
        public async Task AddMultipleAsync(IEnumerable<YouTubeSearchResult> entities)
        {
            var ids = entities.Select(e => e.Id).ToList();

            // Get existing IDs from the database
            var existingIds = await _context.YouTubeSearchResults
                .Where(e => ids.Contains(e.Id))
                .Select(e => e.Id)
                .ToListAsync();

            // Filter out entities that already exist
            var newEntities = entities
                .Where(e => !existingIds.Contains(e.Id))
                .ToList();

            if (newEntities.Any())
            {
                await _context.YouTubeSearchResults.AddRangeAsync(newEntities);
                await _context.SaveChangesAsync();
            }
        }


        public async Task UpdateAsync(YouTubeSearchResult entity)
        {
            _context.YouTubeSearchResults.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.YouTubeSearchResults.FindAsync(id);
            if (entity != null)
            {
                _context.YouTubeSearchResults.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}
