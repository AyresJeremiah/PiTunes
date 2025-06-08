using System.Text.Json.Serialization;

namespace backend.Services;

public class AiSuggestionService
{
    private readonly HttpClient _httpClient;
    private const string OllamaEndpoint = "http://localhost:11434/api/generate";
    private const string Model = "gemma3:4b"; // Or whatever model you are using
    private const int NumSongs = 15; // Number of song suggestions to return

    public AiSuggestionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<SongSuggestion>> GetSuggestionsAsync(string userPrompt)
    {
        var fullPrompt = BuildSystemPrompt(userPrompt);

        var request = new
        {
            model = Model,
            prompt = fullPrompt,
            stream = false
        };

        var response = await _httpClient.PostAsJsonAsync(OllamaEndpoint, request);
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
For each song, give the title and artist in the following format:

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