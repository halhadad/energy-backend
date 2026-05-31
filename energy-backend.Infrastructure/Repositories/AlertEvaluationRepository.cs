using energy_backend.Core.Interfaces;
using energy_backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Infrastructure.Repositories;

public class AlertEvaluationRepository(EnergyDbContext context) : IAlertEvaluationRepository
{
    public async Task<(DateTime? LatestTimestamp, float TotalWatts)> GetLatestMinutePowerSumAsync(
        Guid orgId,
        CancellationToken ct = default)
    {
        var latestTs = await context.AggregateMinuteEnergies
            .Where(a => a.OrgId == orgId)
            .MaxAsync(a => (DateTime?)a.Timestamp, ct);

        if (!latestTs.HasValue) return (null, 0f);

        var totalWatts = await context.AggregateMinuteEnergies
            .Where(a => a.OrgId == orgId && a.Timestamp == latestTs.Value)
            .SumAsync(a => a.AverageActivePowerWatts, ct);

        return (latestTs, totalWatts);
    }
}
