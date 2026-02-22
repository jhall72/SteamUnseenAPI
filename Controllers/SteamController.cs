using Microsoft.AspNetCore.Mvc;
using SteamWebAPI2.Utilities;
using SteamWebAPI2.Interfaces;

namespace SteamUnseenAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class SteamController : ControllerBase
{

    private readonly SteamWebInterfaceFactory _steamFactory;
    private readonly IConfiguration _configuration;

    public SteamController(SteamWebInterfaceFactory steamFactory, IConfiguration configuration)
    {
        _steamFactory = steamFactory;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetDelistedGames(string profileUrl)
    {
        var steamUser = _steamFactory.CreateSteamWebInterface<SteamUser>();
        var playerService = _steamFactory.CreateSteamWebInterface<PlayerService>();

        var lastSegment = profileUrl.TrimEnd('/').Split('/').Last();
        ulong steamId;

        if (!(ulong.TryParse(lastSegment, out steamId)))
        {
            var vanityResponse = await steamUser.ResolveVanityUrlAsync(lastSegment);
            steamId = vanityResponse.Data;
        }

        var gamesResponse = await playerService.GetOwnedGamesAsync(steamId, includeAppInfo: true);
        var ownedGames = gamesResponse.Data.OwnedGames.ToList();

        var httpClient = new HttpClient();
        var availableAppIds = new HashSet<uint>();
        uint lastAppId = 0;
        var steamApiKey = _configuration["SteamApiKey"];

        while (true)
        {
            var url = $"https://api.steampowered.com/IStoreService/GetAppList/v1/?key={steamApiKey}&max_results=50000&last_appid={lastAppId}&include_games=true&include_dlc=true&include_software=true&include_videos=true&include_hardware=true";
            var json = await httpClient.GetStringAsync(url);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var response = doc.RootElement.GetProperty("response");

            if (!response.TryGetProperty("apps", out var apps))
            {
                break;
            }

            foreach (var app in apps.EnumerateArray())
            {
                availableAppIds.Add(app.GetProperty("appid").GetUInt32());
            }

            if (!response.TryGetProperty("last_appid", out var lastId))
            {
                break;
            }

            lastAppId = lastId.GetUInt32();
        }

        var delistedGames = ownedGames
            .Where(g => !availableAppIds.Contains((uint)g.AppId))
            .OrderBy(g => g.Name)
            .Select(g => new
            {
                g.Name,
                g.AppId,
                PlaytimeMinutes = g.PlaytimeForever.TotalMinutes,
                ImageUrl = $"https://cdn.akamai.steamstatic.com/steam/apps/{g.AppId}/header.jpg"
            })
            .ToList();

        return Ok(delistedGames);
    }
}