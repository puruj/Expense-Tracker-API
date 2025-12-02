namespace ExpenseTracker.API.Models.Entities
{
    public class Expense
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = "CAD";

        public ExpenseCategory Category { get; set; }

        public string? Description { get; set; }
        // When the expense actually happened (used for filters)
        public DateTime IncurredAtUtc { get; set; }

        // Metadata for the record itself
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }


        public User User { get; set; } = null!;
    }
}
