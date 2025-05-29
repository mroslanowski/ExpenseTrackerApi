using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureAuthApi.Models;
using SecureAuthApi.Models.Dtos;
using System.ComponentModel.DataAnnotations;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _svc;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(ICategoryService svc, ILogger<CategoriesController> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _svc.GetAllAsync();
        var dtos = categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name
        });
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var cat = await _svc.GetByIdAsync(id);
        if (cat == null) return NotFound();
        
        var dto = new CategoryDto
        {
            Id = cat.Id,
            Name = cat.Name
        };
        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CategoryDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var category = new Category
            {
                Name = dto.Name
            };

            var created = await _svc.CreateAsync(category);
            var responseDto = new CategoryDto
            {
                Id = created.Id,
                Name = created.Name
            };
            return CreatedAtAction(nameof(Get), new { id = created.Id }, responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, CategoryDto dto)
    {
        if (id != dto.Id) return BadRequest();
        
        var category = new Category
        {
            Id = dto.Id,
            Name = dto.Name
        };
        
        await _svc.UpdateAsync(category);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(id);
        return NoContent();
    }
}
