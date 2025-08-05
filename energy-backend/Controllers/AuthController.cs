using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using energy_backend.Entities;
using energy_backend.Models;
using energy_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace energy_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService) : ControllerBase
    {


        [HttpPost]
        [Route("register")]
        public async Task<ActionResult<User>> Register(UserRequestDto request)
        {
            var user = await authService.RegisterAsync(request);
            if (user is null)
            {
                return BadRequest("Username already exists");
            }
            return Ok(user);
        }

        [HttpPost]
        [Route("login")]
        public async Task<ActionResult<TokenResponseDto>> Login(UserRequestDto request)
        {
            var response = await authService.LoginAsync(request);
            if (response is null)
            {
                return BadRequest("Invalid credentials");
            }

            return Ok(response);

        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
        {
            var response = await authService.RefreshTokenAsync(request);
            if (response is null || response.AccessToken is null || response.RefreshToken is null)
            {
                return Unauthorized("Invalid refresh token");
            }
            return Ok(response);
        }

        [Authorize]
        [HttpGet]
        public IActionResult AuthenticatedOnlyEndpoint()
        {
            // This endpoint is only accessible to authenticated users
            return Ok("You are authenticated!");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin-only")]
        public IActionResult AdminOnlyEndpoint()
        {
            // This endpoint is only accessible to authenticated admins
            return Ok("You are in an admin only room!");
        }

    }
}
