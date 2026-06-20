using energy_backend.Application.Interfaces;
using energy_backend.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace energy_backend.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class OrganisationController(
    IOrganisationService orgService,
    ILiveDataOrchestratorService liveOrchestrator,
    IHistoricalDataOrchestratorService historicalOrchestrator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrganisationResponseDto>>> GetAllOrganisations()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized("Invalid User.");
        var orgs = await orgService.GetAllOrganisationsAsync(userId);
        return Ok(orgs ?? Enumerable.Empty<OrganisationResponseDto>());
    }

    [HttpPost]
    public async Task<ActionResult<OrganisationResponseDto>> CreateOrganisation(
        [FromBody] OrganisationRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Type))
            return BadRequest("Organisation name and type are required.");
        if (!TryGetUserId(out var userId)) return Unauthorized("Invalid User.");
        var org = await orgService.CreateOrganisationAsync(userId, request);
        return org is null
            ? BadRequest("Could not create organisation.")
            : CreatedAtAction(nameof(GetLiveSnapshot), new { organisationId = org.OrganisationId }, org);
    }

    [HttpPut("{organisationId:guid}")]
    public async Task<ActionResult<OrganisationResponseDto>> UpdateOrganisation(
        Guid organisationId, [FromBody] OrganisationRequestDto request)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized("Invalid User.");
        var updated = await orgService.UpdateOrganisationAsync(userId, organisationId, request);
        return updated is null ? NotFound("Organisation not found.") : Ok(updated);
    }

    [HttpDelete("{organisationId:guid}")]
    public async Task<IActionResult> DeleteOrganisation(Guid organisationId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized("Invalid User.");
        var deleted = await orgService.DeleteOrganisationAsync(userId, organisationId);
        return deleted ? NoContent() : NotFound("Organisation not found.");
    }

    [HttpGet("HasOrganisation")]
    public async Task<ActionResult<bool>> HasOrganisation()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized("Invalid User.");
        return Ok(await orgService.HasOrganisationAsync(userId));
    }



    // Page 1: seed the 30-minute live chart with raw readings, then subscribe to SignalR.
    [HttpGet("live/{organisationId:guid}")]
    public async Task<ActionResult<LiveSnapshotDto>> GetLiveSnapshot(Guid organisationId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized("Invalid User.");
        if (await orgService.GetByIdAsync(userId, organisationId) is null)
            return NotFound("Organisation not found.");
        var result = await liveOrchestrator.GetLiveSnapshotAsync(organisationId);
        return Ok(result);
    }

    // Page 2: preset-driven aggregate rollups (24h / 7d / 30d). EstimatedCost per row
    // uses the rate that was active at that timestamp.
    [HttpGet("historical/{organisationId:guid}")]
    public async Task<ActionResult<OrganisationAnalyticsDto>> GetHistoricalSnapshot(
        Guid organisationId,
        [FromQuery] string preset = "7d")
    {
        if (!TryGetUserId(out var userId)) return Unauthorized("Invalid User.");
        if (await orgService.GetByIdAsync(userId, organisationId) is null)
            return NotFound("Organisation not found.");
        var result = await historicalOrchestrator.GetHistoricalSnapshotAsync(organisationId, preset);
        return result is null ? NotFound("No historical data available.") : Ok(result);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out userId);
    }
}