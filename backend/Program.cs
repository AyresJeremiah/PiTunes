using backend.Hubs;
using backend.Services;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

// Wire services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS – keep this for dev when Angular runs on 4200
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200",
                "http://frontend"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// DbContext with Postgres
builder.Services.AddDbContext<PiTunesDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<OllamaSettings>(builder.Configuration.GetSection("Ollama"));

builder.Services.AddScoped<YouTubeItemResult>();
builder.Services.AddScoped<IQueueItemResult, QueueItemResult>();
builder.Services.AddSingleton<SongHubService>();
builder.Services.AddSingleton<YouTubeService>();
builder.Services.AddHttpClient<AiSuggestionService>();

builder.Services.Configure<FeatureOptions>(
    builder.Configuration.GetSection("Features"));

builder.Services.AddSignalR();

// Use a consistent URL in all environments (optional, but nice)
builder.WebHost.UseUrls("http://localhost:5219");

var app = builder.Build();

// ----- MIDDLEWARE PIPELINE -----

app.UseHttpsRedirection();

// Serve Angular from wwwroot
app.UseStaticFiles();

app.UseRouting();

// CORS (mainly needed when Angular runs on 4200 in dev)
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Run migrations automatically on startup
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PiTunesDbContext>();

    var retryCount = 5;
    while (retryCount > 0)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch (Exception ex)
        {
            retryCount--;
            Console.WriteLine($"Database migration failed: {ex.Message}. Retrying...");
            Thread.Sleep(2000);
        }
    }
}

// Map endpoints
app.MapControllers();
app.MapHub<SocketHub>("/api/hubs/socket");

// For Angular SPA: anything not matching API/Hub goes to index.html
app.MapFallbackToFile("index.html");

// Optional: auto-open browser on Windows
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    var url = "http://localhost:5219";
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    catch
    {
        // ignore
    }
}

app.Run();
