using Microsoft.AspNetCore.Mvc;
using Ref = TransportGwp.Domain.ReferenceData.TransportReferenceData;

namespace TransportGwp.Api.Controllers
{
    /// <summary>Static lookup data the UI needs to render selectors (modes, vehicles, fuels, materials, templates).</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ReferenceDataController : ControllerBase
    {
        [HttpGet]
        [ResponseCache(Duration = 3600)]
        public IActionResult Get() => Ok(new
        {
            defaultSource = Ref.DefaultSource,
            transportModes = Ref.TransportModes,
            emissionFactorSources = Ref.EmissionFactorSources.Values,
            defaultLoad = Ref.DefaultLoad,
            vehicleOptions = Ref.VehicleOptions,
            fuelOptions = Ref.FuelOptions,
            modeFuelKind = Ref.ModeFuelKind,
            materials = Ref.Materials,
            massUnits = Ref.MassUnits,
            legTemplates = Ref.LegTemplates,
            routingFactors = Ref.TransportModes.ToDictionary(m => m, Ref.RoutingFactor),
            roadModes = Ref.TransportModes.Where(Ref.IsRoadMode),
        });
    }
}
