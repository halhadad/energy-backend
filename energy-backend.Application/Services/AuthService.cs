using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using energy_backend.Application.Interfaces;
using energy_backend.Application.Models;
using energy_backend.Core.Entities;
using energy_backend.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
namespace energy_backend.Application.Services;

public class AuthService(
    IUserRepository userRepository,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;
    public async Task<LoginResponseDto?> LoginAsync(UserRequestDto request)
    {
        var user = await userRepository.GetByUsernameAsync(request.Username);
        if (user is null) return null;
        var hasher = new PasswordHasher<User>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result != PasswordVerificationResult.Success) return null;
        return new LoginResponseDto
        {
            AccessToken = CreateToken(user),
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
            // FIX: Role was never set — every JWT got an empty role claim.
            // Default all new users to "User"; promote via admin endpoint as needed.
            Role = "User"
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password);
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
            AccessToken = CreateToken(user),
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
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await userRepository.SaveChangesAsync();
        return token;
    }
    private string CreateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Role, user.Role)
        };
        var key = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(_jwt.Token));
        var tokenDescriptor = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature));
        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }
}