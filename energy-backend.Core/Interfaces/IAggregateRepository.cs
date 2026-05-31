using energy_backend.Core.Entities;
namespace energy_backend.Core.Interfaces;

public interface IAggregateRepository
{
    Task UpsertMinuteAsync(AggregateMinuteEnergy aggregate, CancellationToken ct = default);
    Task UpsertHourAsync(AggregateHourEnergy aggregate, CancellationToken ct = default);
    Task UpsertDayAsync(AggregateDayEnergy aggregate, CancellationToken ct = default);
    Task UpsertMonthAsync(AggregateMonthEnergy aggregate, CancellationToken ct = default);
    Task<AggregateMinuteEnergy?> GetLatestMinuteAsync(Guid orgId, DateTime timestamp, CancellationToken ct = default);
    Task<AggregateHourEnergy?> GetLatestHourAsync(Guid orgId, DateTime timestamp, CancellationToken ct = default);
    Task<AggregateDayEnergy?> GetLatestDayAsync(Guid orgId, DateTime timestamp, CancellationToken ct = default);
    Task<AggregateMonthEnergy?> GetLatestMonthAsync(Guid orgId, DateTime timestamp, CancellationToken ct = default);
    Task<List<AggregateMinuteEnergy>> GetMinutesBeforeAsync(DateTime from, DateTime boundary, CancellationToken ct = default);
    Task<List<AggregateHourEnergy>> GetHoursBeforeAsync(DateTime boundary, CancellationToken ct = default);
    Task<List<AggregateDayEnergy>> GetDaysBeforeAsync(DateTime boundary, CancellationToken ct = default);
    // ADDED: was missing — needed by RecurrentDataRollupWorker gap-fill guard
    Task<bool> MinuteExistsAsync(Guid orgId, Guid? deviceId, DateTime minuteSlot, CancellationToken ct = default);
    Task<bool> HourExistsAsync(Guid orgId, Guid? deviceId, DateTime hourSlot, CancellationToken ct = default);
    Task<bool> DayExistsAsync(Guid orgId, Guid? deviceId, DateTime daySlot, CancellationToken ct = default);
    Task<bool> MonthExistsAsync(Guid orgId, Guid? deviceId, DateTime monthSlot, CancellationToken ct = default);
    Task AddHourAsync(AggregateHourEnergy aggregate, CancellationToken ct = default);
    Task AddDayAsync(AggregateDayEnergy aggregate, CancellationToken ct = default);
    Task AddMonthAsync(AggregateMonthEnergy aggregate, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}