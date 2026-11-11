using CasaVape.Core.Data;
using CasaVape.Core.DTOs;
using CasaVape.Core.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CasaVapeApi.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Administrador")]
public class AuditsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuditsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditDto>>> Get(
        string? usuario,
        string? entidad,
        string? accion,
        DateTime? desde,
        DateTime? hasta,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.Auditorias
            .AsNoTracking()
            .Include(a => a.Usuario)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(usuario))
        {
            query = query.Where(a => a.IdUsuario == usuario || (a.Usuario != null && a.Usuario.Email == usuario));
        }

        if (!string.IsNullOrWhiteSpace(entidad))
        {
            query = query.Where(a => a.Entidad == entidad);
        }

        if (!string.IsNullOrWhiteSpace(accion))
        {
            query = query.Where(a => a.Accion == accion);
        }

        if (desde.HasValue)
        {
            query = query.Where(a => a.Fecha >= desde.Value.Date);
        }

        if (hasta.HasValue)
        {
            var limit = hasta.Value.Date.AddDays(1);
            query = query.Where(a => a.Fecha < limit);
        }

        var audits = await query
            .OrderByDescending(a => a.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditDto
            {
                IdAuditoria = a.IdAuditoria,
                Fecha = a.Fecha,
                IdUsuario = a.IdUsuario,
                Usuario = a.Usuario == null ? null : $"{a.Usuario.Nombre} {a.Usuario.Apellido}",
                Accion = a.Accion,
                Entidad = a.Entidad,
                IdEntidad = a.IdEntidad,
                ValoresAnteriores = a.ValoresAntes,
                ValoresNuevos = a.ValoresDespues,
                Ip = a.Ip
            })
            .ToListAsync();

        return Ok(audits);
    }
}
