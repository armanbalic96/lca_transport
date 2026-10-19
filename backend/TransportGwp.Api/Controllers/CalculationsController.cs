using System.Text;
using Microsoft.AspNetCore.Mvc;
using TransportGwp.Application.Interfaces;
using TransportGwp.Application.Models;
using TransportGwp.Domain.Models;

namespace TransportGwp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalculationsController : ControllerBase
    {
        private readonly ITransportCalculator _calculator;
        private readonly IExportService _exportService;

        public CalculationsController(ITransportCalculator calculator, IExportService exportService)
        {
            _calculator = calculator;
            _exportService = exportService;
        }

        /// <summary>Calculates cargo, validation, route impact, mode comparison, statistics and indicators for a wizard state.</summary>
        [HttpPost]
        public ActionResult<ScenarioResult> Calculate([FromBody] TransportState state)
            => Ok(_calculator.Calculate(state));

        /// <summary>Exports the scenario as "report" (plain text), "csv" or "json".</summary>
        [HttpPost("export/{format}")]
        public IActionResult Export(string format, [FromBody] TransportState state, [FromQuery] string? name)
        {
            var bundle = _exportService.Export(state, name);
            return format.ToLowerInvariant() switch
            {
                "report" => File(Encoding.UTF8.GetBytes(bundle.Report), "text/plain; charset=utf-8", "transport-scenario.txt"),
                "csv" => File(Encoding.UTF8.GetBytes(bundle.Csv), "text/csv; charset=utf-8", "transport-scenario.csv"),
                "json" => File(Encoding.UTF8.GetBytes(bundle.Json), "application/json", "transport-scenario.json"),
                _ => BadRequest($"Unknown export format '{format}'. Use report, csv or json."),
            };
        }
    }
}
