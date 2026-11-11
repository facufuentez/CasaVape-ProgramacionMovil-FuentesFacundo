using System.ComponentModel.DataAnnotations;

namespace CasaVape.Core.Models.ViewModels
{
    public class DireccionViewModel
    {
        public int IdDireccion { get; set; }

        [Required(ErrorMessage = "La calle es obligatoria.")]
        [MaxLength(150)]
        [Display(Name = "Calle")]
        public string Calle { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número es obligatorio.")]
        [MaxLength(20)]
        [Display(Name = "Número")]
        public string Numero { get; set; } = string.Empty;

        [Required(ErrorMessage = "La ciudad es obligatoria.")]
        [MaxLength(100)]
        [Display(Name = "Ciudad")]
        public string Ciudad { get; set; } = string.Empty;

        [Required(ErrorMessage = "El código postal es obligatorio.")]
        [MaxLength(10)]
        [Display(Name = "Código Postal")]
        public string CodigoPostal { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar una provincia.")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una provincia.")]
        [Display(Name = "Provincia")]
        public int IdProvincia { get; set; }
    }
}
