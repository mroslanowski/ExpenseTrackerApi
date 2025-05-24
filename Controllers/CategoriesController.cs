using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureAuthApi.Models;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _svc;
    public CategoriesController(ICategoryService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _svc.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var cat = await _svc.GetByIdAsync(id);
        return cat == null ? NotFound() : Ok(cat);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Category cat)
    {
        var created = await _svc.CreateAsync(cat);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Category cat)
    {
        if (id != cat.Id) return BadRequest();
        await _svc.UpdateAsync(cat);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(id);
        return NoContent();
    }
}
