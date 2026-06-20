using energy_backend.Application.Configuration;
using energy_backend.Application.Interfaces;
using energy_backend.Application.Models;
using energy_backend.Core.Entities;
using energy_backend.Core.Interfaces;

namespace energy_backend.Application.Services;

public class OrganisationService(
    IOrganisationRepository repository,
    IEnergyRateRepository rateRepo,
    EnergySettings settings) : IOrganisationService
{
    public async Task<IEnumerable<OrganisationResponseDto>?> GetAllOrganisationsAsync(Guid userId)
    {
        var organisations = await repository.GetAllByUserIdAsync(userId);
        var result = new List<OrganisationResponseDto>();
        foreach (var org in organisations)
        {
            var dto = MapToDto(org);
            dto.CostPerKwh = await rateRepo.GetCurrentRateAsync(org.OrganisationId);
            result.Add(dto);
        }
        return result;
    }

    public async Task<OrganisationResponseDto?> GetByIdAsync(Guid userId, Guid organisationId)
    {
        var org = await repository.GetByIdAsync(userId, organisationId);
        if (org is null) return null;
        var dto = MapToDto(org);
        dto.CostPerKwh = await rateRepo.GetCurrentRateAsync(org.OrganisationId);
        return dto;
    }

    public async Task<OrganisationResponseDto?> CreateOrganisationAsync(Guid userId, OrganisationRequestDto request)
    {
        var organisation = new Organisation
        {
            OrganisationId = Guid.NewGuid(),
            Name = request.Name,
            Type = request.Type,
            UserId = userId,
            MonthlyBudgetUsd = request.MonthlyBudgetUsd
        };
        await repository.AddAsync(organisation);
        await repository.SaveChangesAsync();

        // Seed an open-ended starting tariff so cost/carbon analytics are meaningful immediately.
        await rateRepo.AddAsync(new EnergyRate
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisation.OrganisationId,
            RatePerKwh = settings.CostPerKwh,
            ValidFromUtc = DateTime.UtcNow,
            ValidToUtc = null
        });
        await rateRepo.SaveChangesAsync();

        var dto = MapToDto(organisation);
        dto.CostPerKwh = settings.CostPerKwh;
        return dto;
    }

    public async Task<OrganisationResponseDto?> UpdateOrganisationAsync(
        Guid userId, Guid organisationId, OrganisationRequestDto request)
    {
        var organisation = await repository.GetByIdAsync(userId, organisationId);
        if (organisation is null) return null;
        organisation.Name = request.Name;
        organisation.Type = request.Type;
        organisation.MonthlyBudgetUsd = request.MonthlyBudgetUsd;
        await repository.SaveChangesAsync();
        return MapToDto(organisation);
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
        => await repository.ExistsAsync(userId);

    private static OrganisationResponseDto MapToDto(Organisation o) => new()
    {
        OrganisationId = o.OrganisationId,
        Name = o.Name,
        Type = o.Type,
        MonthlyBudgetUsd = o.MonthlyBudgetUsd,
        DeviceCount = o.Devices?.Count ?? 0
    };
}
