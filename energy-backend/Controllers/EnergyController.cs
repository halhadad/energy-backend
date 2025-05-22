using energy_backend.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnergyController(EnergyDbContext context) : ControllerBase
    {
        //some logic later
        private readonly EnergyDbContext _context = context;

        [HttpGet]
        public async Task<ActionResult<Energy>> GetEnergy()
        {
            // Simulate fetching energy data
            var energyData = await _context.Energies.FirstOrDefaultAsync();
            return Ok(energyData);
        }
    }
}
