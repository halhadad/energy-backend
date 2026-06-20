using System.Security.Cryptography;
using energy_backend.Application.Configuration;
using energy_backend.Application.Interfaces;
using energy_backend.Application.Models;
using energy_backend.Core.Entities;
using energy_backend.Core.Interfaces;

namespace energy_backend.Application.Services;

public class AuthService(
    IUserRepository userRepository,
    ITokenService tokenService,
    IPasswordHasher passwordHasher,
    AuthOptions authOptions) : IAuthService
{
    public async Task<LoginResponseDto?> LoginAsync(UserRequestDto request)
    {
        var user = await userRepository.GetByUsernameAsync(request.Username);
        if (user is null) return null;
        if (!passwordHasher.Verify(user.PasswordHash, request.Password)) return null;

        return new LoginResponseDto
        {
            AccessToken = tokenService.CreateAccessToken(user),
            RefreshToken = await GenerateAndSaveRefreshTokenAsync(user),
            User = new UserResponseDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role
            }
        };
    }

    public async Task<UserResponseDto?> RegisterAsync(UserRequestDto request)
    {
        if (await userRepository.ExistsAsync(request.Username)) return null;
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            Role = "User"
        };
        user.PasswordHash = passwordHasher.Hash(request.Password);
        await userRepository.AddAsync(user);
        await userRepository.SaveChangesAsync();
        return new UserResponseDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Role = user.Role
        };
    }

    public async Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        var user = await userRepository.GetByIdAsync(request.UserId);
        if (user is null
            || user.RefreshToken != request.RefreshToken
            || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return null;
        }
        return new TokenResponseDto
        {
            AccessToken = tokenService.CreateAccessToken(user),
            RefreshToken = await GenerateAndSaveRefreshTokenAsync(user)
        };
    }

    private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var token = Convert.ToBase64String(randomNumber);
        user.RefreshToken = token;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(authOptions.RefreshTokenLifetimeDays);
        await userRepository.SaveChangesAsync();
        return token;
    }
}
