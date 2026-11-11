using System.Net.Http.Json;
using CasaVape.Core.DTOs;

namespace CasaVapeApp.Services;

public class AuditService
{
    private readonly HttpClient _http;

    public AuditService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<AuditDto>> GetAuditsAsync(
        string? usuario = null,
        string? entidad = null,
        string? accion = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        int page = 1,
        int pageSize = 50)
    {
        try
        {
            var url = $"api/admin/audits?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(usuario))
            {
                url += $"&usuario={Uri.EscapeDataString(usuario)}";
            }
            if (!string.IsNullOrWhiteSpace(entidad))
            {
                url += $"&entidad={Uri.EscapeDataString(entidad)}";
            }
            if (!string.IsNullOrWhiteSpace(accion))
            {
                url += $"&accion={Uri.EscapeDataString(accion)}";
            }
            if (desde.HasValue)
            {
                url += $"&desde={desde.Value:yyyy-MM-dd}";
            }
            if (hasta.HasValue)
            {
                url += $"&hasta={hasta.Value:yyyy-MM-dd}";
            }

            return await _http.GetFromJsonAsync<List<AuditDto>>(url) ?? new List<AuditDto>();
        }
        catch
        {
            return new List<AuditDto>();
        }
    }
}
