namespace CasaVape.Core.DTOs;

public sealed class OrderDto
{
    public int IdPedido { get; set; }
    public int IdUsuario { get; set; }
    public string? Usuario { get; set; }
    public int IdEstadoPedido { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int IdMetodoEntrega { get; set; }
    public string MetodoEntrega { get; set; } = string.Empty;
    public decimal CostoEnvio { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public List<OrderDetailDto> Detalles { get; set; } = new();
    public List<PaymentDto> Pagos { get; set; } = new();
}

public sealed class OrderDetailDto
{
    public int IdProducto { get; set; }
    public string Producto { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public sealed class PaymentDto
{
    public int IdPago { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public string EstadoPago { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public DateTime? FechaPago { get; set; }
    public string? IdMercadoPago { get; set; }
}

public sealed class CheckoutResponseDto
{
    public int PedidoId { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? CheckoutUrl { get; set; }
    public string? Mensaje { get; set; }
}
