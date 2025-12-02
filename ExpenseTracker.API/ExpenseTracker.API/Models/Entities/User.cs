using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.API.Models.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;
        [Required]
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
        [Required]
        public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    }
}
