using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class QueueItemResult : IQueueItemResult
{
    private readonly PiTunesDbContext _context;
    private IQueueItemResult _queueItemResultImplementation;

    public QueueItemResult(PiTunesDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<QueueItem>> GetAllAsync()
    {
        return await _context.QueueItem.ToListAsync();
    }

    public async Task<QueueItem?> GetByIdAsync(int id)
    {
        return await _context.QueueItem.FindAsync(id);
    }

    public async Task AddAsync(QueueItem entity)
    {
        _context.QueueItem.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddMultipleAsync(IList<QueueItem> entities)
    {
        var ids = entities.Select(e => e.Id).ToList();

        // Get existing IDs from the database
        var existingIds = await _context.QueueItem
            .Where(e => ids.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync();

        // Filter out entities that already exist
        var newEntities = entities
            .Where(e => !existingIds.Contains(e.Id))
            .ToList();

        if (newEntities.Count > 0)
        {
            await _context.QueueItem.AddRangeAsync(newEntities);
            await _context.SaveChangesAsync();
        }
    }


    public async Task UpdateAsync(QueueItem entity)
    {
        _context.QueueItem.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.QueueItem.FindAsync(id);
        if (entity != null)
        {
            _context.QueueItem.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task DeleteByVideoIdAsync(string videoId)
    {
        var entity = await _context.QueueItem
            .FirstOrDefaultAsync(q => q.VideoId == videoId);
        if (entity != null)
        {
            _context.QueueItem.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
