using backend.Hubs;
using backend.Models;
using Microsoft.AspNetCore.SignalR;

namespace backend.Services;

public class SongHubService
{
    
    private readonly IHubContext<SocketHub> _hubContext;

    public SongHubService(IHubContext<SocketHub> hubContext)
    {
        _hubContext = hubContext;
    }

    const string ReceiveQueue = "ReceiveQueue";
    const string ReceiveNowPlaying = "ReceiveNowPlaying";
    const string SendNowPlaying = "SendNowPlaying";  // probably a typo btw
    const string ReceiveDownloadItem = "ReceiveDownloadItem";
    const string ReceiveDeletedSongFromCache = "ReceiveDeletedSongFromCache";
    
    public async Task SendQueueUpdateAsync(YouTubeItem[] items)
    {
        await _hubContext.Clients.All.SendAsync(ReceiveQueue, items);
    }

    public async Task SendNowPlayingUpdateAsync(YouTubeItem item)
    {
        await _hubContext.Clients.All.SendAsync(ReceiveNowPlaying, item);
    }
        
    public async Task SendDownloadQueueUpdateAsync(YouTubeItem[] items)
    {
        await _hubContext.Clients.All.SendAsync(SendNowPlaying, items);
    }
        
    public async Task SendDownloadedSong(YouTubeItem item)
    {
        await _hubContext.Clients.All.SendAsync(ReceiveDownloadItem, item);
    }
    
    public async Task SendDeletedSongFromCache(YouTubeItem item)
    {
        await _hubContext.Clients.All.SendAsync(ReceiveDeletedSongFromCache, item);
    }
}