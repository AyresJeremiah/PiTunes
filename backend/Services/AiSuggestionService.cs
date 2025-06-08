using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace backend.Services;

public class AiSuggestionService
{
    private readonly HttpClient _httpClient;
    private string _ollamaEndpoint;
    private string _model = "gemma3:4b"; 
    private const int NumSongs = 15; 

    public AiSuggestionService(HttpClient httpClient, IOptions<OllamaSettings> options)
    {
        _ollamaEndpoint = options.Value.Endpoint ?? "http://localhost:11434/api/generate";// Default endpoint
        _model = options.Value.Model ?? "gemma3:4b"; // Default model
        _httpClient = httpClient;
    }

    public async Task<List<SongSuggestion>> GetSuggestionsAsync(string userPrompt)
    {
        var fullPrompt = BuildSystemPrompt(userPrompt);

        var request = new
        {
            model = _model,
            prompt = fullPrompt,
            stream = false
        };

        var response = await _httpClient.PostAsJsonAsync(_ollamaEndpoint, request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();

        // Parse Ollama response into a list of song suggestions
        return ParseSuggestions(result?.Response);
    }

    private string BuildSystemPrompt(string userPrompt)
    {
        return @$"
You are a music recommendation engine. 
The user will give you a description or mood, and you will return a list of {NumSongs} song suggestions.
For each song, give the title and artist in the following format without numbering:

Title - Artist

User prompt: {userPrompt}
";
    }

    private List<SongSuggestion> ParseSuggestions(string response)
    {
        var suggestions = new List<SongSuggestion>();

        if (string.IsNullOrWhiteSpace(response)) return suggestions;

        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split(" - ", 2);
            if (parts.Length == 2)
            {
                suggestions.Add(new SongSuggestion
                {
                    Title = parts[0].Trim(),
                    Artist = parts[1].Trim()
                });
            }
        }

        return suggestions;
    }

    private class OllamaResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; set; }
    }
}

public class SongSuggestion
{
    public string Title { get; set; }
    public string Artist { get; set; }
}