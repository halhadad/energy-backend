using System.Security.Claims;
using energy_backend.Entities;
using energy_backend.Models;
using energy_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace energy_backend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class OrganisationController(IOrganisationService orgService) : ControllerBase
    {
        // Retrieves all organisations for the authenticated user.
        [HttpGet]
        public async Task<ActionResult<List<Organisation>>> GetAllOrganisations()
        {
            if (!TryGetUserId(out Guid userId))
                return Unauthorized("Invalid User.");

            var orgs = await orgService.GetAllOrganisationsAsync(userId);
            return orgs is null ? BadRequest("Error fetching organisations") : Ok(orgs);
        }

        // Creates a new organisation for the authenticated user.

        [HttpPost]
        public async Task<ActionResult<OrganisationResponseDto>> CreateOrganisation([FromBody] OrganisationRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Type))
                return BadRequest("Organisation name and type are required.");

            if (!TryGetUserId(out Guid userId))
                return Unauthorized("Invalid User.");

            var org = await orgService.CreateOrganisationAsync(userId, request);
            return org is null ? BadRequest("Error creating organisation") : Ok(org);
        }


        // Updates an existing organisation for the authenticated user.
        [HttpPut("{organisationId}")]
        public async Task<ActionResult<OrganisationResponseDto>> UpdateOrganisation(Guid organisationId, [FromBody] OrganisationRequestDto request)
        {
            if (request == null || organisationId == Guid.Empty)
                return BadRequest("Invalid organisation data.");

            if (!TryGetUserId(out Guid userId))
                return Unauthorized("Invalid User.");

            var updatedOrg = await orgService.UpdateOrganisationAsync(userId, organisationId, request);
            return updatedOrg is null ? NotFound("Organisation not found") : Ok(updatedOrg);
        }

        // Deletes an organisation owned by the authenticated user.

        [HttpDelete("{organisationId}")]
        public async Task<ActionResult<bool>> DeleteOrganisation(Guid organisationId)
        {
            if (organisationId == Guid.Empty)
                return BadRequest("Invalid organisation ID.");

            if (!TryGetUserId(out Guid userId))
                return Unauthorized("Invalid User.");

            var deleted = await orgService.DeleteOrganisationAsync(userId, organisationId);
            return deleted ? Ok(true) : NotFound("Organisation not found");
        }

        [HttpGet("HasOrganisation")]
        public async Task<ActionResult<bool>> HasOrganisation()
        {
            if (!TryGetUserId(out Guid userId))
                return Unauthorized("Invalid User.");

            try
            {
                var hasOrganisation = await orgService.HasOrganisationAsync(userId);
                return Ok(hasOrganisation);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }

        // Tries to extract the user ID from the JWT claims.
        
        private bool TryGetUserId(out Guid userId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaim, out userId);
        }
    }
}
