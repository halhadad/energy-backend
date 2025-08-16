using energy_backend.Application.Models;
using energy_backend.Core.Interfaces;
using energy_backend.Data;
using energy_backend.Entities;
using energy_backend.Infrastructure.Services;
using energy_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Services
{
    public class OrganisationService(IOrganisationRepository repository, OrganisationAnalyticsService orgAn) : IOrganisationService
    {
        public async Task<IEnumerable<OrganisationResponseDto>?> GetAllOrganisationsAsync(Guid userId)
        {
            var organisations = await repository.GetAllByUserIdAsync(userId);

            return organisations.Select(o => new OrganisationResponseDto
            {
                Name = o.Name,
                Type = o.Type,
                EnergyBudget = o.EnergyBudget,
                OrganisationId = o.OrganisationId,
                DeviceCount = o.Devices?.Count ?? 0
            });
        }

        public async Task<OrganisationResponseDto?> CreateOrganisationAsync(Guid userId, OrganisationRequestDto request)
        {
            var organisation = new Organisation
            {
                OrganisationId = Guid.NewGuid(),
                Name = request.Name,
                Type = request.Type,
                UserId = userId,
                EnergyBudget = request.EnergyBudget,
            };

            await repository.AddAsync(organisation);
            await repository.SaveChangesAsync();

            return new OrganisationResponseDto
            {
                OrganisationId = organisation.OrganisationId,
                Name = organisation.Name,
                Type = organisation.Type,
                EnergyBudget = organisation.EnergyBudget,
                DeviceCount = 0
            };
        }

        public async Task<OrganisationResponseDto?> UpdateOrganisationAsync(Guid userId, Guid organisationId, OrganisationRequestDto request)
        {
            var organisation = await repository.GetByIdAsync(userId, organisationId);
            if (organisation is null) return null;

            organisation.Name = request.Name;
            organisation.Type = request.Type;
            organisation.EnergyBudget = request.EnergyBudget;

            await repository.SaveChangesAsync();

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
            var organisation = await repository.GetByIdAsync(userId, organisationId);
            if (organisation is null) return false;

            await repository.DeleteAsync(organisation);
            await repository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasOrganisationAsync(Guid userId)
        {
            return await repository.ExistsAsync(userId);
        }

        public async Task<OrganisationAnalyticsDto?> GetOrganisationAnalyticsAsync(Guid userId, Guid organisationId)
        {
            var organisation = await repository.GetByIdAsync(userId, organisationId);
            if (organisation is null) return null;
            var data = await orgAn.GetOrganisationAnalyticsAsync(organisationId);
            return data;
        }
    }
}
