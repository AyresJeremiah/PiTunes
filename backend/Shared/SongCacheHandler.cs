using System.Diagnostics;
using backend.Models;

namespace backend.Shared;

public static class SongCacheHandler
{
    private const string CacheDir = "song_cache";

#if RELEASE
    public const string YtDlpPath = "/usr/local/bin/yt-dlp";
    private const string FfplayPath = "/usr/bin/ffplay";
#else
    public const string YtDlpPath = "yt-dlp";
    private const string FfplayPath = "ffplay";
#endif

    public static void CreateCacheDirectory()
    {
        if (!Directory.Exists(CacheDir))
        {
            Directory.CreateDirectory(CacheDir);
            Console.WriteLine($"Cache directory created: {CacheDir}");
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

    public static async Task<string> DownloadAndCacheAsync(string videoId)
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

    public static Task DeleteCachedSongAsync(string videoId)
    {
        var filePath = Path.Combine(CacheDir, $"{videoId}.mp3");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Cached file not found: {filePath}");
        }

        try
        {
            File.Delete(filePath);
            Console.WriteLine($"Deleted cached file: {filePath}");
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to delete cached file: {filePath}", ex);
        }

        return Task.CompletedTask;
    }

    public static async Task<Process> StartPlayback(YouTubeItem item)
    {
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

        
        // var ffplay = new ProcessStartInfo
        // {
        //     FileName = FfplayPath,
        //     Arguments = $"-nodisp -autoexit \"{filePath}\"",
        //     RedirectStandardInput = false,
        //     UseShellExecute = false,
        //     CreateNoWindow = true
        // };

        var ffplayProcess = Process.Start(ffplay);

        if (ffplayProcess == null)
        {
            throw new Exception("Failed to start ffplay process.");
        }

        return ffplayProcess;
    }
}