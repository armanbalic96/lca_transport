using Microsoft.AspNetCore.Mvc;
using TransportGwp.Application.Interfaces;
using TransportGwp.Application.Models;
using TransportGwp.Domain.Models;

namespace TransportGwp.Api.Controllers
{
    /// <summary>The transport register: saved transport calculations.</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TransportsController : ControllerBase
    {
        private readonly ITransportRegisterService _registerService;

        public TransportsController(ITransportRegisterService registerService)
            => _registerService = registerService;

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<TransportListItem>>> List(CancellationToken ct)
            => Ok(await _registerService.List(ct));

        [HttpGet("{id}")]
        public async Task<ActionResult<TransportDetail>> Get(string id, CancellationToken ct)
        {
            var transport = await _registerService.Get(id, ct);
            return transport is null ? NotFound() : Ok(transport);
        }

        [HttpPost]
        public async Task<ActionResult<TransportDetail>> Create([FromBody] CreateTransportRequest request, CancellationToken ct)
        {
            var created = await _registerService.Create(request.Name, ct);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        /// <summary>Autosave of the wizard state. Returns the recomputed register summary.</summary>
        [HttpPut("{id}/state")]
        public async Task<ActionResult<TransportListItem>> SaveState(string id, [FromBody] TransportState state, CancellationToken ct)
        {
            var saved = await _registerService.SaveState(id, state, ct);
            return saved is null ? NotFound() : Ok(saved);
        }

        [HttpPost("{id}/duplicate")]
        public async Task<ActionResult<TransportListItem>> Duplicate(string id, CancellationToken ct)
        {
            var copy = await _registerService.Duplicate(id, ct);
            return copy is null ? NotFound() : Ok(copy);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken ct)
            => await _registerService.Delete(id, ct) ? NoContent() : NotFound();
    }
}
