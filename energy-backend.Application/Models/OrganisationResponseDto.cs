namespace energy_backend.Application.Models;

public class OrganisationResponseDto
{
    public Guid OrganisationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal MonthlyBudgetUsd { get; set; }
    public decimal CostPerKwh { get; set; }
    public int DeviceCount { get; set; }
}
