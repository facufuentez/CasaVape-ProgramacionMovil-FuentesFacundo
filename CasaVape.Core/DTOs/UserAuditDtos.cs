namespace CasaVape.Core.DTOs;

public sealed class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public int CantidadPedidos { get; set; }
}

public sealed class UpdateUserRolesRequest
{
    public List<string> Roles { get; set; } = new();
}

public sealed class ResetPasswordRequest
{
    public string NuevaPassword { get; set; } = string.Empty;
    public string ConfirmarPassword { get; set; } = string.Empty;
}

public sealed class AuditDto
{
    public int IdAuditoria { get; set; }
    public DateTime Fecha { get; set; }
    public string? IdUsuario { get; set; }
    public string? Usuario { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string Entidad { get; set; } = string.Empty;
    public string? IdEntidad { get; set; }
    public string? ValoresAnteriores { get; set; }
    public string? ValoresNuevos { get; set; }
    public string? Ip { get; set; }
}
