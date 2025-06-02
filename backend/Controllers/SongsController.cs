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

        public SongsController(YouTubeService youtube)
        {
            _youtube = youtube;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string query)
        {
            var results = await YouTubeService.SearchAsync(query);
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