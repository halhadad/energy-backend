namespace energy_backend.Core.Interfaces;

public interface IAlertEvaluationRepository
{
    Task<(DateTime? LatestTimestamp, double TotalWatts)> GetLatestMinutePowerSumAsync(
        Guid orgId,
        CancellationToken ct = default);
}
