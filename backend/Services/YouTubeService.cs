using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using backend.Models;

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

        private readonly ConcurrentQueue<QueueItem> _incomingQueue = new();
        private readonly ConcurrentQueue<QueueItem> _queue = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly SemaphoreSlim _queueSignal = new(0);

        private Process? _ffplayProcess;
        private bool _isPlaying = false;
        private QueueItem? nowPlaying;

        public YouTubeService()
        {
            Directory.CreateDirectory(CacheDir);
            Task.Run(ProcessIncomingQueue);
        }

        public record YouTubeSearchResult(string Id, string Title, string Url, string Thumbnail);

        // Public APIs

        public void Enqueue(QueueItem item)
        {
            _incomingQueue.Enqueue(item);
            _queueSignal.Release();
        }

        public Queue<QueueItem> GetQueue() => new Queue<QueueItem>(_queue);

        public QueueItem? GetNowPlaying() => nowPlaying;

        public void Skip()
        {
            _ffplayProcess?.Kill();
        }

        public static async Task<List<YouTubeSearchResult>> SearchAsync(string query)
        {
            var results = new List<YouTubeSearchResult>();

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

                    results.Add(new YouTubeSearchResult(id, title, url, thumbnail));
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
                Console.WriteLine($"Now playing: {nextItem.Title}");
                _ = PlayAsync(nextItem);
            }
            else
            {
                this.PlayRandom();
            }
        }
        private void PlayRandom()
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

            var item = new QueueItem
            {
                Id = Path.GetFileNameWithoutExtension(randomFile),
                Title = "Playing Random Song",
                Url = "",
                Thumbnail = "/assets/default-thumbnail.jpg"
            };

            Console.WriteLine($"Randomly selected cached song: {item.Id}");
            _ = PlayAsync(item);
        }

        private async Task PlayAsync(QueueItem item)
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
