using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using backend.Models;
using Microsoft.AspNetCore.SignalR;
using backend.hubs;

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
        private readonly IHubContext<SocketHub> _hubContext;

        private Process? _ffplayProcess;
        private bool _isPlaying = false;
        private YouTubeItem? _nowPlaying;

        public YouTubeService(IServiceScopeFactory scopeFactory, IHubContext<SocketHub> hubContext)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            Directory.CreateDirectory(CacheDir);
            _ = this.LoadQueueFromDbAsync();
            Task.Run(ProcessIncomingQueue);
        }

        // Public APIs
        public async Task EnqueueAsync(string videoId, IQueueItemResult queueItemResult,
            IYouTubeItemResult youTubeItemResult)
        {
            var item = await youTubeItemResult.GetByIdAsync(videoId);
            if (item?.Id == null)
            {
                throw new Exception($"YouTubeItem with ID {videoId} not found.");
            }

            if (_incomingQueue.Any(x => x.Id == item.Id) || _queue.Any(x => x.Id == item.Id))
            {
                Console.WriteLine($"Item {item.Id} is already in the queue.");
                return;
            }

            _ = queueItemResult.AddAsync(new QueueItem(item.Id));
            _incomingQueue.Enqueue(item);
            _queueSignal.Release();
        }

        public Queue<YouTubeItem> GetQueue() => new Queue<YouTubeItem>(_queue);
        
        public Queue<YouTubeItem> GetDownloadQueue() => new Queue<YouTubeItem>(_incomingQueue);

        public YouTubeItem? GetNowPlaying() => _nowPlaying;

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
                ArgumentList = { $"ytsearch10:{query}", "--print-json", "--flat-playlist" },
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

        private async Task LoadQueueFromDbAsync()
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
                    _queue.Enqueue(youTubeItem);
                }
            }

            await this.CheckAndPlayNext();
        }

        private async Task ProcessIncomingQueue()
        {
            while (!_cts.IsCancellationRequested)
            {
                await _queueSignal.WaitAsync(_cts.Token);
                await this.SendDownloadQueueUpdateAsync();
                if (_incomingQueue.TryDequeue(out var item))
                {
                    try
                    {
                        await DownloadAndCacheAsync(item.Id);
                        _queue.Enqueue(item);
                        await this.SendQueueUpdateAsync();
                        await this.SendDownloadQueueUpdateAsync();
                        await CheckAndPlayNext();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing queue: {ex.Message}");
                    }
                }
            }
        }

        private async Task CheckAndPlayNext()
        {
            if (_isPlaying) return;

            if (_queue.TryDequeue(out var nextItem))
            {
                using var scope = _scopeFactory.CreateScope();
                var queueItemResult = scope.ServiceProvider.GetRequiredService<IQueueItemResult>();
                await queueItemResult.DeleteByVideoIdAsync(nextItem.Id);
                await SendQueueUpdateAsync();
                Console.WriteLine($"Now playing: {nextItem.Title}");
                _ = PlayAsync(nextItem);
            }
            else
            {
                _ = this.PlayRandom();
            }
        }

        public static IEnumerable<string> GetListOfCachedSongs()
        {
            var cacheDir = Path.Combine("song_cache");
            if (!Directory.Exists(cacheDir))
            {
                Console.WriteLine("Cache directory does not exist.");
                return [];
            }

            var files = Directory.GetFiles(cacheDir, "*.mp3");

            return files
                .Select(Path.GetFileNameWithoutExtension)
                .OfType<string>() // this removes any nulls
                .Where(x => x.Length > 0);
        }
        
        private async Task PlayRandom()
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
            if (item == null)
            {
                Console.WriteLine($"No YouTubeItem found for cached file: {fileName}");
                item = new YouTubeItem(
                    fileName,
                    "Random Song",
                    "",
                    "/assets/default-thumbnail.jpg"
                ); //Default
            }

            Console.WriteLine($"Randomly selected cached song: {item.Id}");
            _ = PlayAsync(item);
        }

        private async Task PlayAsync(YouTubeItem item)
        {
            if (_isPlaying) return;
            
            _isPlaying = true;
            _nowPlaying = item;
            await this.SendNowPlayingUpdateAsync();

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
            _nowPlaying = null;
            _ = CheckAndPlayNext();
        }

        private async Task<string> DownloadAndCacheAsync(string videoId)
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
        
        private async Task PlayFaker(YouTubeItem item)
        {
            this._nowPlaying = item;
            await this.SendNowPlayingUpdateAsync();
            Console.WriteLine($"[FAKE MODE] Pretending to play song: {item.Id}");
            await Task.Delay(TimeSpan.FromSeconds(20));
            OnProcessExited(null, EventArgs.Empty);
        }

        // SignalR methods
        private async Task SendQueueUpdateAsync()
        {
            await _hubContext.Clients.All.SendAsync("ReceiveQueue", _queue.ToArray());
        }

        private async Task SendNowPlayingUpdateAsync()
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNowPlaying", _nowPlaying);
        }
        
        private async Task SendDownloadQueueUpdateAsync()
        {
            await _hubContext.Clients.All.SendAsync("ReceiveDownloadQueue", _incomingQueue.ToArray());
        }
    }
}