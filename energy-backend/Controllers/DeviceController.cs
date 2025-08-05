using System.Security.Claims;
using energy_backend.Models;
using energy_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace energy_backend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceController(IDeviceService deviceService) : ControllerBase
    {
        private Guid? GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaim, out var id) ? id : null;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Invalid user.");
            var devices = await deviceService.GetDevicesAsync(userId.Value);
            return Ok(devices);
        }

        [HttpGet("byOrganisation/{orgId}")]
        public async Task<IActionResult> GetByOrganisationId(Guid orgId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Invalid user.");

            var devices = await deviceService.GetDevicesByOrganisationIdAsync(userId.Value, orgId);
            return Ok(devices);
        }


        [HttpGet("{deviceId}")]
        public async Task<IActionResult> GetById(Guid deviceId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Invalid user.");
            var device = await deviceService.GetDeviceByIdAsync(userId.Value, deviceId);
            if (device == null) return NotFound();
            return Ok(device);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DeviceRequestDto request)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Invalid user.");
            var device = await deviceService.CreateDeviceAsync(userId.Value, request);
            if (device == null) return BadRequest("Invalid organisation or data.");
            return Ok(device);
        }

        [HttpPut("{deviceId}")]
        public async Task<IActionResult> Update(Guid deviceId, [FromBody] DeviceRequestDto request)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Invalid user.");
            var device = await deviceService.UpdateDeviceAsync(userId.Value, deviceId, request);
            if (device == null) return NotFound();
            return Ok(device);
        }

        [HttpDelete("{deviceId}")]
        public async Task<IActionResult> Delete(Guid deviceId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Invalid user.");
            var success = await deviceService.DeleteDeviceAsync(userId.Value, deviceId);
            return success ? Ok() : NotFound();
        }
    }
}