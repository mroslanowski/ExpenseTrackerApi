using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureAuthApi.Models;
using SecureAuthApi.Models.Dtos;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _svc;
    public ExpensesController(IExpenseService svc) => _svc = svc;

    private string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _svc.GetAllAsync(CurrentUserId));

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var exp = await _svc.GetByIdAsync(id, CurrentUserId);
        return exp == null ? NotFound() : Ok(exp);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Expense exp)
    {
        exp.UserId = CurrentUserId;
        var created = await _svc.CreateAsync(exp);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Expense exp)
    {
        if (id != exp.Id) return BadRequest();
        exp.UserId = CurrentUserId;
        await _svc.UpdateAsync(exp);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(id, CurrentUserId);
        return NoContent();
    }

    /// <summary>
    /// GET api/Expenses/summary/monthly?year=2025&month=5
    /// </summary>
    [HttpGet("summary/monthly")]
    public async Task<ActionResult<MonthlyExpenseTotalDto>> GetMonthlyTotal(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var dto = await _svc.GetMonthlyTotalAsync(CurrentUserId, year, month);
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
        var list = await _svc.GetCategorySummariesAsync(CurrentUserId, year, month);
        return Ok(list);
    }
}
