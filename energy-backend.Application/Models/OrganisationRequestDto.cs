namespace energy_backend.Application.Models;

public class OrganisationRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal MonthlyBudgetUsd { get; set; }
}
