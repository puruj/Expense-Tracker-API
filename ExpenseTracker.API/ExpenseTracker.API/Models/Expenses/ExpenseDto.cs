using ExpenseTracker.API.Models.Entities;

namespace ExpenseTracker.API.Models.Expenses
{
    public class ExpenseDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = "CAD";
        public ExpenseCategory Category { get; set; }
        public string? Description { get; set; }
        public DateTime IncurredAtUtc { get; set; }
    }
}
