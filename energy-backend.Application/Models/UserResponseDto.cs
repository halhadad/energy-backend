using energy_backend.Entities;

namespace energy_backend.Models
{
    public class UserResponseDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

    }
}
