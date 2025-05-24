using SecureAuthApi.Models;
using SecureAuthApi.Models.Dtos;
public interface IExpenseService
{
    Task<IEnumerable<Expense>> GetAllAsync(string userId);
    Task<Expense> GetByIdAsync(int id, string userId);
    Task<Expense> CreateAsync(Expense expense);
    Task UpdateAsync(Expense expense);
    Task DeleteAsync(int id, string userId);

    /// <summary>
    /// Zwraca sumę wszystkich wydatków użytkownika w danym roku i miesiącu.
    /// </summary>
    Task<MonthlyExpenseTotalDto> GetMonthlyTotalAsync(string userId, int year, int month);

    /// <summary>
    /// Zwraca sumy wydatków pogrupowane po kategoriach dla danego roku i miesiąca.
    /// </summary>
    Task<IEnumerable<CategoryExpenseSummaryDto>> GetCategorySummariesAsync(string userId, int year, int month);

}
