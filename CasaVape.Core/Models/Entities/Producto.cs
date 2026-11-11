using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CasaVape.Core.Models.Entities
{
    public class Producto
    {
        [Key]
        public int IdProducto { get; set; }

        [Required]
        public int IdCategoria { get; set; }

        [ForeignKey(nameof(IdCategoria))]
        public virtual Categoria? Categoria { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Descripcion { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "El precio no puede ser negativo.")]
        public decimal Precio { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
        public int Stock { get; set; }

        [MaxLength(500)]
        public string? ImagenUrl { get; set; }

        public bool Activo { get; set; } = true;

        public virtual ICollection<DetallePedido> DetallesPedido { get; set; } = new List<DetallePedido>();
    }
}
