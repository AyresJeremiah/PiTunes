namespace backend.Models;

public class YouTubeItem
{
    // Parameterless constructor required for EF Core
    public YouTubeItem() { }

    public YouTubeItem(string id, string title, string url, string? thumbnail)
    {
        Id = id;
        Title = title;
        Url = url;
        Thumbnail = thumbnail ?? "/assets/default-thumbnail.jpg"; // Default thumbnail if none provided
    }

    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string Thumbnail { get; set; } = null!;
}