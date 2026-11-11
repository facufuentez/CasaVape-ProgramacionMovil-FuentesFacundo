using CasaVape.Core.Models;
using CasaVape.Core.Models.Entities;

namespace CasaVape.Core.Models.ViewModels
{
    public class HomeIndexViewModel
    {
        public string? Busqueda { get; set; }
        public PaginatedList<Producto> Productos { get; set; } =
            new(new List<Producto>(), 0, 1, PaginatedList<Producto>.DefaultPageSize);

        public List<Producto> CarruselProductos { get; set; } = new();
    }
}
