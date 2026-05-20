using energy_backend.Core.Entities;
using energy_backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace energy_backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly EnergyDbContext _context;

        public SettingsController(EnergyDbContext context)
        {
            _context = context;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var userId = GetUserId();
            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.UserId == userId);
            if (setting == null) return NotFound();

            return Ok(new SettingResponseDto
            {
                ElectricityCostPerKwh = setting.ElectricityCostPerKwh,
                PeakAlerts = setting.PeakAlerts,
                UnusualAlerts = setting.UnusualAlerts,
                BudgetAlerts = setting.BudgetAlerts,
                RequireEmail = setting.RequireEmail,
                FavoriteOrg = setting.FavoriteOrg
            });
        }

        [HttpPatch]
        public async Task<IActionResult> Update([FromBody] UpdateSettingDto dto)
        {
            var userId = GetUserId();
            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.UserId == userId);
            if (setting == null) return NotFound();

            if (dto.ElectricityCostPerKwh.HasValue)
            {
                if (dto.ElectricityCostPerKwh.Value < 0)
                    return BadRequest("ElectricityCostPerKwh must be non-negative.");
                setting.ElectricityCostPerKwh = dto.ElectricityCostPerKwh.Value;
            }

            if (dto.PeakAlerts.HasValue) setting.PeakAlerts = dto.PeakAlerts.Value;
            if (dto.UnusualAlerts.HasValue) setting.UnusualAlerts = dto.UnusualAlerts.Value;
            if (dto.BudgetAlerts.HasValue) setting.BudgetAlerts = dto.BudgetAlerts.Value;
            if (dto.RequireEmail.HasValue) setting.RequireEmail = dto.RequireEmail.Value;
            if (dto.FavoriteOrg.HasValue) setting.FavoriteOrg = dto.FavoriteOrg.Value;

            await _context.SaveChangesAsync();

            return Ok(new SettingResponseDto
            {
                ElectricityCostPerKwh = setting.ElectricityCostPerKwh,
                PeakAlerts = setting.PeakAlerts,
                UnusualAlerts = setting.UnusualAlerts,
                BudgetAlerts = setting.BudgetAlerts,
                RequireEmail = setting.RequireEmail,
                FavoriteOrg = setting.FavoriteOrg
            });
        }
    }

    public class SettingResponseDto
    {
        public float ElectricityCostPerKwh { get; set; }
        public bool PeakAlerts { get; set; }
        public bool UnusualAlerts { get; set; }
        public bool BudgetAlerts { get; set; }
        public bool RequireEmail { get; set; }
        public Guid FavoriteOrg { get; set; }
    }

    public class UpdateSettingDto
    {
        public float? ElectricityCostPerKwh { get; set; }
        public bool? PeakAlerts { get; set; }
        public bool? UnusualAlerts { get; set; }
        public bool? BudgetAlerts { get; set; }
        public bool? RequireEmail { get; set; }
        public Guid? FavoriteOrg { get; set; }
    }
}