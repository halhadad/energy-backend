using energy_backend.Core.Entities;

namespace energy_backend.Application.Interfaces;

public interface ITokenService
{
    string CreateAccessToken(User user);
}
