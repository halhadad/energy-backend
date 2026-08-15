using energy_backend.Application.Interfaces;
using energy_backend.Application.Models;
using energy_backend.Core.Projections;
using energy_backend.Core.Enums;
using energy_backend.Core.Interfaces;

namespace energy_backend.Application.Services;

public class SnapshotService(
    IAnalyticsRepository analyticsRepo) : ISnapshotService
{
    public async Task<AggregationResultDto> GetSnapshotAsync(AggregationRequestDto request)
    {
        request.StartTime = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc);
        request.EndTime = DateTime.SpecifyKind(request.EndTime, DateTimeKind.Utc);

        var labelFormat = request.Granularity switch
        {
            TimeGranularity.Minute => "HH:mm:ss",
            TimeGranularity.Hour => "HH:mm",
            TimeGranularity.Day => "ddd d",
            TimeGranularity.Month => "MMM yy",
            _ => "HH:mm:ss"
        };

        var metrics = await analyticsRepo.GetAggregateMetricsAsync(
            request.OrganisationId,
            request.StartTime,
            request.EndTime,
            request.Granularity);

        var dataPoints = metrics.Select(r => new TimeSeriesDataPoint
        {
            Timestamp = r.Timestamp,
            Label = r.Timestamp.ToString(labelFormat),
            Value = r.TotalEnergyKwh,
            AverageWatts = r.AverageActivePowerWatts,
            AverageVoltageVolts = r.AverageVoltageVolts,
            TotalCurrentAmps = r.AverageCurrentAmps,
            AveragePowerFactor = r.AveragePowerFactor
        }).ToList();

        return new AggregationResultDto
        {
            DataPoints = dataPoints,
            TotalValue = dataPoints.Sum(x => x.Value),
            AggregationPeriod = request.Granularity.ToString()
        };
    }
}