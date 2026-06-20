namespace energy_backend.Core.Interfaces;

public interface IEnergyAggregate
{
    Guid Id { get; set; }
    Guid OrgId { get; set; }
    Guid? DeviceId { get; set; }
    DateTime Timestamp { get; set; }
    double TotalActiveEnergyKwh { get; set; }
    double AverageActivePowerWatts { get; set; }
    double MinActivePowerWatts { get; set; }
    double MaxActivePowerWatts { get; set; }
    double AverageVoltageVolts { get; set; }
    double AverageCurrentAmps { get; set; }
    double AveragePowerFactor { get; set; }
    int DataPointsCount { get; set; }
}
