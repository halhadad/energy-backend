using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using energy_backend.Application.Configuration;
using energy_backend.Application.Interfaces;
using energy_backend.Application.Models;
using energy_backend.Core.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace energy_backend.Infrastructure.Services;

public class JwtTokenService(IOptions<JwtOptions> jwtOptions, AuthOptions authOptions) : ITokenService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public string CreateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Token));
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(authOptions.AccessTokenLifetimeHours),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
