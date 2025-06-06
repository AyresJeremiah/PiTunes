using backend.Models;
using Microsoft.AspNetCore.Mvc;
using backend.Services;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SongsController : ControllerBase
    {
        private readonly YouTubeService _youtube;
        private readonly YouTubeItemResult _youTubeItemResult;
        private readonly IQueueItemResult _queueItemResult;

        public SongsController(YouTubeService youtube, YouTubeItemResult youTubeItemResult, IQueueItemResult queueItemResult)
        {
            _youtube = youtube;
            _youTubeItemResult = youTubeItemResult;
            _queueItemResult = queueItemResult;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string query)
        {
            var results = await YouTubeService.SearchAsync(query);
            await this._youTubeItemResult.AddMultipleAsync(results);
            return Ok(results);
        }

        [HttpPost("queue")]
        public async Task<IActionResult> Queue([FromBody] YouTubeItem request)
        {
            await _youtube.EnqueueAsync(request.Id, this._queueItemResult, _youTubeItemResult);
            return Ok();
        }

        
        [HttpGet("queue")]
        public IActionResult GetQueue()
        {
            var queue = _youtube.GetQueue();
            return Ok(queue);
        }
        
        [HttpGet("download-queue")]
        public IActionResult GetDownloadQueue()
        {
            var queue = _youtube.GetDownloadQueue();
            return Ok(queue);
        }
        
        [HttpGet("now-playing")]
        public IActionResult GetNowPlaying()
        {
            var queue = _youtube.GetNowPlaying();
            return Ok(queue);
        }
        
        [HttpPost("skip")]
        public IActionResult Skip()
        {
            _youtube.Skip();
            return Ok();
        }
    }
}