using energy_backend.Application.Models;
using energy_backend.Application.Models.SignalR;
using energy_backend.Core.Interfaces;
using energy_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace energy_backend.Infrastructure.Services
{
    public class OrganisationAnalyticsService
    {
        private readonly IAggregatedEnergyRepository _repository;
        private readonly EnergyDbContext _context;
        private readonly float _costPerKwh;
        private readonly float _carbonKgPerKwh;

        public OrganisationAnalyticsService(
            IAggregatedEnergyRepository repository,
            EnergyDbContext context,
            IConfiguration configuration)
        {
            _repository = repository;
            _context = context;

            // Read from appsettings.json → EnergySettings section.
            // Defaults: $0.28/kWh (global average), 0.233 kg CO₂/kWh (IEA 2023 world avg).
            _costPerKwh = configuration.GetValue<float>("EnergySettings:CostPerKwh", 0.28f);
            _carbonKgPerKwh = configuration.GetValue<float>("EnergySettings:CarbonKgPerKwh", 0.233f);
        }

        public async Task<OrganisationAnalyticsDto> GetOrganisationAnalyticsAsync(Guid organisationId)
        {
            var now = DateTime.UtcNow;
            var startOfToday = now.Date;
            var startOfWeek = now.Date.AddDays(-6);
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfMonth = startOfMonth.AddMonths(1);

            var dto = new OrganisationAnalyticsDto();

            // ── Pie charts (device type breakdown) ────────────────────────────
            dto.PieChartDay = BuildBreakdown(
                await _repository.GetAggregatedByDeviceTypeAsync(organisationId, startOfToday, now));
            dto.PieChartWeek = BuildBreakdown(
                await _repository.GetAggregatedByDeviceTypeAsync(organisationId, startOfWeek, now));
            dto.PieChartMonth = BuildBreakdown(
                await _repository.GetAggregatedByDeviceTypeAsync(organisationId, startOfMonth, endOfMonth));

            // ── Time series charts ─────────────────────────────────────────────
            dto.ConsumptionChartDay = BuildTimeSeries(
                await _repository.GetAggregatedByHourAsync(organisationId, startOfToday, now));
            dto.ConsumptionChartWeek = BuildTimeSeries(
                await _repository.GetAggregatedByDayAsync(organisationId, startOfWeek, now));
            dto.ConsumptionChartMonth = BuildTimeSeries(
                await _repository.GetAggregatedByDayAsync(organisationId, startOfMonth, endOfMonth));

            dto.CostChartDay = ScaleTimeSeries(dto.ConsumptionChartDay, _costPerKwh);
            dto.CostChartWeek = ScaleTimeSeries(dto.ConsumptionChartWeek, _costPerKwh);
            dto.CostChartMonth = ScaleTimeSeries(dto.ConsumptionChartMonth, _costPerKwh);

            dto.CarbonChartDay = ScaleTimeSeries(dto.ConsumptionChartDay, _carbonKgPerKwh);
            dto.CarbonChartWeek = ScaleTimeSeries(dto.ConsumptionChartWeek, _carbonKgPerKwh);
            dto.CarbonChartMonth = ScaleTimeSeries(dto.ConsumptionChartMonth, _carbonKgPerKwh);

            // ── Current live power ─────────────────────────────────────────────
            // Sum AveragePowerWatts across all devices for the latest minute bucket.
            var latestTs = await _context.AggregateMinuteEnergies
                .Where(a => a.OrgId == organisationId)
                .MaxAsync(a => (DateTime?)a.Timestamp);

            if (latestTs.HasValue)
            {
                dto.CurrentPowerWatts = await _context.AggregateMinuteEnergies
                    .Where(a => a.OrgId == organisationId && a.Timestamp == latestTs.Value)
                    .SumAsync(a => a.AveragePowerWatts);
            }

            // ── Total rated power (sum of all device nameplates) ───────────────
            // Used as the "rated" midpoint marker on the consumption progress bar.
            dto.TotalRatedPowerWatts = await _context.Devices
                .Where(d => d.OrganisationId == organisationId)
                .SumAsync(d => (float?)d.RatedPowerWatts) ?? 0f;

            // ── Month-to-date totals ───────────────────────────────────────────
            var totalKwhMonth = await _repository.GetTotalConsumptionAsync(
                organisationId, startOfMonth, endOfMonth);

            dto.Consumption = totalKwhMonth;
            dto.Cost = (float)Math.Round(totalKwhMonth * _costPerKwh, 2);
            dto.Carbon = (float)Math.Round(totalKwhMonth * _carbonKgPerKwh, 2);

            // ── Organisation power budget ──────────────────────────────────────
            var org = await _context.Organisations.FindAsync(organisationId);
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