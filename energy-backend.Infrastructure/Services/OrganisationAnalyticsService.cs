using energy_backend.Application.Models;
using energy_backend.Application.Models.SignalR;
using energy_backend.Core.Interfaces;
using energy_backend.Data;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Infrastructure.Services
{
    public class OrganisationAnalyticsService
    {
        private readonly IAggregatedEnergyRepository _repository;
        private readonly EnergyDbContext _context;

        private const float CostPerKwh = 1.92f;
        private const float CarbonPerKwh = 0.42f;

        public OrganisationAnalyticsService(
            IAggregatedEnergyRepository repository,
            EnergyDbContext context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task<OrganisationAnalyticsDto> GetOrganisationAnalyticsAsync(Guid organisationId)
        {
            var now = DateTime.UtcNow;
            var startOfToday = now.Date;
            var startOfWeek = now.Date.AddDays(-6);
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfMonth = startOfMonth.AddMonths(1);

            var dto = new OrganisationAnalyticsDto();

            // ── Pie charts (device type breakdown) ───────────────────────────
            dto.PieChartDay = BuildBreakdown(
                await _repository.GetAggregatedByDeviceTypeAsync(organisationId, startOfToday, now));
            dto.PieChartWeek = BuildBreakdown(
                await _repository.GetAggregatedByDeviceTypeAsync(organisationId, startOfWeek, now));
            dto.PieChartMonth = BuildBreakdown(
                await _repository.GetAggregatedByDeviceTypeAsync(organisationId, startOfMonth, endOfMonth));

            // ── Time series charts ────────────────────────────────────────────
            dto.ConsumptionChartDay = BuildTimeSeries(
                await _repository.GetAggregatedByHourAsync(organisationId, startOfToday, now));
            dto.ConsumptionChartWeek = BuildTimeSeries(
                await _repository.GetAggregatedByDayAsync(organisationId, startOfWeek, now));
            dto.ConsumptionChartMonth = BuildTimeSeries(
                await _repository.GetAggregatedByDayAsync(organisationId, startOfMonth, endOfMonth));

            dto.CostChartDay = ScaleTimeSeries(dto.ConsumptionChartDay, CostPerKwh);
            dto.CostChartWeek = ScaleTimeSeries(dto.ConsumptionChartWeek, CostPerKwh);
            dto.CostChartMonth = ScaleTimeSeries(dto.ConsumptionChartMonth, CostPerKwh);

            dto.CarbonChartDay = ScaleTimeSeries(dto.ConsumptionChartDay, CarbonPerKwh);
            dto.CarbonChartWeek = ScaleTimeSeries(dto.ConsumptionChartWeek, CarbonPerKwh);
            dto.CarbonChartMonth = ScaleTimeSeries(dto.ConsumptionChartMonth, CarbonPerKwh);

            // ── Current live power in Watts ───────────────────────────────────
            // Sum AverageWatts across all devices for the latest minute bucket
            var latestTimestamp = await _context.AggregateMinuteEnergies
                .Where(a => a.OrgId == organisationId)
                .MaxAsync(a => (DateTime?)a.Timestamp);

            if (latestTimestamp.HasValue)
            {
                dto.CurrentWatts = await _context.AggregateMinuteEnergies
                    .Where(a => a.OrgId == organisationId && a.Timestamp == latestTimestamp.Value)
                    .SumAsync(a => a.AverageWatts);
            }

            // ── Month-to-date totals ──────────────────────────────────────────
            var totalKwhMonth = await _repository.GetTotalConsumptionAsync(
                organisationId, startOfMonth, endOfMonth);

            dto.Consumption = totalKwhMonth;
            dto.Cost = (float)Math.Round(totalKwhMonth * CostPerKwh, 2);
            dto.Carbon = (float)Math.Round(totalKwhMonth * CarbonPerKwh, 2);

            // ── Energy budget from organisation record ────────────────────────
            var org = await _context.Organisations.FindAsync(organisationId);
            dto.EnergyBudget = org?.EnergyBudget ?? 0f;

            return dto;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static BreakdownDto BuildBreakdown(Dictionary<string, float> data) =>
            new() { Labels = data.Keys.ToList(), Data = data.Values.ToList() };

        private static TimeSeriesDto BuildTimeSeries(Dictionary<string, float> data) =>
            new() { Labels = data.Keys.ToList(), Data = data.Values.ToList() };

        private static TimeSeriesDto ScaleTimeSeries(TimeSeriesDto original, float factor) =>
            new()
            {
                Labels = original.Labels,
                Data = original.Data.Select(v => (float)Math.Round(v * factor, 2)).ToList()
            };
    }
}