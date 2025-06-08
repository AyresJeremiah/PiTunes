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
    const string ReceiveDownloadedSong = "ReceiveDownloadedSong";
    const string ReceiveDeletedSongFromCache = "ReceiveDeletedSongFromCache";
    private const string ReceiveDownloadQueue = "ReceiveDownloadQueue";
    
    public async Task SendQueueUpdateAsync(YouTubeItem[] items)
    {
        await _hubContext.Clients.All.SendAsync(ReceiveQueue, items);
    }

    public async Task SendNowPlayingUpdateAsync(YouTubeItem? item)
    {
        await _hubContext.Clients.All.SendAsync(ReceiveNowPlaying, item);
    }
        
    public async Task SendDownloadQueueUpdateAsync(YouTubeItem[] items)
    {
        await _hubContext.Clients.All.SendAsync(ReceiveDownloadQueue, items);
    }
        
    public async Task SendDownloadedSongAsync(YouTubeItem item)
    {
        await _hubContext.Clients.All.SendAsync(ReceiveDownloadedSong, item);
    }
    
    public async Task SendDeletedSongFromCacheAsync(YouTubeItem item)
    {
        await _hubContext.Clients.All.SendAsync(ReceiveDeletedSongFromCache, item);
    }
}