using CasaVape.Core.Data;
using CasaVape.Core.DTOs;
using CasaVape.Core.Models.Identity;
using CasaVape.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CasaVapeApi.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Administrador")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AuditoriaService _auditoria;

    public UsersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        AuditoriaService auditoria)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _auditoria = auditoria;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> Get()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.Apellido)
            .ThenBy(u => u.Nombre)
            .ToListAsync();

        var orderCounts = await _context.Pedidos
            .GroupBy(p => p.IdUsuario)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var result = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserDto
            {
                Id = user.Id,
                Nombre = user.Nombre,
                Apellido = user.Apellido,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList(),
                CantidadPedidos = orderCounts.TryGetValue(user.Id, out var count) ? count : 0
            });
        }

        return Ok(result);
    }

    [HttpPut("{id}/roles")]
    public async Task<IActionResult> UpdateRoles(string id, UpdateUserRolesRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var before = await _userManager.GetRolesAsync(user);

        foreach (var role in request.Roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                return BadRequest(new { error = $"El rol '{role}' no existe." });
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRolesAsync(user, request.Roles.Distinct());

        await _auditoria.RegistrarAsync("CambiarRoles", "Usuario", user.Id,
            new { Roles = before },
            new { Roles = request.Roles.Distinct() });

        return NoContent();
    }

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, ResetPasswordRequest request)
    {
        if (request.NuevaPassword != request.ConfirmarPassword)
        {
            return BadRequest(new { error = "Las contraseñas no coinciden." });
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            return BadRequest(new { errors = removeResult.Errors.Select(e => e.Description) });
        }

        var addResult = await _userManager.AddPasswordAsync(user, request.NuevaPassword);
        if (!addResult.Succeeded)
        {
            return BadRequest(new { errors = addResult.Errors.Select(e => e.Description) });
        }

        await _auditoria.RegistrarAsync("ResetPassword", "Usuario", user.Id, null, new
        {
            user.Email
        });

        return NoContent();
    }
}
