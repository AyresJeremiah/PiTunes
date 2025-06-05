using backend.Models;
using Microsoft.AspNetCore.SignalR;

namespace backend.hubs
{
    public class SocketHub : Hub
    {
        public async Task SendQueue(YouTubeItem[] items)
        {
            await Clients.All.SendAsync("ReceiveQueue", items);
        }

        public async Task SendNowPlaying(YouTubeItem item)
        {
            await Clients.All.SendAsync("ReceiveNowPlaying", item);
        }
    }
}