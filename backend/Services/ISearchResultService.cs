using backend.Models;

namespace Services
{
    public interface ISearchResultService
    {
        Task<IEnumerable<YouTubeSearchResult>> GetAllAsync();
        Task<YouTubeSearchResult?> GetByIdAsync(int id);
        Task AddAsync(YouTubeSearchResult entity);
        Task UpdateAsync(YouTubeSearchResult entity);
        Task DeleteAsync(int id);
    }
}