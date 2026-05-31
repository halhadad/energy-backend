using energy_backend.Application.Interfaces;
using energy_backend.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace energy_backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EnergyController(ISnapshotService aggregationService) : ControllerBase
{
    [Authorize]
    [HttpPost("snapshot")]
    public async Task<ActionResult<AggregationResultDto>> GetSnapshot([FromBody] AggregationRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = await aggregationService.GetSnapshotAsync(request);
            return Ok(result);
        }
        catch (Exception)
        {
            return StatusCode(500, "Error retrieving snapshot data.");
        }
    }
}
