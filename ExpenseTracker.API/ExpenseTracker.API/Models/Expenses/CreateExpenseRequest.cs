using System.ComponentModel.DataAnnotations;
using ExpenseTracker.API.Models.Entities;

namespace ExpenseTracker.API.Models.Expenses
{
    public class CreateExpenseRequest
    {
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        // Optional: enforce 3-letter currency codes
        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string CurrencyCode { get; set; } = "CAD";

        [Required]
        public ExpenseCategory Category { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        // Let the client optionally pass when it happened; if null, we’ll default it
        public DateTime? IncurredAtUtc { get; set; }
    }
}
