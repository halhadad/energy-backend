using energy_backend.Application.Models;

namespace energy_backend.Application.Interfaces;

public interface IAuthService
{
    Task<UserResponseDto?> RegisterAsync(UserRequestDto request);
    Task<LoginResponseDto?> LoginAsync(UserRequestDto request);
    Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto requestDto);
}
