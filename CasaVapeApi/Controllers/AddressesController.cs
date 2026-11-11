using CasaVape.Core.Data;
using CasaVape.Core.DTOs;
using CasaVape.Core.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CasaVapeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AddressesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AddressesController(ApplicationDbContext context)
    {
        _context = context;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AddressDto>>> Get()
    {
        var addresses = await _context.Direcciones
            .AsNoTracking()
            .Where(d => d.IdUsuario == UserId && d.Activa)
            .Include(d => d.Provincia)
            .OrderBy(d => d.Ciudad)
            .Select(d => new AddressDto
            {
                IdDireccion = d.IdDireccion,
                IdProvincia = d.IdProvincia,
                Provincia = d.Provincia != null ? d.Provincia.Nombre : string.Empty,
                Calle = d.Calle,
                Numero = d.Numero,
                Ciudad = d.Ciudad,
                CodigoPostal = d.CodigoPostal,
                Activa = d.Activa
            })
            .ToListAsync();

        return Ok(addresses);
    }

    [HttpPost]
    public async Task<ActionResult<AddressDto>> Create(CreateAddressRequest request)
    {
        var province = await _context.Provincias.FindAsync(request.IdProvincia);
        if (province is null)
        {
            return BadRequest(new { error = "La provincia no existe." });
        }

        var address = new Direccion
        {
            IdUsuario = UserId,
            IdProvincia = request.IdProvincia,
            Calle = request.Calle,
            Numero = request.Numero,
            Ciudad = request.Ciudad,
            CodigoPostal = request.CodigoPostal,
            Activa = true
        };

        _context.Direcciones.Add(address);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = address.IdDireccion }, new AddressDto
        {
            IdDireccion = address.IdDireccion,
            IdProvincia = address.IdProvincia,
            Provincia = province.Nombre,
            Calle = address.Calle,
            Numero = address.Numero,
            Ciudad = address.Ciudad,
            CodigoPostal = address.CodigoPostal,
            Activa = address.Activa
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AddressDto>> Update(int id, CreateAddressRequest request)
    {
        var address = await _context.Direcciones
            .FirstOrDefaultAsync(d => d.IdDireccion == id && d.IdUsuario == UserId);

        if (address is null)
        {
            return NotFound();
        }

        var province = await _context.Provincias.FindAsync(request.IdProvincia);
        if (province is null)
        {
            return BadRequest(new { error = "La provincia no existe." });
        }

        address.IdProvincia = request.IdProvincia;
        address.Calle = request.Calle;
        address.Numero = request.Numero;
        address.Ciudad = request.Ciudad;
        address.CodigoPostal = request.CodigoPostal;

        await _context.SaveChangesAsync();

        return Ok(new AddressDto
        {
            IdDireccion = address.IdDireccion,
            IdProvincia = address.IdProvincia,
            Provincia = province.Nombre,
            Calle = address.Calle,
            Numero = address.Numero,
            Ciudad = address.Ciudad,
            CodigoPostal = address.CodigoPostal,
            Activa = address.Activa
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var address = await _context.Direcciones
            .FirstOrDefaultAsync(d => d.IdDireccion == id && d.IdUsuario == UserId);

        if (address is null)
        {
            return NotFound();
        }

        address.Activa = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("provinces")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<object>>> GetProvinces()
    {
        var provinces = await _context.Provincias
            .AsNoTracking()
            .OrderBy(p => p.Nombre)
            .Select(p => new { p.IdProvincia, p.Nombre })
            .ToListAsync();

        return Ok(provinces);
    }
}
