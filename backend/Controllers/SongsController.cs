using backend.Hubs;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.Shared;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SongsController(
        YouTubeService youtube,
        YouTubeItemResult youTubeItemResult,
        IQueueItemResult queueItemResult,
        SongHubService songHub,
        AiSuggestionService aiService)
        : ControllerBase
    {
        [HttpGet("search")]
        public async Task<IActionResult> Search(string query)
        {
            var results = await YouTubeService.SearchAsync(query);
            await youTubeItemResult.AddMultipleAsync(results);
            return Ok(results);
        }

        [HttpPost("queue")]
        public async Task<IActionResult> Queue([FromBody] YouTubeItem request)
        {
            await youtube.EnqueueAsync(request.Id, queueItemResult, youTubeItemResult);
            return Ok();
        }

        [HttpGet("queue")]
        public IActionResult GetQueue()
        {
            var queue = youtube.GetQueue();
            return Ok(queue);
        }

        [HttpGet("downloaded")]
        public async Task<IActionResult> GetDownloadedSongs()
        {
            var downloadedSongs = SongCacheHandler.GetListOfCachedSongs();

            var dataBaseItems = await youTubeItemResult.GetAllAsync();

            var downloadedItems = dataBaseItems
                .Where(item => downloadedSongs.Contains(item.Id))
                .ToList();

            return Ok(downloadedItems);
        }

        [HttpGet("download-queue")]
        public IActionResult GetDownloadQueue()
        {
            var queue = youtube.GetDownloadQueue();
            return Ok(queue);
        }

        [HttpGet("now-playing")]
        public IActionResult GetNowPlaying()
        {
            var queue = youtube.GetNowPlaying();
            return Ok(queue);
        }

        [HttpPost("skip")]
        public IActionResult Skip()
        {
            youtube.Skip();
            return Ok();
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] YouTubeItem request)
        {
            var item = await youTubeItemResult.GetByIdAsync(request.Id);

            if (item == null)
            {
                throw new FileNotFoundException($"YouTubeItem with ID {request.Id} not found.");
            }

            if (youtube.GetNowPlaying()?.Id == item.Id)
            {
                youtube.Skip();
            }
            else
            {
                youtube.Dequeue(item);
            }

            await SongCacheHandler.DeleteCachedSongAsync(item.Id);
            await songHub.SendDeletedSongFromCacheAsync(item);
            return Ok();
        }

        [HttpPost("dequeue")]
        public IActionResult Dequeue([FromBody] YouTubeItem request)
        {
            youtube.Dequeue(request);
            return Ok();
        }
        
        [HttpPost("suggest")]
        public async Task<IActionResult> Suggest([FromBody] SuggestRequest request)
        {
            var suggestions = await aiService.GetSuggestionsAsync(request.Prompt);
            return Ok(suggestions);
        }
    }
}