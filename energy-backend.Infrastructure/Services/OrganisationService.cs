using energy_backend.Data;
using energy_backend.Entities;
using energy_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Services
{
    public class OrganisationService(EnergyDbContext context) : IOrganisationService
    {
        public async Task<IEnumerable<OrganisationResponseDto>?> GetAllOrganisationsAsync(Guid requestUserId)
        {
            // Fetch all organisations from the database
            var organisations = await context.Organisations
                .Where(o => o.UserId == requestUserId)
                .Include(o => o.Devices)
                .ToListAsync();
            // Map the entities to DTOs
            var organisationDtos = organisations
                .Where(o => o.UserId == requestUserId)
                .Select(o => new OrganisationResponseDto
                {
                    Name = o.Name,
                    Type = o.Type,
                    EnergyBudget = o.EnergyBudget,
                    OrganisationId = o.OrganisationId,
                    DeviceCount = o.Devices?.Count ?? 0
                }).ToList();
            // Return the list of organisation DTOs
            return organisationDtos;
        }


        public async Task<OrganisationResponseDto?> CreateOrganisationAsync(Guid userId, OrganisationRequestDto request)
        {
            // Create a new organisation entity
            var organisation = new Organisation
            {
                OrganisationId = Guid.NewGuid(),
                Name = request.Name,
                Type = request.Type,
                UserId = userId,
                EnergyBudget = request.EnergyBudget,
            };
            // Add the organisation to the database
            context.Organisations.Add(organisation);
            await context.SaveChangesAsync();
            // Map the entity to a DTO and return it
            return new OrganisationResponseDto
            {
                OrganisationId = organisation.OrganisationId,
                Name = organisation.Name,
                Type = organisation.Type,
                EnergyBudget = organisation.EnergyBudget,
                DeviceCount = organisation.Devices?.Count ?? 0
            };
        }

        public async Task<OrganisationResponseDto?> UpdateOrganisationAsync(Guid userId, Guid organisationId, OrganisationRequestDto request)
        {
            // Find the organisation by ID
            var organisation = await context.Organisations
                .Include(o => o.Devices) 
                .FirstOrDefaultAsync(o => o.OrganisationId == organisationId && o.UserId == userId);
            if (organisation == null)
            {
                return null; // Organisation not found or doesn't belong to the user
            }
            // Update the organisation properties
            organisation.Name = request.Name;
            organisation.Type = request.Type;
            organisation.EnergyBudget = request.EnergyBudget;
            // Save changes to the database
            await context.SaveChangesAsync();
            // Map the updated entity to a DTO and return it
            return new OrganisationResponseDto
            {
                OrganisationId = organisation.OrganisationId,
                Name = organisation.Name,
                Type = organisation.Type,
                EnergyBudget = organisation.EnergyBudget,
                DeviceCount = organisation.Devices?.Count ?? 0
            };
        }

        public async Task<bool> DeleteOrganisationAsync(Guid userId, Guid organisationId)
        {
            // Find the organisation by ID
            var organisation = await context.Organisations
                .FirstOrDefaultAsync(o => o.OrganisationId == organisationId && o.UserId == userId);
            if (organisation == null)
            {
                return false; // Organisation not found or does not belong to the user
            }
            // Remove the organisation from the database
            context.Organisations.Remove(organisation);
            await context.SaveChangesAsync();
            return true; // Organisation successfully deleted

        }

        public async Task<bool> HasOrganisationAsync(Guid userId)
        {
            return await context.Organisations
                .AnyAsync(o => o.UserId == userId);
        }
    }
}
