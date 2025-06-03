using backend.Models;

namespace Services
{
    public interface ISearchResultService
    {
        Task<IEnumerable<YouTubeItem>> GetAllAsync();
        Task<YouTubeItem?> GetByIdAsync(int id);
        Task AddAsync(YouTubeItem entity);
        Task UpdateAsync(YouTubeItem entity);
        Task DeleteAsync(int id);
    }
}