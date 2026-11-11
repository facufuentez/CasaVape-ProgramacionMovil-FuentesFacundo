using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace CasaVape.Core.Models.Entities
{
    public class MercadoPagoCheckout
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string IdUsuario { get; set; } = string.Empty;

        [Required]
        public int IdDireccionEnvio { get; set; }

        [Required]
        public int IdMetodoEntrega { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostoEnvio { get; set; }

        [Required]
        public string ItemsJson { get; set; } = "[]";

        [MaxLength(100)]
        public string? IdMercadoPago { get; set; }

        [MaxLength(500)]
        public string? CheckoutUrl { get; set; }

        [Required]
        [MaxLength(30)]
        public string Estado { get; set; } = "Pendiente";

        public int? IdPedido { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;
    }

    public sealed record MercadoPagoCheckoutItem(
        int IdProducto,
        string Nombre,
        decimal Precio,
        int Cantidad);
}
