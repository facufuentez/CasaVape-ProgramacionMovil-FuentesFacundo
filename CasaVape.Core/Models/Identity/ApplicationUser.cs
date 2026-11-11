using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CasaVape.Core.Models.Identity
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Apellido { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? FechaNacimiento { get; set; }

        public virtual ICollection<Entities.Direccion> Direcciones { get; set; } = new List<Entities.Direccion>();
        public virtual ICollection<Entities.Pedido> Pedidos { get; set; } = new List<Entities.Pedido>();
        public virtual ICollection<Entities.Auditoria> Auditorias { get; set; } = new List<Entities.Auditoria>();
    }
}
