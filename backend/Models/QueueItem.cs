namespace backend.Models;

public class QueueItem 
{
    // Parameterless constructor required for EF Core
    public QueueItem() { }

    public QueueItem(string videoId)
    {
        VideoId = videoId;
    }
    public int Id { get; set; }  // Auto-increment PK
    public string VideoId { get; set; } = null!;
}