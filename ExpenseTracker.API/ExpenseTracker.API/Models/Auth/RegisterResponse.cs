using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.API.Models.Auth
{
    public class RegisterResponse
    {
        [Required]
        public AuthUserDto User { get; set; } = new();

        public DateTime CreatedAt { get; set; }
    }
}
