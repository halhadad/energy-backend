namespace energy_backend.Core.Interfaces;

public interface IEnergyAggregate
{
    Guid Id { get; set; }
    Guid OrgId { get; set; }
    Guid? DeviceId { get; set; }
    DateTime Timestamp { get; set; }
    float TotalActiveEnergyKwh { get; set; }
    float AverageActivePowerWatts { get; set; }
    float MinActivePowerWatts { get; set; }
    float MaxActivePowerWatts { get; set; }
    float AverageVoltageVolts { get; set; }
    float AverageCurrentAmps { get; set; }
    float AveragePowerFactor { get; set; }
    int DataPointsCount { get; set; }
}