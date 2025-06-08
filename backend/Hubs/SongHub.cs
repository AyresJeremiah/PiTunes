using backend.Models;
using Microsoft.AspNetCore.SignalR;

namespace backend.hubs;

public abstract class SongHub(IHubContext<SocketHub> hubContext)
{
    const string ReceiveQueue = "ReceiveQueue";
    const string ReceiveNowPlaying = "ReceiveNowPlaying";
    const string SendNowPlaying = "SendNowPlaying";
    const string ReceiveDownloadItem = "ReceiveDownloadItem";
    const string ReceiveDeletedSongFromCache = "ReceiveDeletedSongFromCache";
    
    public async Task SendQueueUpdateAsync(YouTubeItem[] items)
    {
        await hubContext.Clients.All.SendAsync(ReceiveQueue, items);
    }

    public async Task SendNowPlayingUpdateAsync(YouTubeItem item)
    {
        await hubContext.Clients.All.SendAsync(ReceiveNowPlaying, item);
    }
        
    public async Task SendDownloadQueueUpdateAsync(YouTubeItem[] items)
    {
        await hubContext.Clients.All.SendAsync(SendNowPlaying, items);
    }
        
    public async Task SendDownloadedSong(YouTubeItem item)
    {
        await hubContext.Clients.All.SendAsync(ReceiveDownloadItem, item);
    }
    
    public async Task SendDeletedSongFromCache(YouTubeItem item)
    {
        await hubContext.Clients.All.SendAsync(ReceiveDeletedSongFromCache, item);
    }
}