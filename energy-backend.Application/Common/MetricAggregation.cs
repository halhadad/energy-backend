using energy_backend.Core.Projections;

namespace energy_backend.Application.Common;

public static class MetricAggregation
{
    // collapse per-device rows for the same timestamp into one org-level row.
    // energy is summed, the averages are weighted by point count, min/max take the extremes
    public static List<AggregateMetricRow> ByTimestamp(IEnumerable<AggregateMetricRow> rows) =>
        rows.GroupBy(r => r.Timestamp)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var points = g.Sum(r => r.DataPointsCount);
                double Weighted(Func<AggregateMetricRow, double> sel) =>
                    points > 0 ? g.Sum(r => sel(r) * r.DataPointsCount) / points : 0;

                return new AggregateMetricRow(
                    g.Key,
                    g.Sum(r => r.TotalEnergyKwh),
                    Weighted(r => r.AverageActivePowerWatts),
                    g.Min(r => r.MinActivePowerWatts),
                    g.Max(r => r.MaxActivePowerWatts),
                    Weighted(r => r.AverageVoltageVolts),
                    Weighted(r => r.AverageCurrentAmps),
                    Weighted(r => r.AveragePowerFactor),
                    points);
            })
            .ToList();
}
