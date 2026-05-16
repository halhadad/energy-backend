using energy_backend.Application.Models;

namespace energy_backend.Application.Services
{
    public enum AggregationLevel
    {
        Minute,
        Hour,
        Day,
        Month
    }

    public interface IAggregationService
    {
        Task<AggregationResultDto> GetSnapshotAsync(AggregationRequestDto request);
    }
}