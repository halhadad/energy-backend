using energy_backend.Application.Interfaces;

namespace energy_backend.Api.Services;

public class AppEnvironment(IHostEnvironment environment) : IAppEnvironment
{
    public bool IsDevelopment => environment.IsDevelopment();
}
