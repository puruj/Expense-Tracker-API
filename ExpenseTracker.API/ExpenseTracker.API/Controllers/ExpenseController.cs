using ExpenseTracker.API.Models.Entities;
using ExpenseTracker.API.Models.Expenses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ExpenseTracker.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ExpenseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExpenseController(ApplicationDbContext context)
        {
            _context = context;
        }

        private Guid GetUserId()
        {
            // Default mapping: "sub" -> ClaimTypes.NameIdentifier
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub); // fallback if you later disable mapping

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var guid))
            {
                throw new UnauthorizedAccessException("Invalid user id claim.");
            }

            return guid;
        }

        private static ExpenseDto MapToExpenseDto(Expense expense) => new()
        {
            Id = expense.Id,
            Amount = expense.Amount,
            CurrencyCode = expense.CurrencyCode,
            Category = expense.Category,
            Description = expense.Description,
            IncurredAtUtc = expense.IncurredAtUtc
        };

        [HttpPost]
        public async Task<ActionResult<ExpenseDto>> CreateExpense([FromBody] CreateExpenseRequest request)
        {
            var userId = GetUserId();

            var incurredAtUtc = request.IncurredAtUtc?.ToUniversalTime() ?? DateTime.UtcNow;

            var expense = new Expense
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = request.Amount,
                CurrencyCode = request.CurrencyCode,
                Category = request.Category,
                Description = request.Description,
                IncurredAtUtc = incurredAtUtc
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            return Ok(MapToExpenseDto(expense));
        }

        // GET api/Expense/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ExpenseDto>> GetExpenseById(Guid id)
        {
            var userId = GetUserId();

            var expense = await _context.Expenses
                .Where(t => t.UserId == userId && t.Id == id)
                .FirstOrDefaultAsync();

            if (expense == null)
                return NotFound();

            return Ok(MapToExpenseDto(expense));
        }

        // PUT api/Expense/{id}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ExpenseDto>> UpdateExpense(Guid id, [FromBody] CreateExpenseRequest request)
        {
            var userId = GetUserId();

            var expense = await _context.Expenses
                .Where(t => t.UserId == userId && t.Id == id)
                .FirstOrDefaultAsync();

            if (expense == null)
            {
                return NotFound();
            }

            expense.Amount = request.Amount;
            expense.CurrencyCode = request.CurrencyCode;
            expense.Category = request.Category;
            expense.Description = request.Description;
            expense.IncurredAtUtc = request.IncurredAtUtc?.ToUniversalTime() ?? DateTime.UtcNow;
            expense.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(MapToExpenseDto(expense));
        }

        // DELETE api/Expense/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteExpense(Guid id)
        {
            var userId = GetUserId();

            var expense = await _context.Expenses
                .Where(t => t.UserId == userId && t.Id == id)
                .FirstOrDefaultAsync();

            if (expense == null)
            {
                return NotFound();
            }

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET api/expense
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetExpenses(
            [FromQuery] string? period,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var userId = GetUserId();

            var query = _context.Expenses
                .AsNoTracking()
                .Where(e => e.UserId == userId);

            // Predefined ranges: past week / month / 3 months
            if (!string.IsNullOrWhiteSpace(period))
            {
                var now = DateTime.UtcNow;
                DateTime from;

                switch (period.ToLowerInvariant())
                {
                    case "week":
                    case "pastweek":
                        from = now.AddDays(-7);
                        break;

                    case "month":
                    case "pastmonth":
                        from = now.AddMonths(-1);
                        break;

                    case "3months":
                    case "last3months":
                        from = now.AddMonths(-3);
                        break;

                    default:
                        return BadRequest("Invalid period. Use 'week', 'month', or '3months'.");
                }

                query = query.Where(e => e.IncurredAtUtc >= from && e.IncurredAtUtc <= now);
            }

            // Custom date range (overrides period if provided)
            if (startDate.HasValue || endDate.HasValue)
            {
                // normalize to UTC
                var from = startDate?.ToUniversalTime();
                var to = endDate?.ToUniversalTime();

                if (from.HasValue)
                {
                    query = query.Where(e => e.IncurredAtUtc >= from.Value);
                }

                if (to.HasValue)
                {
                    query = query.Where(e => e.IncurredAtUtc <= to.Value);
                }
            }

            // Optional: sort newest first
            query = query.OrderByDescending(e => e.IncurredAtUtc);

            var expenses = await query
                .Select(e => MapToExpenseDto(e))
                .ToListAsync();

            return Ok(expenses);
        }

    }
}
