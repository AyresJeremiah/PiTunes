using backend.Hubs;
using backend.Services;
using backend.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Wire services (only what you need)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS (keep this for your Angular frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder
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


// Add DbContext with Postgres
builder.Services.AddDbContext<PiTunesDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<YouTubeItemResult>();
builder.Services.AddScoped<IQueueItemResult, QueueItemResult>();
builder.Services.AddSingleton<SongHubService>();
builder.Services.AddSingleton<YouTubeService>();

builder.Services.AddSignalR();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.MapHub<SocketHub>("api/hubs/socket");

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

app.MapControllers();

app.Run();