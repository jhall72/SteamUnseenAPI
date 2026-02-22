namespace SteamUnseenAPI.Library;
public class Response
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string> GameNames { get; set; } = new List<string>();
}