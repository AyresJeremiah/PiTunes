using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using backend.Models;
using Microsoft.OpenApi.Writers;

namespace backend.Services
{
    public class YouTubeService
    {
        private const string CacheDir = "song_cache";

#if RELEASE
        private const string YtDlpPath = "/usr/local/bin/yt-dlp";
        private const string FfplayPath = "/usr/bin/ffplay";
#else
        private const string YtDlpPath = "yt-dlp";
        private const string FfplayPath = "ffplay";
#endif

        private readonly ConcurrentQueue<YouTubeItem> _incomingQueue = new();
        private readonly ConcurrentQueue<YouTubeItem> _queue = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly SemaphoreSlim _queueSignal = new(0);
        private readonly IServiceScopeFactory _scopeFactory;

        private Process? _ffplayProcess;
        private bool _isPlaying = false;
        private YouTubeItem? nowPlaying;

        public YouTubeService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            Directory.CreateDirectory(CacheDir);
            _ = this.LoadIncomingQueueAsync();
            Task.Run(ProcessIncomingQueue);
        }
        
        // Public APIs
        public async Task EnqueueAsync(string videoId, IQueueItemResult queueItemResult, IYouTubeItemResult youTubeItemResult)
        {
            var item = await youTubeItemResult.GetByIdAsync(videoId);
            if(item?.Id == null)
            {
                throw new Exception($"YouTubeItem with ID {videoId} not found.");
            }
            
            if (_incomingQueue.Any(x => x.Id == item.Id))
            {
                return; 
            }
            _ = queueItemResult.AddAsync(new QueueItem(item.Id));
            _incomingQueue.Enqueue(item);
            _queueSignal.Release();
        }


        public Queue<YouTubeItem> GetQueue() => new Queue<YouTubeItem>(_queue);

        public YouTubeItem? GetNowPlaying() => nowPlaying;

        public void Skip()
        {
            _ffplayProcess?.Kill();
        }

        public static async Task<List<YouTubeItem>> SearchAsync(string query)
        {
            var results = new List<YouTubeItem>();

            var psi = new ProcessStartInfo
            {
                FileName = YtDlpPath,
                Arguments = $"ytsearch10:\"{query}\" --print-json --flat-playlist",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi) ?? throw new Exception("Failed to start yt-dlp process.");
            while (!process.StandardOutput.EndOfStream)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var json = JsonDocument.Parse(line);
                    var id = json.RootElement.GetProperty("id").GetString()!;
                    var title = json.RootElement.GetProperty("title").GetString()!;
                    var url = $"https://www.youtube.com/watch?v={id}";

                    string? thumbnail = null;
                    if (json.RootElement.TryGetProperty("thumbnails", out var thumbs) && thumbs.GetArrayLength() > 0)
                        thumbnail = thumbs[0].GetProperty("url").GetString();

                    results.Add(new YouTubeItem(id, title, url, thumbnail));
                }
            }

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"yt-dlp failed: {error}");
            }
            
            return results;
        }
        
        
        private async Task LoadIncomingQueueAsync()
        {
            
            using var scope = _scopeFactory.CreateScope();

            var youTubeItemResult = scope.ServiceProvider.GetRequiredService<YouTubeItemResult>();
            var queueItemResult = scope.ServiceProvider.GetRequiredService<IQueueItemResult>();

            
            var queueItems = await queueItemResult.GetAllAsync();

            // Order by insertion order (PK Id)
            var orderedQueueItems = queueItems.OrderBy(q => q.Id).ToList();

            // Get all VideoIds in insertion order
            var videoIds = orderedQueueItems.Select(q => q.VideoId).ToList();

            // Load YouTubeItems
            var youTubeItems = await youTubeItemResult.GetByIdsAsync(videoIds);

            // Build a dictionary for quick lookup
            var youTubeDict = youTubeItems.ToDictionary(x => x.Id, x => x);

            // Preserve insertion order and match VideoIds to YouTubeItems
            foreach (var queueItem in orderedQueueItems)
            {
                if (youTubeDict.TryGetValue(queueItem.VideoId, out var youTubeItem))
                {
                    _incomingQueue.Enqueue(youTubeItem);
                }
            }
            _queueSignal.Release();
        }


        // Private workers

        private async Task ProcessIncomingQueue()
        {
            while (!_cts.IsCancellationRequested)
            {
                await _queueSignal.WaitAsync(_cts.Token);
                if (_incomingQueue.TryDequeue(out var item))
                {
                    try
                    {
                        await DownloadAndCacheAsync(item.Id);
                        _queue.Enqueue(item);
                        CheckAndPlayNext();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing queue: {ex.Message}");
                    }
                }
            }
        }

        private void CheckAndPlayNext()
        {
            if (_isPlaying) return;

            if (_queue.TryDequeue(out var nextItem))
            {
                using var scope = _scopeFactory.CreateScope();
                var queueItemResult = scope.ServiceProvider.GetRequiredService<IQueueItemResult>();
                queueItemResult.DeleteByVideoIdAsync(nextItem.Id);
                Console.WriteLine($"Now playing: {nextItem.Title}");
                _ = PlayAsync(nextItem);
            }
            else
            {
                this.PlayRandom();
            }
        }
        private async void PlayRandom()
        {
            if (_isPlaying) return;

            var cacheDir = Path.Combine("song_cache");
            if (!Directory.Exists(cacheDir))
            {
                Console.WriteLine("Cache directory does not exist.");
                return;
            }

            var files = Directory.GetFiles(cacheDir, "*.mp3");
            if (files.Length == 0)
            {
                Console.WriteLine("No cached files found.");
                return;
            }

            var random = new Random();
            var randomFile = files[random.Next(files.Length)];

            using var scope = _scopeFactory.CreateScope();

            var searchResultService = scope.ServiceProvider.GetRequiredService<YouTubeItemResult>();
            var fileName = Path.GetFileNameWithoutExtension(randomFile);
            var item = await searchResultService.GetByIdAsync(fileName);

            Console.WriteLine($"Randomly selected cached song: {item.Id}");
            _ = PlayAsync(item);
        }

        private async Task PlayAsync(YouTubeItem item)
        {
            if (_isPlaying) return;

            _isPlaying = true;
            nowPlaying = item;

            var filePath = await DownloadAndCacheAsync(item.Id);
            Console.WriteLine($"Starting playback for {filePath}");

            var ffplay = new ProcessStartInfo
            {
                FileName = FfplayPath,
                Arguments = $"-nodisp -autoexit \"{filePath}\"",
                RedirectStandardInput = false,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _ffplayProcess = Process.Start(ffplay);
            if (_ffplayProcess == null)
            {
                _isPlaying = false;
                throw new Exception("Failed to start ffplay process.");
            }

            _ffplayProcess.EnableRaisingEvents = true;
            _ffplayProcess.Exited += OnProcessExited;

            await _ffplayProcess.WaitForExitAsync();
        }

        private void OnProcessExited(object? sender, EventArgs e)
        {
            _isPlaying = false;
            nowPlaying = null;
            CheckAndPlayNext();
        }

        private static async Task<string> DownloadAndCacheAsync(string videoId)
        {
            var outputPath = Path.Combine(CacheDir, $"{videoId}.mp3");

            if (File.Exists(outputPath))
            {
                Console.WriteLine($"Using cached file: {outputPath}");
                return outputPath;
            }

            var url = $"https://www.youtube.com/watch?v={videoId}";
            var psi = new ProcessStartInfo
            {
                FileName = YtDlpPath,
                Arguments = $"-f bestaudio -o \"{outputPath}\" --extract-audio --audio-format mp3 {url}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi) ?? throw new Exception("Failed to start yt-dlp process.");
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"yt-dlp failed: {error}");
            }

            Console.WriteLine($"Downloaded and cached: {outputPath}");
            return outputPath;
        }
    }
}
