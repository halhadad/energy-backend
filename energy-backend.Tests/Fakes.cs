using energy_backend.Application.Interfaces;
using energy_backend.Application.Models.SignalR;
using energy_backend.Core.Entities;
using energy_backend.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace energy_backend.Tests;

// in-memory repo that records which tiers were upserted
internal sealed class FakeAggregateRepository : IAggregateRepository
{
    public readonly Dictionary<(Guid DeviceId, DateTime Ts), AggregateMinuteEnergy> Minutes = new();
    public int MinuteUpserts, HourUpserts, DayUpserts, MonthUpserts, SaveCalls;

    public Task UpsertMinuteAsync(AggregateMinuteEnergy a, CancellationToken ct = default)
    {
        MinuteUpserts++;
        Minutes[(a.DeviceId!.Value, a.Timestamp)] = a;
        return Task.CompletedTask;
    }
    public Task UpsertHourAsync(AggregateHourEnergy a, CancellationToken ct = default) { HourUpserts++; return Task.CompletedTask; }
    public Task UpsertDayAsync(AggregateDayEnergy a, CancellationToken ct = default) { DayUpserts++; return Task.CompletedTask; }
    public Task UpsertMonthAsync(AggregateMonthEnergy a, CancellationToken ct = default) { MonthUpserts++; return Task.CompletedTask; }

    public Task<AggregateMinuteEnergy?> GetMinuteAsync(Guid deviceId, DateTime ts, CancellationToken ct = default)
        => Task.FromResult(Minutes.TryGetValue((deviceId, ts), out var row) ? row : null);

    public Task<AggregateMinuteEnergy?> GetLatestMinuteAsync(Guid orgId, DateTime ts, CancellationToken ct = default)
        => Task.FromResult<AggregateMinuteEnergy?>(null);
    public Task<AggregateHourEnergy?> GetLatestHourAsync(Guid orgId, DateTime ts, CancellationToken ct = default)
        => Task.FromResult<AggregateHourEnergy?>(null);
    public Task<AggregateDayEnergy?> GetLatestDayAsync(Guid orgId, DateTime ts, CancellationToken ct = default)
        => Task.FromResult<AggregateDayEnergy?>(null);
    public Task<AggregateMonthEnergy?> GetLatestMonthAsync(Guid orgId, DateTime ts, CancellationToken ct = default)
        => Task.FromResult<AggregateMonthEnergy?>(null);
    public Task<List<AggregateMinuteEnergy>> GetMinutesBeforeAsync(DateTime from, DateTime boundary, CancellationToken ct = default)
        => Task.FromResult(new List<AggregateMinuteEnergy>());
    public Task<List<AggregateHourEnergy>> GetHoursBeforeAsync(DateTime boundary, CancellationToken ct = default)
        => Task.FromResult(new List<AggregateHourEnergy>());
    public Task<List<AggregateDayEnergy>> GetDaysBeforeAsync(DateTime boundary, CancellationToken ct = default)
        => Task.FromResult(new List<AggregateDayEnergy>());
    public Task<bool> HourExistsAsync(Guid orgId, Guid? deviceId, DateTime slot, CancellationToken ct = default) => Task.FromResult(false);
    public Task<bool> DayExistsAsync(Guid orgId, Guid? deviceId, DateTime slot, CancellationToken ct = default) => Task.FromResult(false);
    public Task<bool> MonthExistsAsync(Guid orgId, Guid? deviceId, DateTime slot, CancellationToken ct = default) => Task.FromResult(false);
    public Task AddHourAsync(AggregateHourEnergy a, CancellationToken ct = default) => Task.CompletedTask;
    public Task AddDayAsync(AggregateDayEnergy a, CancellationToken ct = default) => Task.CompletedTask;
    public Task AddMonthAsync(AggregateMonthEnergy a, CancellationToken ct = default) => Task.CompletedTask;
    public Task SaveChangesAsync(CancellationToken ct = default) { SaveCalls++; return Task.CompletedTask; }
}

internal sealed class FakeDeviceRepository : IDeviceRepository
{
    private readonly Dictionary<Guid, Device> _devices = new();
    public void Add(Device d) => _devices[d.DeviceId] = d;

    public Task<Device?> GetByDeviceIdAsync(Guid deviceId)
        => Task.FromResult(_devices.TryGetValue(deviceId, out var d) ? d : null);

    public Task<IEnumerable<Device>> GetAllByUserIdAsync(Guid userId) => Task.FromResult<IEnumerable<Device>>([]);
    public Task<Device?> GetByIdAsync(Guid userId, Guid deviceId) => Task.FromResult<Device?>(null);
    public Task<IEnumerable<Device>> GetByOrganisationIdAsync(Guid userId, Guid organisationId) => Task.FromResult<IEnumerable<Device>>([]);
    public Task<Device?> AddAsync(Device device) => Task.FromResult<Device?>(device);
    public Task<bool> DeleteAsync(Device device) => Task.FromResult(true);
    public Task SaveChangesAsync() => Task.CompletedTask;
}

// records the per-device minute notifications the service pushes
internal sealed class FakeRealTimeStream : IRealTimeDataStreamService
{
    public readonly List<(Guid OrgId, Guid? DeviceId)> MinuteNotifications = new();

    public Task NotifyMinuteAggregateUpdated(Guid orgId, AggregateMinuteEnergy a)
    {
        MinuteNotifications.Add((orgId, a.DeviceId));
        return Task.CompletedTask;
    }

    public readonly List<Guid> HistoricalNotifications = new();

    public Task SubscribeToChart(string c, Guid o, string r) => Task.CompletedTask;
    public Task UnsubscribeFromChart(string c, Guid o, string r) => Task.CompletedTask;
    public Task SubscribeToLive(string c, Guid o) => Task.CompletedTask;
    public Task UnsubscribeFromLive(string c, Guid o) => Task.CompletedTask;
    public Task SubscribeToHistorical(string c, Guid o) => Task.CompletedTask;
    public Task UnsubscribeFromHistorical(string c, Guid o) => Task.CompletedTask;
    public Task NotifyHistoricalUpdatedAsync(Guid orgId)
    {
        HistoricalNotifications.Add(orgId);
        return Task.CompletedTask;
    }
    public Task NotifyHourAggregateUpdated(Guid orgId, AggregateHourEnergy a) => Task.CompletedTask;
    public Task NotifyDayAggregateUpdated(Guid orgId, AggregateDayEnergy a) => Task.CompletedTask;
    public Task NotifyMonthAggregateUpdated(Guid orgId, AggregateMonthEnergy a) => Task.CompletedTask;
    public Task NotifyLiveTickAsync(Guid orgId, IEnumerable<EnergyReading> orgReadings, decimal ratePerKwh) => Task.CompletedTask;
}

internal sealed class NoopLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) { }
}
