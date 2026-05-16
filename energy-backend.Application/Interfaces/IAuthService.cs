using energy_backend.Core.Entities;
using energy_backend.Models;

namespace energy_backend.Application.Services
{
    public interface IAuthService
    {
        Task<User?> RegisterAsync(UserRequestDto request);
        Task<LoginResponseDto?> LoginAsync(UserRequestDto request);
        Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto requestDto);
    }
}
