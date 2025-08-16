using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Application.Models.SignalR;
using energy_backend.Application.Models;
using energy_backend.Infrastructure.Repositories;
using energy_backend.Core.Interfaces;

namespace energy_backend.Infrastructure.Services
{
    public class OrganisationAnalyticsService
    {
        private readonly IAggregatedEnergyRepository _repository;

        public OrganisationAnalyticsService(IAggregatedEnergyRepository repository)
        {
            _repository = repository;
        }

        private const float CostFactor = 1.92f;
        private const float CarbonFactor = 0.42f;
        public async Task<OrganisationAnalyticsDto> GetOrganisationAnalyticsAsync(Guid organisationId)
        {
            
        var now = DateTime.UtcNow;

            // Periods
            var startOfToday = now.Date;
            var startOfWeek = now.Date.AddDays(-6);
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            var dto = new OrganisationAnalyticsDto();

            // Pie charts
            dto.PieChartDay = await BuildBreakdownDtoAsync(
                await _repository.GetAggregatedByDeviceTypeAsync(organisationId, startOfToday, now));

            dto.PieChartWeek = await BuildBreakdownDtoAsync(
                await _repository.GetAggregatedByDeviceTypeAsync(organisationId, startOfWeek, now));

            dto.PieChartMonth = await BuildBreakdownDtoAsync(
                await _repository.GetAggregatedByDeviceTypeAsync(organisationId, startOfMonth, endOfMonth));

            // Time series charts
            dto.ConsumptionChartDay = await BuildTimeSeriesDtoAsync(
                await _repository.GetAggregatedByHourAsync(organisationId, startOfToday, now));

            dto.ConsumptionChartWeek = await BuildTimeSeriesDtoAsync(
                await _repository.GetAggregatedByDayAsync(organisationId, startOfWeek, now));

            dto.ConsumptionChartMonth = await BuildTimeSeriesDtoAsync(
                await _repository.GetAggregatedByDayAsync(organisationId, startOfMonth, endOfMonth));

            // Cost charts
            dto.CostChartDay = ScaleTimeSeriesDto(dto.ConsumptionChartDay, CostFactor);
            dto.CostChartWeek = ScaleTimeSeriesDto(dto.ConsumptionChartWeek, CostFactor);
            dto.CostChartMonth = ScaleTimeSeriesDto(dto.ConsumptionChartMonth, CostFactor);

            // Carbon charts
            dto.CarbonChartDay = ScaleTimeSeriesDto(dto.ConsumptionChartDay, CarbonFactor);
            dto.CarbonChartWeek = ScaleTimeSeriesDto(dto.ConsumptionChartWeek, CarbonFactor);
            dto.CarbonChartMonth = ScaleTimeSeriesDto(dto.ConsumptionChartMonth, CarbonFactor);

            // Totals for the month
            var totalKwhMonth = await _repository.GetTotalConsumptionAsync(organisationId, startOfMonth, endOfMonth);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠ WARNING: Something important!");
            Console.ResetColor();
            dto.Consumption = totalKwhMonth;
            dto.Cost = totalKwhMonth * CostFactor;
            dto.Carbon = totalKwhMonth * CarbonFactor;

            return dto;
        }

        private Task<BreakdownDto> BuildBreakdownDtoAsync(Dictionary<string, float> data)
        {
            var dto = new BreakdownDto
            {
                Labels = data.Keys.ToList(),
                Data = data.Values.ToList()
            };
            return Task.FromResult(dto);
        }

        private Task<TimeSeriesDto> BuildTimeSeriesDtoAsync(Dictionary<string, float> data)
        {
            var dto = new TimeSeriesDto
            {
                Labels = data.Keys.ToList(),
                Data = data.Values.ToList()
            };
            return Task.FromResult(dto);
        }

        private TimeSeriesDto ScaleTimeSeriesDto(TimeSeriesDto original, float factor)
        {
            return new TimeSeriesDto
            {
                Labels = original.Labels,
                Data = original.Data.Select(v => (float)Math.Round(v * factor, 2)).ToList()
            };
        }
    }
}
