using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CasaVape.Core.Models.Identity;

namespace CasaVape.Core.Models.Entities
{
    public class Pedido
    {
        [Key]
        public int IdPedido { get; set; }

        [Required]
        public string IdUsuario { get; set; } = string.Empty;

        [ForeignKey(nameof(IdUsuario))]
        public virtual ApplicationUser? Usuario { get; set; }

        [Required]
        public int IdEstadoPedido { get; set; }

        [ForeignKey(nameof(IdEstadoPedido))]
        public virtual EstadoPedido? EstadoPedido { get; set; }

        [Required]
        public int IdMetodoEntrega { get; set; }

        [ForeignKey(nameof(IdMetodoEntrega))]
        public virtual MetodoEntrega? MetodoEntrega { get; set; }

        [Required]
        public int IdDireccionEnvio { get; set; }

        [ForeignKey(nameof(IdDireccionEnvio))]
        public virtual Direccion? DireccionEnvio { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostoEnvio { get; set; }

        public virtual ICollection<DetallePedido> DetallesPedido { get; set; } = new List<DetallePedido>();
        public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    }
}
