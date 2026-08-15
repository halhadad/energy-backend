namespace energy_backend.Core.Projections;

public record AggregateMetricRow(
    DateTime Timestamp,
    double TotalEnergyKwh,
    double AverageActivePowerWatts,
    double MinActivePowerWatts,
    double MaxActivePowerWatts,
    double AverageVoltageVolts,
    double AverageCurrentAmps,
    double AveragePowerFactor,
    int DataPointsCount);


public record DeviceSnapshotRow(
    DateTime Timestamp,
    string DeviceName,
    double TotalEnergyKwh);


public record MinuteRollupProjection(
    Guid OrgId,
    Guid DeviceId,
    DateTime MinuteSlot,
    double TotalEnergyKwh,
    double AvgWatts,
    double MinWatts,
    double MaxWatts,
    double AvgVoltage,
    double AvgCurrent,
    double AvgPowerFactor,
    int Count);
