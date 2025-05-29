using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureAuthApi.Data;
using SecureAuthApi.Models;
using SecureAuthApi.Models.Dtos;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(ApplicationDbContext context, ILogger<ExpensesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string CurrentUserId
    {
        get
        {
            var claims = User.Claims.ToList();
            _logger.LogInformation("Available claims: {Claims}", string.Join(", ", claims.Select(c => $"{c.Type}: {c.Value}")));
            
            var nameIdentifierClaims = claims.Where(c => c.Type == ClaimTypes.NameIdentifier).ToList();
            _logger.LogInformation("NameIdentifier claims: {Claims}", string.Join(", ", nameIdentifierClaims.Select(c => c.Value)));
            
            var userIdClaim = nameIdentifierClaims.FirstOrDefault(c => !c.Value.Contains("@"));
            _logger.LogInformation("Found userId claim: {Claim}", userIdClaim?.Value);
            
            if (userIdClaim == null)
            {
                _logger.LogError("No valid user ID claim found");
                throw new InvalidOperationException("User ID not found in claims");
            }
            
            return userIdClaim.Value;
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Expense>>> GetExpenses()
    {
        var expenses = await _context.Expenses
            .Include(e => e.Category)
            .Where(e => e.UserId == CurrentUserId)
            .ToListAsync();

        return expenses;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Expense>> GetExpense(int id)
    {
        var expense = await _context.Expenses
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == CurrentUserId);

        if (expense == null)
        {
            return NotFound();
        }

        return expense;
    }

    [HttpPost]
    public async Task<ActionResult<Expense>> CreateExpense(CreateExpenseDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var expense = new Expense
            {
                Amount = dto.Amount,
                Description = dto.Description,
                Date = dto.Date,
                CategoryId = dto.CategoryId,
                UserId = CurrentUserId
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            var createdExpense = await _context.Expenses
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.Id == expense.Id);

            return CreatedAtAction(nameof(GetExpense), new { id = expense.Id }, createdExpense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExpense(int id, UpdateExpenseDto dto)
    {
        if (id != dto.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingExpense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == CurrentUserId);

        if (existingExpense == null)
        {
            return NotFound();
        }

        existingExpense.Amount = dto.Amount;
        existingExpense.Description = dto.Description;
        existingExpense.Date = dto.Date;
        existingExpense.CategoryId = dto.CategoryId;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ExpenseExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == CurrentUserId);

        if (expense == null)
        {
            return NotFound();
        }

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ExpenseExists(int id)
    {
        return _context.Expenses.Any(e => e.Id == id);
    }

    /// <summary>
    /// GET api/Expenses/summary/monthly?year=2025&month=5
    /// </summary>
    [HttpGet("summary/monthly")]
    public async Task<ActionResult<MonthlyExpenseTotalDto>> GetMonthlyTotal(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var total = await _context.Expenses
            .Where(e => e.UserId == CurrentUserId && e.Date >= startDate && e.Date <= endDate)
            .SumAsync(e => e.Amount);

        var dto = new MonthlyExpenseTotalDto
        {
            Year = year,
            Month = month,
            TotalAmount = total
        };

        return Ok(dto);
    }

    /// <summary>
    /// GET api/Expenses/summary/categories?year=2025&month=5
    /// </summary>
    [HttpGet("summary/categories")]
    public async Task<ActionResult<IEnumerable<CategoryExpenseSummaryDto>>> GetCategorySummary(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var summaries = await _context.Expenses
            .Where(e => e.UserId == CurrentUserId && e.Date >= startDate && e.Date <= endDate)
            .GroupBy(e => new { e.CategoryId, e.Category.Name })
            .Select(g => new CategoryExpenseSummaryDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                TotalAmount = g.Sum(e => e.Amount)
            })
            .ToListAsync();

        return Ok(summaries);
    }
}
