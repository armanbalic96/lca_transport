using Microsoft.AspNetCore.Mvc;
using TransportGwp.Application.Interfaces;
using TransportGwp.Application.Models;

namespace TransportGwp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiController : ControllerBase
    {
        private readonly IAiAdvisor _advisor;

        public AiController(IAiAdvisor advisor)
            => _advisor = advisor;

        [HttpPost("recommendations")]
        public async Task<ActionResult<AiRecommendation>> Recommend([FromBody] AiRecommendRequest request, CancellationToken ct)
            => Ok(await _advisor.Recommend(request.State, ct));

        [HttpPost("explain")]
        public async Task<ActionResult<AiExplainResponse>> Explain([FromBody] AiExplainRequest request, CancellationToken ct)
            => Ok(new AiExplainResponse(await _advisor.Explain(request.Question, request.State, ct)));
    }
}
