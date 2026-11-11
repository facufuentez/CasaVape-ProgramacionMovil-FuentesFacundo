using System.ComponentModel.DataAnnotations;

namespace CasaVape.Core.Models.ViewModels
{
    public class MetodoEntregaViewModel
    {
        public int IdMetodoEntrega { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(100)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Range(0, 9999999.99, ErrorMessage = "El costo debe ser mayor o igual a 0.")]
        [Display(Name = "Costo")]
        public decimal Costo { get; set; }
    }
}
