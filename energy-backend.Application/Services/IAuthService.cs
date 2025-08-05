using energy_backend.Entities;
using energy_backend.Models;

namespace energy_backend.Services
{
    public interface IAuthService
    {
        Task<User?> RegisterAsync(UserRequestDto request);
        Task<LoginResponseDto?> LoginAsync(UserRequestDto request);
        Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto requestDto);
    }
}
