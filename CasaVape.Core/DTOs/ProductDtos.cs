namespace CasaVape.Core.DTOs;

public sealed class ProductDto
{
    public int IdProducto { get; set; }
    public int IdCategoria { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public string? ImagenUrl { get; set; }
    public bool Activo { get; set; }
}

public class CreateProductRequest
{
    public int IdCategoria { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public bool Activo { get; set; } = true;
}

public class UpdateProductRequest : CreateProductRequest
{
    public int IdProducto { get; set; }
}

public sealed class CategoryDto
{
    public int IdCategoria { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}

public sealed class CreateCategoryRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
