using System.ComponentModel.DataAnnotations;

namespace CasaVape.Core.Models.Entities
{
    public class EstadoPedido
    {
        [Key]
        public int IdEstadoPedido { get; set; }

        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; } = string.Empty;

        public virtual ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    }
}
