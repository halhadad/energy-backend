namespace energy_backend.Models
{
    public class LoginResponseDto
    {
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
        public UserResponseDto User { get; set; } = default!;
    }
}
