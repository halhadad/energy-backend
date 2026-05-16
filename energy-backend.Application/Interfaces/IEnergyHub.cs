namespace energy_backend.Application.Services
{
    /// <summary>
    /// Marker interface so Infrastructure can reference IHubContext<IEnergyHub>
    /// without depending on the concrete UnifiedHub in the API layer.
    /// </summary>
    public interface IEnergyHub
    {
    }
}