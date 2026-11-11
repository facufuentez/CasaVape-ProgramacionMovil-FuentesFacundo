using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CasaVape.Core.Models.Entities
{
    public class Pago
    {
        [Key]
        public int IdPago { get; set; }

        [Required]
        public int IdPedido { get; set; }

        [ForeignKey(nameof(IdPedido))]
        public virtual Pedido? Pedido { get; set; }

        [Required]
        [MaxLength(50)]
        public string MetodoPago { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string EstadoPago { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        public DateTime? FechaPago { get; set; }

        [MaxLength(100)]
        public string? IdMercadoPago { get; set; }
    }
}
