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

            // ── Resolve user's electricity cost rate ──────────────────────────
            var org = await _context.Organisations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrganisationId == organisationId);

            float costPerKwh = 0.28f;
            if (org != null)
            {
                var setting = await _context.Settings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.UserId == org.UserId);
                if (setting != null)
                    costPerKwh = setting.ElectricityCostPerKwh;
            }

            var dto = new OrganisationAnalyticsDto
            {
                ElectricityCostPerKwh = costPerKwh
            };

            // ── Pie charts ────────────────────────────────────────────────────
            dto.PieChartDay = BuildBreakdown(await _repository.GetAggregatedByDeviceTypeAsync(organisationId, startOfToday, now));
            dto.PieChartWeek = BuildBreakdown(await _repository.GetAggregatedByDeviceTypeAsync(organisationId, startOfWeek, now));
            dto.PieChartMonth = BuildBreakdown(await _repository.GetAggregatedByDeviceTypeAsync(organisationId, startOfMonth, endOfMonth));

            // ── Consumption time series ───────────────────────────────────────
            dto.ConsumptionChartDay = BuildTimeSeries(await _repository.GetAggregatedByHourAsync(organisationId, startOfToday, now));
            dto.ConsumptionChartWeek = BuildTimeSeries(await _repository.GetAggregatedByDayAsync(organisationId, startOfWeek, now));
            dto.ConsumptionChartMonth = BuildTimeSeries(await _repository.GetAggregatedByDayAsync(organisationId, startOfMonth, endOfMonth));

            // ── Cost time series ──────────────────────────────────────────────
            dto.CostChartDay = ScaleTimeSeries(dto.ConsumptionChartDay, costPerKwh);
            dto.CostChartWeek = ScaleTimeSeries(dto.ConsumptionChartWeek, costPerKwh);
            dto.CostChartMonth = ScaleTimeSeries(dto.ConsumptionChartMonth, costPerKwh);

            // ── Live snapshot — latest minute bucket ──────────────────────────
            var latestTs = await _context.AggregateMinuteEnergies
                .Where(a => a.OrgId == organisationId)
                .MaxAsync(a => (DateTime?)a.Timestamp);

            if (latestTs.HasValue)
            {
                var latestRows = await _context.AggregateMinuteEnergies
                    .Where(a => a.OrgId == organisationId && a.Timestamp == latestTs.Value)
                    .ToListAsync();

                dto.CurrentPowerWatts = latestRows.Sum(a => a.AverageActivePowerWatts);
                dto.TotalCurrentAmps = latestRows.Sum(a => a.AverageCurrentAmps);
                dto.AverageVoltageVolts = latestRows.Any()
                    ? latestRows.Average(a => a.AverageVoltageVolts)
                    : 0f;
                dto.AveragePowerFactor = latestRows.Any()
                    ? latestRows.Average(a => a.AveragePowerFactor)
                    : 0f;
            }

            // ── Rated power ───────────────────────────────────────────────────
            dto.TotalRatedPowerWatts = await _context.Devices
                .Where(d => d.OrganisationId == organisationId)
                .SumAsync(d => (float?)d.RatedPowerWatts) ?? 0f;

            // ── Month-to-date totals ──────────────────────────────────────────
            var totalKwhMonth = await _repository.GetTotalConsumptionAsync(organisationId, startOfMonth, endOfMonth);
            dto.Consumption = totalKwhMonth;
            dto.Cost = (float)Math.Round(totalKwhMonth * costPerKwh, 2);

            // ── Org power budget ──────────────────────────────────────────────
            dto.EnergyBudget = org?.EnergyBudget ?? 0f;

            return dto;
        }

        private static BreakdownDto BuildBreakdown(Dictionary<string, float> data) =>
            new() { Labels = data.Keys.ToList(), Data = data.Values.ToList() };

        private static TimeSeriesDto BuildTimeSeries(Dictionary<string, float> data) =>
            new() { Labels = data.Keys.ToList(), Data = data.Values.ToList() };

        private static TimeSeriesDto ScaleTimeSeries(TimeSeriesDto src, float factor) =>
            new()
            {
                Labels = src.Labels,
                Data = src.Data.Select(v => (float)Math.Round(v * factor, 4)).ToList()
            };
    }
}