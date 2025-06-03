using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Services
{
    public class SearchResultService : ISearchResultService
    {
        private readonly PiTunesDbContext _context;
        private ISearchResultService _searchResultServiceImplementation;

        public SearchResultService(PiTunesDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<YouTubeItem>> GetAllAsync()
        {
            return await _context.YouTubeSearchResults.ToListAsync();
        }

        public Task<YouTubeItem?> GetByIdAsync(int id)
        {
            return _searchResultServiceImplementation.GetByIdAsync(id);
        }

        public async Task<YouTubeItem?> GetByIdAsync(string id )
        {
            return await _context.YouTubeSearchResults.FindAsync(id);
        }

        public async Task AddAsync(YouTubeItem entity)
        {
            _context.YouTubeSearchResults.Add(entity);
            await _context.SaveChangesAsync();
        }
        
        public async Task AddMultipleAsync(IEnumerable<YouTubeItem> entities)
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


        public async Task UpdateAsync(YouTubeItem entity)
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
