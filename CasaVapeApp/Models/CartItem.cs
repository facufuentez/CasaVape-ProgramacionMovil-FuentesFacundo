namespace CasaVapeApp.Models;

public class CartItem
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Cantidad { get; set; }
    public int Stock { get; set; }
    public string? ImagenUrl { get; set; }

    public decimal Subtotal => Precio * Cantidad;
}
