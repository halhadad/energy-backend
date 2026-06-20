using energy_backend.Application.Common;
using energy_backend.Application.Services;
using energy_backend.Core.Entities;
using energy_backend.Core.Projections;
using energy_backend.Core.Services;
using energy_backend.Tests;

var runner = new TestRunner();

// EnergyCalculator
runner.Test("CalculateKwhFromWatts: 1000 W over 3600 s = 1 kWh", () =>
{
    var kwh = EnergyCalculator.CalculateKwhFromWatts(1000d, 3600d);
    Assert.Near(1.0, kwh);
});

runner.Test("CalculateEstimatedCost: 10 kWh at 0.28 = 2.80, rounded to 4dp", () =>
{
    var cost = EnergyCalculator.CalculateEstimatedCost(10d, 0.28m);
    Assert.Equal(2.8000m, cost);
});

// UpsertBucketsAsync
runner.Test("UpsertBucketsAsync writes the MINUTE tier only (downsampler owns the rest)", async () =>
{
    var (svc, aggregates, _, _) = BuildService();
    await svc.UpsertBucketsAsync(NewReading(activeEnergyKwh: 0.0042));

    Assert.Equal(1, aggregates.MinuteUpserts);
    Assert.Equal(0, aggregates.HourUpserts);
    Assert.Equal(0, aggregates.DayUpserts);
    Assert.Equal(0, aggregates.MonthUpserts);
});

runner.Test("UpsertBucketsAsync uses reading.ActiveEnergyKwh directly (no recompute)", async () =>
{
    var (svc, aggregates, _, slot) = BuildService();
    await svc.UpsertBucketsAsync(NewReading(activeEnergyKwh: 0.0042));

    var row = aggregates.Minutes.Values.Single();
    Assert.Near(0.0042, row.TotalActiveEnergyKwh);
    Assert.Equal(1, row.DataPointsCount);
});

runner.Test("UpsertBucketsAsync floors the reading timestamp to the minute boundary", async () =>
{
    var (svc, aggregates, _, _) = BuildService();
    var reading = NewReading(activeEnergyKwh: 0.001);
    reading.Timestamp = new DateTime(2026, 6, 18, 14, 37, 42, DateTimeKind.Utc);

    await svc.UpsertBucketsAsync(reading);

    var row = aggregates.Minutes.Values.Single();
    Assert.Equal(new DateTime(2026, 6, 18, 14, 37, 0, DateTimeKind.Utc), row.Timestamp);
});

// NotifyOrgAsync
runner.Test("NotifyOrgAsync pushes one minute update PER device (not just the first)", async () =>
{
    var (svc, aggregates, stream, slot) = BuildService();
    var d1 = Guid.NewGuid();
    var d2 = Guid.NewGuid();
    aggregates.Minutes[(d1, slot)] = MinuteRow(d1, slot);
    aggregates.Minutes[(d2, slot)] = MinuteRow(d2, slot);

    await svc.NotifyOrgAsync(OrgId, [d1, d2], slot);

    Assert.Equal(2, stream.MinuteNotifications.Count);
    Assert.True(stream.MinuteNotifications.Any(n => n.DeviceId == d1), "device 1 notified");
    Assert.True(stream.MinuteNotifications.Any(n => n.DeviceId == d2), "device 2 notified");
});

runner.Test("NotifyOrgAsync skips devices that have no bucket for the slot", async () =>
{
    var (svc, aggregates, stream, slot) = BuildService();
    var present = Guid.NewGuid();
    var missing = Guid.NewGuid();
    aggregates.Minutes[(present, slot)] = MinuteRow(present, slot);

    await svc.NotifyOrgAsync(OrgId, [present, missing], slot);

    Assert.Equal(1, stream.MinuteNotifications.Count);
    Assert.Equal(present, stream.MinuteNotifications[0].DeviceId!.Value);
});

runner.Test("NotifyOrgAsync signals the historical page only on a minute boundary", async () =>
{
    var (svc, aggregates, stream, slot) = BuildService();        // slot is at :00 seconds
    var d = Guid.NewGuid();
    aggregates.Minutes[(d, slot)] = MinuteRow(d, slot);
    aggregates.Minutes[(d, slot.AddSeconds(5))] = MinuteRow(d, slot.AddSeconds(5));

    await svc.NotifyOrgAsync(OrgId, [d], slot.AddSeconds(5));    // mid-minute, no signal
    Assert.Equal(0, stream.HistoricalNotifications.Count);

    await svc.NotifyOrgAsync(OrgId, [d], slot);                  // on the minute, one signal
    Assert.Equal(1, stream.HistoricalNotifications.Count);
});

// multi-device aggregation
runner.Test("MetricAggregation.ByTimestamp collapses 2 devices into one org row per timestamp", () =>
{
    var ts = new DateTime(2026, 6, 18, 14, 0, 0, DateTimeKind.Utc);
    var rows = new List<AggregateMetricRow>
    {
        // device A: 1 kWh, 100 W avg over 10 points, min 80 max 120
        new(ts, 1.0, 100, 80, 120, 230, 0.4, 0.95, 10),
        // device B: 3 kWh, 200 W avg over 30 points, min 50 max 400
        new(ts, 3.0, 200, 50, 400, 240, 1.2, 0.90, 30),
    };

    var merged = MetricAggregation.ByTimestamp(rows);

    Assert.Equal(1, merged.Count);
    Assert.Near(4.0, merged[0].TotalEnergyKwh);             // summed
    Assert.Equal(40, merged[0].DataPointsCount);            // summed
    Assert.Near(175.0, merged[0].AverageActivePowerWatts);  // (100*10 + 200*30)/40
    Assert.Near(50, merged[0].MinActivePowerWatts);         // min across devices
    Assert.Near(400, merged[0].MaxActivePowerWatts);        // max across devices
});

// analytics summary math
runner.Test("AnalyticsMath.Percent guards divide-by-zero", () =>
{
    Assert.Near(25.0, AnalyticsMath.Percent(50, 200));
    Assert.Near(0.0, AnalyticsMath.Percent(50, 0));
});

runner.Test("AnalyticsMath.ProjectMonth scales month-to-date to the full month", () =>
    Assert.Near(300.0, AnalyticsMath.ProjectMonth(100.0, 10.0, 30)));

runner.Test("AnalyticsMath.ChangePercent computes period-over-period delta", () =>
{
    Assert.Near(20.0, AnalyticsMath.ChangePercent(120, 100));
    Assert.Near(-50.0, AnalyticsMath.ChangePercent(50, 100));
    Assert.Near(0.0, AnalyticsMath.ChangePercent(50, 0));
});

return runner.Report();

partial class Program
{
    static readonly Guid OrgId = Guid.NewGuid();
    static readonly Guid DeviceId = Guid.NewGuid();

    static (MockDataAggregationService Svc, FakeAggregateRepository Aggregates, FakeRealTimeStream Stream, DateTime Slot)
        BuildService()
    {
        var aggregates = new FakeAggregateRepository();
        var devices = new FakeDeviceRepository();
        devices.Add(new Device { DeviceId = DeviceId, OrganisationId = OrgId, Name = "Test", RatedPowerWatts = 100 });
        var stream = new FakeRealTimeStream();
        var svc = new MockDataAggregationService(aggregates, devices, stream, new NoopLogger<MockDataAggregationService>());
        var slot = new DateTime(2026, 6, 18, 14, 0, 0, DateTimeKind.Utc);
        return (svc, aggregates, stream, slot);
    }

    static EnergyReading NewReading(double activeEnergyKwh) => new()
    {
        EnergyReadingId = Guid.NewGuid(),
        OrgId = OrgId,
        DeviceId = DeviceId,
        Timestamp = new DateTime(2026, 6, 18, 14, 0, 0, DateTimeKind.Utc),
        ActivePowerWatts = 100,
        VoltageVolts = 230,
        CurrentAmps = 0.45,
        PowerFactor = 0.95,
        ActiveEnergyKwh = activeEnergyKwh
    };

    static AggregateMinuteEnergy MinuteRow(Guid deviceId, DateTime slot) => new()
    {
        Id = Guid.NewGuid(),
        OrgId = OrgId,
        DeviceId = deviceId,
        Timestamp = slot,
        TotalActiveEnergyKwh = 0.01,
        AverageActivePowerWatts = 120,
        DataPointsCount = 12
    };
}
