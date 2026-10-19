using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TransportGwp.Application.Interfaces;
using TransportGwp.Infrastructure.Geo;

namespace TransportGwp.Api.Controllers
{
    /// <summary>Client configuration: the referrer-restricted Maps JS key and which optional features are enabled.</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigController : ControllerBase
    {
        private readonly GoogleMapsOptions _maps;
        private readonly IAiAdvisor _advisor;

        public ConfigController(IOptions<GoogleMapsOptions> maps, IAiAdvisor advisor)
        {
            _maps = maps.Value;
            _advisor = advisor;
        }

        [HttpGet]
        public IActionResult Get() => Ok(new
        {
            mapsBrowserApiKey = string.IsNullOrWhiteSpace(_maps.BrowserApiKey) ? null : _maps.BrowserApiKey,
            roadRoutingAvailable = !string.IsNullOrWhiteSpace(_maps.ServerApiKey),
            aiAvailable = _advisor.IsAvailable,
        });
    }
}
