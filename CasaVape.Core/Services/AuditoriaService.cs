using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using CasaVape.Core.Data;
using CasaVape.Core.Models.Entities;
using CasaVape.Core.Models.Identity;

namespace CasaVape.Core.Services
{
    public class AuditoriaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AuditoriaService> _logger;

        public AuditoriaService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager,
            ILogger<AuditoriaService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task RegistrarAsync(
            string accion,
            string entidad,
            string? idEntidad = null,
            object? antes = null,
            object? despues = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var principal = httpContext?.User;

                if (principal?.Identity?.IsAuthenticated != true)
                {
                    return;
                }

                var idUsuario = _userManager.GetUserId(principal);
                if (string.IsNullOrEmpty(idUsuario))
                {
                    return;
                }

                var auditoria = new Auditoria
                {
                    IdUsuario = idUsuario,
                    Fecha = DateTime.Now,
                    Accion = Limitar(accion, 100) ?? string.Empty,
                    Entidad = Limitar(entidad, 100) ?? string.Empty,
                    IdEntidad = idEntidad == null ? null : Limitar(idEntidad, 100),
                    ValoresAntes = Serializar(antes),
                    ValoresDespues = Serializar(despues),
                    Ip = Limitar(httpContext?.Connection.RemoteIpAddress?.ToString(), 45)
                };

                _context.Auditorias.Add(auditoria);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo registrar la auditoría para {Entidad} {IdEntidad}.", entidad, idEntidad);
            }
        }

        private static string? Serializar(object? valores)
        {
            return valores == null ? null : JsonSerializer.Serialize(valores);
        }

        private static string? Limitar(string? valor, int maximo)
        {
            if (string.IsNullOrEmpty(valor))
            {
                return valor;
            }

            return valor.Length <= maximo ? valor : valor[..maximo];
        }
    }
}
