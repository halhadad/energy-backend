namespace energy_backend.Application.Models.Projections;

public record AggregateMetricRow(
    DateTime Timestamp,
    float TotalEnergyKwh,
    float AverageActivePowerWatts,
    float MinActivePowerWatts,
    float MaxActivePowerWatts,
    float AverageVoltageVolts,
    float AverageCurrentAmps,
    float AveragePowerFactor,
    int DataPointsCount);


public record DeviceSnapshotRow(
    DateTime Timestamp,
    string DeviceName,
    float TotalEnergyKwh);


public record MinuteRollupProjection(
    Guid OrgId,
    Guid DeviceId,
    DateTime MinuteSlot,
    float TotalEnergyKwh,
    float AvgWatts,
    float MinWatts,
    float MaxWatts,
    float AvgVoltage,
    float AvgCurrent,
    float AvgPowerFactor,
    int Count);