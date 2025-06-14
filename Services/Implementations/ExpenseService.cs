﻿using Microsoft.EntityFrameworkCore;
using SecureAuthApi.Data;
using SecureAuthApi.Models;
using SecureAuthApi.Models.Dtos;
using Microsoft.Extensions.Logging;

public class ExpenseService : IExpenseService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(ApplicationDbContext db, ILogger<ExpenseService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<Expense>> GetAllAsync(string userId) =>
        await _db.Expenses
                 .Include(e => e.Category)
                 .Where(e => e.UserId == userId)
                 .ToListAsync();

    public async Task<Expense> GetByIdAsync(int id, string userId) =>
        await _db.Expenses
                 .Include(e => e.Category)
                 .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

    public async Task<Expense> CreateAsync(Expense expense)
    {
        try
        {
            _logger.LogInformation("Adding expense to database: {@Expense}", expense);
            _db.Expenses.Add(expense);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Expense saved successfully with ID: {Id}", expense.Id);
            return expense;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving expense to database: {@Expense}", expense);
            throw;
        }
    }

    public async Task UpdateAsync(Expense expense)
    {
        _db.Entry(expense).State = EntityState.Modified;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, string userId)
    {
        var exp = await _db.Expenses
                           .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (exp != null)
        {
            _db.Expenses.Remove(exp);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<MonthlyExpenseTotalDto> GetMonthlyTotalAsync(string userId, int year, int month)
    {
        var total = await _db.Expenses
            .Where(e => e.UserId == userId
                     && e.Date.Year == year
                     && e.Date.Month == month)
            .SumAsync(e => e.Amount);

        return new MonthlyExpenseTotalDto
        {
            Year = year,
            Month = month,
            TotalAmount = total
        };
    }

    public async Task<IEnumerable<CategoryExpenseSummaryDto>> GetCategorySummariesAsync(string userId, int year, int month)
    {
        var query = _db.Expenses
            .Include(e => e.Category)
            .Where(e => e.UserId == userId
                     && e.Date.Year == year
                     && e.Date.Month == month);

        var result = await query
            .GroupBy(e => new { e.CategoryId, e.Category.Name })
            .Select(g => new CategoryExpenseSummaryDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                TotalAmount = g.Sum(e => e.Amount)
            })
            .ToListAsync();

        return result;
    }
}
