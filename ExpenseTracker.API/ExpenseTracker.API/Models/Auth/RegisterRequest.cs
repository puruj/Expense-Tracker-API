using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.API.Models.Auth
{
    public class RegisterRequest
    {
        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(64, MinimumLength = 8,
            ErrorMessage = "Password must be between 8 and 64 characters long.")]
        public string Password { get; set; } = string.Empty;
    }
}
