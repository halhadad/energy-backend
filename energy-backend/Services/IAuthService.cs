using energy_backend.Entities;
using energy_backend.Models;

namespace energy_backend.Services
{
    public interface IAuthService
    {
        Task<User?> RegisterAsync(UserDTO request);
        Task<TokenResponseDto?> LoginAsync(UserDTO request);
        Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto requestDto);
    }
}
