using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using energy_backend.Application.Models; // For AggregationRequestDto and AggregationResultDto
using energy_backend.Application.Services; // For IAggregationService
using Microsoft.AspNetCore.Authorization; // Assuming authorization is needed

namespace energy_backend.Controllers
{
    [Authorize] // Apply authorization as per existing controllers
    [ApiController]
    [Route("api/[controller]")] // Renamed to SnapshotController
    public class SnapshotController : ControllerBase
    {
        private readonly IAggregationService _aggregationService;

        public SnapshotController(IAggregationService aggregationService)
        {
            _aggregationService = aggregationService;
        }

        [HttpPost]
        public async Task<ActionResult<AggregationResultDto>> GetAggregatedData([FromBody] AggregationRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _aggregationService.GetAggregatedDataAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the exception (using a logger injected into the controller)
                // _logger.LogError(ex, "Error while getting aggregated data");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
    }
}
