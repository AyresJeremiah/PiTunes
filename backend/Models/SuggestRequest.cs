namespace backend.Models;

public class SuggestRequest(string prompt)
{
    public string Prompt { get; set; } = prompt;
}