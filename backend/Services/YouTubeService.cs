using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using backend.Models;
using backend.Hubs;
using backend.Shared;

namespace backend.Services
{
    public class YouTubeService
    {
        private readonly ConcurrentQueue<YouTubeItem> _incomingQueue = new();
        private readonly ConcurrentQueue<YouTubeItem> _queue = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly SemaphoreSlim _queueSignal = new(0);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SongHubService _songHub;

        private Process? _ffplayProcess;
        private bool _isPlaying = false;
        private YouTubeItem? _nowPlaying;

        public YouTubeService(IServiceScopeFactory scopeFactory, SongHubService songHub)
        {
            _scopeFactory = scopeFactory;
            _songHub = songHub;
            SongCacheHandler.CreateCacheDirectory();
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

        public void Dequeue(YouTubeItem item)
        {
            if (_queue.All(x => x.Id != item.Id))
            {
               return;
            } 
            var tempQueue = new Queue<YouTubeItem>();

            while (_queue.TryDequeue(out var tempItem))
            {
                if (tempItem.Id != item.Id)
                {
                    tempQueue.Enqueue(tempItem);
                }
            }
            
            foreach (var tempItem in tempQueue)
            {
                _queue.Enqueue(tempItem);
            }

            _ = _songHub.SendQueueUpdateAsync(_queue.ToArray());
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
                FileName = SongCacheHandler.YtDlpPath,
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

        //Processes Handling
        private async Task PlayRandom()
        {
            if (_isPlaying) return;

            var files = SongCacheHandler.GetListOfCachedSongs().ToArray();

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
            if (item == null) //This should not happen, but just in case
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

            _ffplayProcess = await SongCacheHandler.StartPlayback(item);
            _isPlaying = true;
            _nowPlaying = item;
            _ffplayProcess.EnableRaisingEvents = true;
            _ffplayProcess.Exited += OnProcessExited;

            await _songHub.SendNowPlayingUpdateAsync(_nowPlaying);

            await _ffplayProcess.WaitForExitAsync();
        }

        private void OnProcessExited(object? sender, EventArgs e)
        {
            _isPlaying = false;
            _nowPlaying = null;
            _ = CheckAndPlayNext();
        }

        //Background processing
        private async Task ProcessIncomingQueue()
        {
            while (!_cts.IsCancellationRequested)
            {
                await _queueSignal.WaitAsync(_cts.Token);
                await this._songHub.SendDownloadQueueUpdateAsync(_incomingQueue.ToArray());
                if (_incomingQueue.TryDequeue(out var item))
                {
                    try
                    {
                        await SongCacheHandler.DownloadAndCacheAsync(item.Id);
                        _queue.Enqueue(item);
                        await this._songHub.SendQueueUpdateAsync(_queue.ToArray());
                        await this._songHub.SendDownloadQueueUpdateAsync(_incomingQueue.ToArray());
                        await this._songHub.SendDownloadedSong(item);
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
                await this._songHub.SendQueueUpdateAsync(_queue.ToArray());
                Console.WriteLine($"Now playing: {nextItem.Title}");
                _ = PlayAsync(nextItem);
            }
            else
            {
                _ = this.PlayRandom();
            }
        }

        //On Start
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
    }
}