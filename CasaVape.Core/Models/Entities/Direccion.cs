using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CasaVape.Core.Models.Identity;

namespace CasaVape.Core.Models.Entities
{
    public class Direccion
    {
        [Key]
        public int IdDireccion { get; set; }

        [Required]
        public string IdUsuario { get; set; } = string.Empty;

        [ForeignKey(nameof(IdUsuario))]
        public virtual ApplicationUser? Usuario { get; set; }

        [Required]
        public int IdProvincia { get; set; }

        [ForeignKey(nameof(IdProvincia))]
        public virtual Provincia? Provincia { get; set; }

        [Required]
        [MaxLength(150)]
        public string Calle { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Numero { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Ciudad { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string CodigoPostal { get; set; } = string.Empty;

        public bool Activa { get; set; } = true;

        public virtual ICollection<Pedido> PedidosEnvio { get; set; } = new List<Pedido>();
    }
}
