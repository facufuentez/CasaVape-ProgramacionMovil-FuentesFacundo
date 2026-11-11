using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CasaVape.Core.Models.Identity;

namespace CasaVape.Core.Models.Entities
{
    public class Auditoria
    {
        [Key]
        public int IdAuditoria { get; set; }

        [Required]
        public string IdUsuario { get; set; } = string.Empty;

        [ForeignKey(nameof(IdUsuario))]
        public virtual ApplicationUser? Usuario { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(100)]
        public string Accion { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Entidad { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? IdEntidad { get; set; }

        public string? ValoresAntes { get; set; }

        public string? ValoresDespues { get; set; }

        [MaxLength(45)]
        public string? Ip { get; set; }
    }
}
