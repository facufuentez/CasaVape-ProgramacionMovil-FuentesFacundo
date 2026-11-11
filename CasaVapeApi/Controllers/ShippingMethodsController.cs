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
public class ShippingMethodsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly AuditoriaService _auditoria;

    public ShippingMethodsController(ApplicationDbContext context, AuditoriaService auditoria)
    {
        _context = context;
        _auditoria = auditoria;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ShippingMethodDto>>> Get()
    {
        var methods = await _context.MetodosEntrega
            .AsNoTracking()
            .OrderBy(m => m.Nombre)
            .Select(m => new ShippingMethodDto
            {
                Id = m.IdMetodoEntrega,
                Nombre = m.Nombre,
                Costo = m.Costo
            })
            .ToListAsync();

        return Ok(methods);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<ShippingMethodDto>> Create(CreateShippingMethodRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return BadRequest(new { error = "El nombre es obligatorio." });
        }

        var method = new MetodoEntrega
        {
            Nombre = request.Nombre.Trim(),
            Costo = request.Costo
        };

        _context.MetodosEntrega.Add(method);
        await _context.SaveChangesAsync();

        await _auditoria.RegistrarAsync("Crear", "MetodoEntrega", method.IdMetodoEntrega.ToString(), null, new
        {
            method.Nombre,
            method.Costo
        });

        return CreatedAtAction(nameof(Get), new { id = method.IdMetodoEntrega }, new ShippingMethodDto
        {
            Id = method.IdMetodoEntrega,
            Nombre = method.Nombre,
            Costo = method.Costo
        });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<ShippingMethodDto>> Update(int id, CreateShippingMethodRequest request)
    {
        var method = await _context.MetodosEntrega.FindAsync(id);
        if (method is null)
        {
            return NotFound();
        }

        var before = new { method.Nombre, method.Costo };

        method.Nombre = request.Nombre.Trim();
        method.Costo = request.Costo;

        await _context.SaveChangesAsync();

        await _auditoria.RegistrarAsync("Editar", "MetodoEntrega", id.ToString(), before, new
        {
            method.Nombre,
            method.Costo
        });

        return Ok(new ShippingMethodDto
        {
            Id = method.IdMetodoEntrega,
            Nombre = method.Nombre,
            Costo = method.Costo
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(int id)
    {
        var method = await _context.MetodosEntrega.FindAsync(id);
        if (method is null)
        {
            return NotFound();
        }

        _context.MetodosEntrega.Remove(method);
        await _context.SaveChangesAsync();

        await _auditoria.RegistrarAsync("Eliminar", "MetodoEntrega", id.ToString(), new
        {
            method.Nombre,
            method.Costo
        }, null);

        return NoContent();
    }
}
