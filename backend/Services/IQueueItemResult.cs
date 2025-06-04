using backend.Models;

namespace backend.Services
{
    public interface IQueueItemResult
    {
        Task<IEnumerable<QueueItem>> GetAllAsync();
        Task<QueueItem?> GetByIdAsync(int id);
        Task AddAsync(QueueItem entity);
        Task UpdateAsync(QueueItem entity);
        Task DeleteAsync(int id);
        Task DeleteByVideoIdAsync(string videoId);
    }
}