using System.Security.Claims;
using energy_backend.Application.Interfaces;
using energy_backend.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace energy_backend.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class OrganisationController(
    IOrganisationService orgService,
    // ADDED: analytics injected here directly — OrganisationService no longer calls
    // IEnergyAnalyticsOrchestratorService internally (cross-service dependency removed).
    IEnergyAnalyticsOrchestratorService analyticsOrchestrator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrganisationResponseDto>>> GetAllOrganisations()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized("Invalid User.");

        var orgs = await orgService.GetAllOrganisationsAsync(userId);
        // CHANGED: null → empty list, not BadRequest. No organisations is a valid
        // state, not an error.
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
            : CreatedAtAction(nameof(GetOrganisationAnalytics), new { organisationId = org.OrganisationId }, org);
    }

    [HttpPut("{organisationId:guid}")]
    public async Task<ActionResult<OrganisationResponseDto>> UpdateOrganisation(
        Guid organisationId, [FromBody] OrganisationRequestDto request)
    {
        if (organisationId == Guid.Empty) return BadRequest("Invalid organisation ID.");
        if (!TryGetUserId(out var userId)) return Unauthorized("Invalid User.");

        var updated = await orgService.UpdateOrganisationAsync(userId, organisationId, request);
        return updated is null ? NotFound("Organisation not found.") : Ok(updated);
    }

    [HttpDelete("{organisationId:guid}")]
    public async Task<IActionResult> DeleteOrganisation(Guid organisationId)
    {
        if (organisationId == Guid.Empty) return BadRequest("Invalid organisation ID.");
        if (!TryGetUserId(out var userId)) return Unauthorized("Invalid User.");

        var deleted = await orgService.DeleteOrganisationAsync(userId, organisationId);
        // CHANGED: was ActionResult<bool> returning Ok(true) — DELETE should return
        // 204 No Content on success, not a bool payload.
        return deleted ? NoContent() : NotFound("Organisation not found.");
    }

    [HttpGet("HasOrganisation")]
    public async Task<ActionResult<bool>> HasOrganisation()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized("Invalid User.");
        return Ok(await orgService.HasOrganisationAsync(userId));
    }

    [HttpGet("GetOrganisationAnalytics/{organisationId:guid}")]
    public async Task<ActionResult<OrganisationAnalyticsDto>> GetOrganisationAnalytics(
        Guid organisationId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized("Invalid User.");

        // Ownership check — verify this org belongs to the calling user before
        // returning any analytics data.
        var org = await orgService.GetByIdAsync(userId, organisationId);
        if (org is null) return NotFound("Organisation not found.");

        // CHANGED: was orgService.GetOrganisationAnalyticsAsync — that made
        // OrganisationService call IEnergyAnalyticsOrchestratorService internally.
        // Controller now calls the two services independently.
        var analytics = await analyticsOrchestrator.GetOrganisationAnalyticsAsync(organisationId);
        return analytics is null
            ? NotFound("No analytics data available.")
            : Ok(analytics);
        // REMOVED: bare try/catch swallowing all exceptions into BadRequest.
        // Let the global exception handler deal with unexpected errors — hiding
        // them here makes bugs invisible.
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out userId);
    }
}