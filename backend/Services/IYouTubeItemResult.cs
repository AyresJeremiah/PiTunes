using backend.Models;

namespace backend.Services
{
    public interface IYouTubeItemResult
    {
        Task<IEnumerable<YouTubeItem>> GetAllAsync();
        Task<YouTubeItem?> GetByIdAsync(string id);
        Task<List<YouTubeItem>> GetByIdsAsync(IEnumerable<string> ids);
        Task AddAsync(YouTubeItem entity);
        Task UpdateAsync(YouTubeItem entity);
        Task DeleteAsync(int id);
    }
}