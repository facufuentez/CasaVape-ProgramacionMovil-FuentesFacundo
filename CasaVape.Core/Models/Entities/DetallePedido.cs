using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CasaVape.Core.Models.Entities
{
    public class DetallePedido
    {
        [Key]
        public int IdDetalle { get; set; }

        [Required]
        public int IdPedido { get; set; }

        [ForeignKey(nameof(IdPedido))]
        public virtual Pedido? Pedido { get; set; }

        [Required]
        public int IdProducto { get; set; }

        [ForeignKey(nameof(IdProducto))]
        public virtual Producto? Producto { get; set; }

        [Required]
        public int Cantidad { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioUnitario { get; set; }
    }
}
