using energy_backend.Application.Interfaces;
using energy_backend.Application.Models;
using energy_backend.Application.Models.Projections;
using energy_backend.Core.Enums;
using energy_backend.Core.Interfaces;
using energy_backend.Core.Services;
namespace energy_backend.Application.Services;

public class EnergyAnalyticsOrchestratorService(IAnalyticsRepository metricRepository)
    : IEnergyAnalyticsOrchestratorService
{
    public async Task<OrganisationAnalyticsDto?> GetOrganisationAnalyticsAsync(Guid organisationId)

    {
        var now = DateTime.UtcNow;
        var minuteStart = now.AddMinutes(-30);
        var hourStart = now.AddDays(-1);
        var dayStart = now.AddDays(-7);
        var monthStart = now.AddMonths(-1);

        var minuteRollups = await metricRepository.GetAggregateMetricsAsync(organisationId, minuteStart, now, TimeGranularity.Minute);
        var hourRollups = await metricRepository.GetAggregateMetricsAsync(organisationId, hourStart, now, TimeGranularity.Hour);
        var dayRollups = await metricRepository.GetAggregateMetricsAsync(organisationId, dayStart, now, TimeGranularity.Day);
        var monthRollups = await metricRepository.GetAggregateMetricsAsync(organisationId, monthStart, now, TimeGranularity.Month);
        var costRate = await metricRepository.GetUserCostRateAsync(organisationId);
        return new OrganisationAnalyticsDto
        {
            PeriodStart = monthStart,
            PeriodEnd = now,
            MinuteRollups = minuteRollups.Select(x => MapToRollupDto(x, costRate, "HH:mm:ss")).ToList(),
            HourRollups = hourRollups.Select(x => MapToRollupDto(x, costRate, "HH:mm")).ToList(),
            DayRollups = dayRollups.Select(x => MapToRollupDto(x, costRate, "ddd d")).ToList(),
            MonthRollups = monthRollups.Select(x => MapToRollupDto(x, costRate, "MMM yy")).ToList()
        };
    }
    private static EnergyMetricRollupDto MapToRollupDto(
        AggregateMetricRow row,
        decimal costRate,
        string labelFormat) => new()
        {
            Timestamp = row.Timestamp,
            Label = row.Timestamp.ToString(labelFormat),
            TotalEnergyKwh = (decimal)row.TotalEnergyKwh,
            AverageActivePowerWatts = row.AverageActivePowerWatts,
            MinActivePowerWatts = row.MinActivePowerWatts,
            MaxActivePowerWatts = row.MaxActivePowerWatts,
            AverageVoltageVolts = row.AverageVoltageVolts,
            TotalCurrentAmps = row.AverageCurrentAmps,
            AveragePowerFactor = row.AveragePowerFactor,
            DataPointsCount = row.DataPointsCount,
            EstimatedCost = (decimal)EnergyCalculator.CalculateEstimatedCost(row.TotalEnergyKwh, (float)costRate)
        };
}