using backend.Models;
using Microsoft.AspNetCore.Mvc;
using backend.Services;
using Services;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SongsController : ControllerBase
    {
        private readonly YouTubeService _youtube;
        private readonly SearchResultService _searchResultService;

        public SongsController(YouTubeService youtube, SearchResultService searchResultService)
        {
            _youtube = youtube;
            _searchResultService = searchResultService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string query)
        {
            var results = await YouTubeService.SearchAsync(query);
            await this._searchResultService.AddMultipleAsync(results);
            return Ok(results);
        }

        [HttpPost("queue")]
        public async Task<IActionResult> Queue([FromBody] QueueItem request)
        {
            _youtube.Enqueue(request);
            return Ok();
        }

        
        [HttpGet("queue")]
        public IActionResult GetQueue()
        {
            var queue = _youtube.GetQueue();
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