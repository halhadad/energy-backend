
using System.Security.Claims;
using energy_backend.Application.Models;
using energy_backend.Application.Interfaces;
using energy_backend.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace energy_backend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AlertsController(IAlertService alertService) : ControllerBase
    {
        [HttpGet("events")]
        public async Task<ActionResult<IEnumerable<AlertEventDto>>> GetAlertEvents()
        {
            if (!TryGetUserId(out Guid userId))
                return Unauthorized("Invalid User.");

            var events = await alertService.GetRecentEventsAsync(userId);
            return Ok(events);
        }

        [HttpGet]
        public async Task<ActionResult<List<AlertResponseDto>>> GetActiveAlerts()
        {
            if (!TryGetUserId(out Guid userId))
                return Unauthorized("Invalid User.");

            var alerts = await alertService.GetActiveAlertsAsync(userId);
            return alerts is null ? BadRequest("Error fetching alerts") : Ok(alerts);
        }

        // Creates a new alert for the authenticated user

        [HttpPost]
        public async Task<ActionResult<AlertResponseDto>> CreateAlert([FromBody] AlertRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name) || (request.Threshold) == 0)
                return BadRequest("Alert name and threshold are required.");

            if (!TryGetUserId(out Guid userId))
                return Unauthorized("Invalid User.");

            var org = await alertService.CreateAlertAsync(userId, request);
            return org is null ? BadRequest("Error creating organisation") : Ok(org);
        }

        // Manually resolves (acknowledges) a triggered alert.

        [HttpPost("{alertId}/resolve")]
        public async Task<ActionResult<bool>> ResolveAlert(Guid alertId)
        {
            if (alertId == Guid.Empty)
                return BadRequest("Invalid Alert ID.");

            if (!TryGetUserId(out Guid userId))
                return Unauthorized("Invalid User.");

            var resolved = await alertService.ResolveAlertAsync(userId, alertId);
            return resolved ? Ok(true) : NotFound("Alert not found");
        }

        // Deletes an alert by the authenticated user.

        [HttpDelete("{alertId}")]
        public async Task<ActionResult<bool>> DeleteAlert(Guid alertId)
        {
            if (alertId == Guid.Empty)
                return BadRequest("Invalid Alert ID.");

            if (!TryGetUserId(out Guid userId))
                return Unauthorized("Invalid User.");

            var deleted = await alertService.DeleteAlertAsync(userId, alertId);
            return deleted ? Ok(true) : NotFound("Alert not found");
        }

        // Tries to extract the user ID from the JWT claims.

        private bool TryGetUserId(out Guid userId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaim, out userId);
        }
    }
}
