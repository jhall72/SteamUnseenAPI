using Microsoft.AspNetCore.Mvc;

namespace SteamUnseenAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class UnlistedInLibraryController : ControllerBase
{
    [HttpGet(Name = "GetUnlistedInLibrary")]
    public IEnumerable<WeatherForecast> Get()
    {
    }
}
