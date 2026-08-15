using AutoMapper;
using energy_backend.Application.Common;
using energy_backend.Application.Configuration;
using energy_backend.Application.Interfaces;
using energy_backend.Application.Models;
using energy_backend.Core.Projections;
using energy_backend.Core.Entities;
using energy_backend.Core.Enums;
using energy_backend.Core.Interfaces;
using energy_backend.Core.Services;
using Microsoft.Extensions.Logging;

namespace energy_backend.Application.Services;

public class HistoricalDataOrchestratorService(
    IAnalyticsRepository analyticsRepo,
    IEnergyRateRepository rateRepo,
    IOrganisationRepository orgRepo,
    EnergySettings settings,
    IMapper mapper,
    ILogger<HistoricalDataOrchestratorService> logger) : IHistoricalDataOrchestratorService
{
    public async Task<OrganisationAnalyticsDto?> GetHistoricalSnapshotAsync(
        Guid organisationId, string preset = "7d")
    {
        var now = DateTime.UtcNow;
        var config = ResolvePreset(preset, now);
        var windowLength = now - config.EarliestStart;
        var previousStart = config.EarliestStart - windowLength;
        var finest = config.Buckets[0];

        logger.LogDebug(
            "HistoricalDataOrchestrator: OrgId={OrgId} Preset={Preset} [{Start:O} -> {End:O}]",
            organisationId, preset, config.EarliestStart, now);

        // Fetch rates from the previous window's start so both the window and its
        // comparison period are covered by one query.
        var rates = await rateRepo.GetRatesInRangeAsync(
            organisationId,
            DateTime.SpecifyKind(previousStart, DateTimeKind.Utc),
            DateTime.SpecifyKind(now, DateTimeKind.Utc));

        decimal fallbackRate = settings.CostPerKwh;
        if (rates.Count == 0)
            fallbackRate = await rateRepo.GetCurrentRateAsync(organisationId);

        var result = new OrganisationAnalyticsDto
        {
            PeriodStart = config.EarliestStart,
            PeriodEnd = now
        };

        List<AggregateMetricRow> finestOrgRows = [];

        foreach (var bucket in config.Buckets)
        {
            var orgRows = MetricAggregation.ByTimestamp(await analyticsRepo.GetAggregateMetricsAsync(
                organisationId,
                DateTime.SpecifyKind(bucket.Start, DateTimeKind.Utc),
                DateTime.SpecifyKind(now, DateTimeKind.Utc),
                bucket.Granularity));

            if (bucket.Granularity == finest.Granularity) finestOrgRows = orgRows;

            var rollups = orgRows.Select(r => ToDto(r, bucket.LabelFormat, rates, fallbackRate)).ToList();
            switch (bucket.Granularity)
            {
                case TimeGranularity.Minute: result.MinuteRollups = rollups; break;
                case TimeGranularity.Hour: result.HourRollups = rollups; break;
                case TimeGranularity.Day: result.DayRollups = rollups; break;
                case TimeGranularity.Month: result.MonthRollups = rollups; break;
            }
        }

        result.Summary = await BuildSummaryAsync(
            organisationId, finest.Granularity, finestOrgRows, previousStart, config.EarliestStart, now, rates, fallbackRate);
        // use minute buckets for the device breakdown, they always exist for every device
        result.DeviceBreakdown = await BuildDeviceBreakdownAsync(
            organisationId, config.EarliestStart, now, TimeGranularity.Minute);

        return result;
    }

    private async Task<AnalyticsSummaryDto> BuildSummaryAsync(
        Guid orgId, TimeGranularity finest, List<AggregateMetricRow> windowRows,
        DateTime previousStart, DateTime windowStart, DateTime now,
        List<EnergyRate> rates, decimal fallbackRate)
    {
        var totalKwh = windowRows.Sum(r => r.TotalEnergyKwh);
        var totalCost = windowRows.Sum(r =>
            EnergyCalculator.CalculateEstimatedCost(r.TotalEnergyKwh, GetRateAt(r.Timestamp, rates, fallbackRate)));
        var peakRow = windowRows.Count > 0 ? windowRows.MaxBy(r => r.AverageActivePowerWatts) : null;

        var summary = new AnalyticsSummaryDto
        {
            TotalEnergyKwh = totalKwh,
            TotalCost = totalCost,
            PeakPowerWatts = peakRow?.AverageActivePowerWatts ?? 0,
            PeakAt = peakRow?.Timestamp,
            MinPowerWatts = windowRows.Count > 0 ? windowRows.Min(r => r.AverageActivePowerWatts) : 0
        };

        // Budget vs actual for the current calendar month (cost vs USD budget).
        var org = await orgRepo.GetByOrganisationIdAsync(orgId);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthRows = MetricAggregation.ByTimestamp(await analyticsRepo.GetAggregateMetricsAsync(
            orgId, monthStart, now, TimeGranularity.Day));
        var monthKwh = monthRows.Sum(r => r.TotalEnergyKwh);
        var monthCost = monthRows.Sum(r =>
            EnergyCalculator.CalculateEstimatedCost(r.TotalEnergyKwh, GetRateAt(r.Timestamp, rates, fallbackRate)));
        var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
        var elapsedDays = Math.Max((now - monthStart).TotalDays, 1.0 / 24);
        var budgetUsd = org?.MonthlyBudgetUsd ?? 0m;
        var projectedKwh = AnalyticsMath.ProjectMonth(monthKwh, elapsedDays, daysInMonth);

        summary.MonthToDateKwh = monthKwh;
        summary.MonthToDateCostUsd = monthCost;
        summary.MonthlyBudgetUsd = budgetUsd;
        summary.PercentOfBudget = AnalyticsMath.Percent((double)monthCost, (double)budgetUsd);
        summary.ProjectedMonthKwh = projectedKwh;
        summary.ProjectedMonthCostUsd = EnergyCalculator.CalculateEstimatedCost(
            projectedKwh, GetRateAt(now, rates, fallbackRate));

        // Period-over-period: the equal-length window immediately before this one.
        var prevRows = MetricAggregation.ByTimestamp(await analyticsRepo.GetAggregateMetricsAsync(
            orgId, previousStart, windowStart, finest));
        if (prevRows.Count > 0)
        {
            var prevKwh = prevRows.Sum(r => r.TotalEnergyKwh);
            var prevCost = prevRows.Sum(r =>
                EnergyCalculator.CalculateEstimatedCost(r.TotalEnergyKwh, GetRateAt(r.Timestamp, rates, fallbackRate)));
            summary.PreviousPeriod = new PeriodComparisonDto
            {
                TotalEnergyKwh = prevKwh,
                TotalCost = prevCost,
                EnergyChangePercent = AnalyticsMath.ChangePercent(totalKwh, prevKwh)
            };
        }

        return summary;
    }

    private async Task<List<DeviceBreakdownDto>> BuildDeviceBreakdownAsync(
        Guid orgId, DateTime start, DateTime now, TimeGranularity granularity)
    {
        var rows = await analyticsRepo.GetDeviceSnapshotMetricsAsync(
            orgId, DateTime.SpecifyKind(start, DateTimeKind.Utc), DateTime.SpecifyKind(now, DateTimeKind.Utc), granularity);

        var perDevice = rows
            .GroupBy(r => r.DeviceName)
            .Select(g => new DeviceBreakdownDto
            {
                DeviceName = g.Key,
                TotalEnergyKwh = g.Sum(r => r.TotalEnergyKwh)
            })
            .ToList();

        var grandTotal = perDevice.Sum(d => d.TotalEnergyKwh);
        foreach (var d in perDevice)
            d.SharePercent = grandTotal > 0 ? d.TotalEnergyKwh / grandTotal * 100 : 0;

        return perDevice.OrderByDescending(d => d.TotalEnergyKwh).ToList();
    }

    private EnergyMetricRollupDto ToDto(
        AggregateMetricRow row, string labelFormat, List<EnergyRate> rates, decimal fallbackRate)
    {
        var dto = mapper.Map<EnergyMetricRollupDto>(row);
        dto.Timestamp = DateTime.SpecifyKind(dto.Timestamp, DateTimeKind.Utc);
        dto.Label = dto.Timestamp.ToString(labelFormat);
        dto.EstimatedCost = EnergyCalculator.CalculateEstimatedCost(
            row.TotalEnergyKwh, GetRateAt(dto.Timestamp, rates, fallbackRate));
        return dto;
    }

    private static decimal GetRateAt(DateTime utcTs, List<EnergyRate> rates, decimal fallback)
    {
        foreach (var r in rates)
        {
            if (r.ValidFromUtc <= utcTs && (r.ValidToUtc == null || r.ValidToUtc >= utcTs))
                return r.RatePerKwh;
        }
        return fallback;
    }

    private record BucketConfig(DateTime Start, TimeGranularity Granularity, string LabelFormat);
    private record PresetConfig(DateTime EarliestStart, List<BucketConfig> Buckets);

    private static PresetConfig ResolvePreset(string preset, DateTime now) =>
        preset.ToLowerInvariant() switch
        {
            "24h" => new PresetConfig(now.AddHours(-24),
            [
                new(now.AddHours(-24), TimeGranularity.Minute, "HH:mm"),
                new(now.AddHours(-24), TimeGranularity.Hour,   "HH:mm")
            ]),

            "7d" => new PresetConfig(now.AddDays(-7),
            [
                new(now.AddDays(-7), TimeGranularity.Hour, "HH:mm ddd"),
                new(now.AddDays(-7), TimeGranularity.Day,  "ddd d MMM")
            ]),

            "30d" => new PresetConfig(now.AddDays(-30),
            [
                new(now.AddDays(-30), TimeGranularity.Day,   "d MMM"),
                new(now.AddDays(-30), TimeGranularity.Month, "MMM yyyy")
            ]),

            _ => ResolvePreset("7d", now)
        };
}
