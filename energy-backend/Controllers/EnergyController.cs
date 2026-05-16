using energy_backend.Application.Models;
using energy_backend.Application.Services;
using energy_backend.Data;
using energy_backend.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnergyController : ControllerBase
    {
        private readonly EnergyDbContext _context;
        private readonly IAggregationService _aggregationService;

        public EnergyController(EnergyDbContext context, IAggregationService aggregationService)
        {
            _context = context;
            _aggregationService = aggregationService;
        }

        // Legacy endpoint — kept for compatibility
        [HttpGet]
        public async Task<ActionResult<Energy>> GetEnergy()
        {
            var energyData = await _context.Energies.FirstOrDefaultAsync();
            return Ok(energyData);
        }

        // Snapshot endpoint — call this on dashboard load, then use SignalR for live updates
        // POST because the request body carries orgId, range, and time bounds
        [Authorize]
        [HttpPost("snapshot")]
        public async Task<ActionResult<AggregationResultDto>> GetSnapshot([FromBody] AggregationRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _aggregationService.GetSnapshotAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error retrieving snapshot data.");
            }
        }
    }
}