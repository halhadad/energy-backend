using energy_backend.Application.Models;
using energy_backend.Application.Services;
using energy_backend.Core.Entities;
using energy_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace energy_backend.Infrastructure.Services
{
    public class AggregationService : IAggregationService
    {
        private readonly EnergyDbContext _context;
        private readonly ILogger<AggregationService> _logger;

        public AggregationService(EnergyDbContext context, ILogger<AggregationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AggregationResultDto> GetSnapshotAsync(AggregationRequestDto request)
        {
            request.StartTime = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc);
            request.EndTime = DateTime.SpecifyKind(request.EndTime, DateTimeKind.Utc);

            var dataPoints = new List<TimeSeriesDataPoint>();
            double total = 0;

            switch (request.AggregationLevel)
            {
                case AggregationLevel.Minute:
                    var minutes = await _context.AggregateMinuteEnergies
                        .Where(a => a.OrgId == request.OrganisationId
                                 && a.Timestamp >= request.StartTime
                                 && a.Timestamp < request.EndTime)
                        .GroupBy(a => a.Timestamp)
                        .Select(g => new { Timestamp = g.Key, Total = g.Sum(x => x.TotalEnergyKwh) })
                        .OrderBy(x => x.Timestamp)
                        .ToListAsync();
                    dataPoints = minutes.Select(x => new TimeSeriesDataPoint
                    {
                        Timestamp = x.Timestamp,
                        Value = x.Total,
                        Label = x.Timestamp.ToString("HH:mm")
                    }).ToList();
                    total = dataPoints.Sum(x => x.Value);
                    break;

                case AggregationLevel.Hour:
                    var hours = await _context.AggregateHourEnergies
                        .Where(a => a.OrgId == request.OrganisationId
                                 && a.Timestamp >= request.StartTime
                                 && a.Timestamp < request.EndTime)
                        .GroupBy(a => a.Timestamp)
                        .Select(g => new { Timestamp = g.Key, Total = g.Sum(x => x.TotalEnergyKwh) })
                        .OrderBy(x => x.Timestamp)
                        .ToListAsync();
                    dataPoints = hours.Select(x => new TimeSeriesDataPoint
                    {
                        Timestamp = x.Timestamp,
                        Value = x.Total,
                        Label = x.Timestamp.ToString("HH:mm")
                    }).ToList();
                    total = dataPoints.Sum(x => x.Value);
                    break;

                case AggregationLevel.Day:
                    var days = await _context.AggregateDayEnergies
                        .Where(a => a.OrgId == request.OrganisationId
                                 && a.Timestamp >= request.StartTime
                                 && a.Timestamp < request.EndTime)
                        .GroupBy(a => a.Timestamp)
                        .Select(g => new { Timestamp = g.Key, Total = g.Sum(x => x.TotalEnergyKwh) })
                        .OrderBy(x => x.Timestamp)
                        .ToListAsync();
                    dataPoints = days.Select(x => new TimeSeriesDataPoint
                    {
                        Timestamp = x.Timestamp,
                        Value = x.Total,
                        Label = x.Timestamp.ToString("yyyy-MM-dd")
                    }).ToList();
                    total = dataPoints.Sum(x => x.Value);
                    break;

                case AggregationLevel.Month:
                    var months = await _context.AggregateMonthEnergies
                        .Where(a => a.OrgId == request.OrganisationId
                                 && a.Timestamp >= request.StartTime
                                 && a.Timestamp < request.EndTime)
                        .GroupBy(a => a.Timestamp)
                        .Select(g => new { Timestamp = g.Key, Total = g.Sum(x => x.TotalEnergyKwh) })
                        .OrderBy(x => x.Timestamp)
                        .ToListAsync();
                    dataPoints = months.Select(x => new TimeSeriesDataPoint
                    {
                        Timestamp = x.Timestamp,
                        Value = x.Total,
                        Label = x.Timestamp.ToString("yyyy-MM")
                    }).ToList();
                    total = dataPoints.Sum(x => x.Value);
                    break;
            }

            return new AggregationResultDto
            {
                DataPoints = dataPoints,
                TotalValue = total,
                AggregationPeriod = request.AggregationLevel.ToString()
            };
        }
    }
}