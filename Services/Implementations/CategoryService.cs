using Microsoft.EntityFrameworkCore;
using SecureAuthApi.Data;
using SecureAuthApi.Models;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _db;
    public CategoryService(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<Category>> GetAllAsync() =>
        await _db.Categories.ToListAsync();

    public async Task<Category> GetByIdAsync(int id) =>
        await _db.Categories.FindAsync(id);

    public async Task<Category> CreateAsync(Category category)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task UpdateAsync(Category category)
    {
        _db.Entry(category).State = EntityState.Modified;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat != null)
        {
            _db.Categories.Remove(cat);
            await _db.SaveChangesAsync();
        }
    }
}
