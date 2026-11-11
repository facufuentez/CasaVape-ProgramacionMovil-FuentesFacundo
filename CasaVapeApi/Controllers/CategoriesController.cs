using CasaVape.Core.Data;
using CasaVape.Core.DTOs;
using CasaVape.Core.Models.Entities;
using CasaVape.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CasaVapeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly AuditoriaService _auditoria;

    public CategoriesController(ApplicationDbContext context, AuditoriaService auditoria)
    {
        _context = context;
        _auditoria = auditoria;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> Get()
    {
        var categories = await _context.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoryDto
            {
                IdCategoria = c.IdCategoria,
                Nombre = c.Nombre,
                Descripcion = c.Descripcion
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return BadRequest(new { error = "El nombre es obligatorio." });
        }

        var category = new Categoria
        {
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion
        };

        _context.Categorias.Add(category);
        await _context.SaveChangesAsync();

        await _auditoria.RegistrarAsync("Crear", "Categoria", category.IdCategoria.ToString(), null, new
        {
            category.Nombre,
            category.Descripcion
        });

        return CreatedAtAction(nameof(Get), new { id = category.IdCategoria }, new CategoryDto
        {
            IdCategoria = category.IdCategoria,
            Nombre = category.Nombre,
            Descripcion = category.Descripcion
        });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<CategoryDto>> Update(int id, CreateCategoryRequest request)
    {
        var category = await _context.Categorias.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        var before = new { category.Nombre, category.Descripcion };

        category.Nombre = request.Nombre.Trim();
        category.Descripcion = request.Descripcion;

        await _context.SaveChangesAsync();

        await _auditoria.RegistrarAsync("Editar", "Categoria", id.ToString(), before, new
        {
            category.Nombre,
            category.Descripcion
        });

        return Ok(new CategoryDto
        {
            IdCategoria = category.IdCategoria,
            Nombre = category.Nombre,
            Descripcion = category.Descripcion
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.Categorias.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        var hasProducts = await _context.Productos.AnyAsync(p => p.IdCategoria == id);
        if (hasProducts)
        {
            return BadRequest(new { error = "No se puede eliminar la categoría porque tiene productos asociados." });
        }

        _context.Categorias.Remove(category);
        await _context.SaveChangesAsync();

        await _auditoria.RegistrarAsync("Eliminar", "Categoria", id.ToString(), new
        {
            category.Nombre,
            category.Descripcion
        }, null);

        return NoContent();
    }
}
