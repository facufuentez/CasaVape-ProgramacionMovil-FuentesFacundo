namespace CasaVape.Core.DTOs;

public sealed class AddressDto
{
    public int IdDireccion { get; set; }
    public int IdProvincia { get; set; }
    public string Provincia { get; set; } = string.Empty;
    public string Calle { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;
    public bool Activa { get; set; }
}

public sealed class CreateAddressRequest
{
    public int IdProvincia { get; set; }
    public string Calle { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;
}

public sealed class ShippingMethodDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Costo { get; set; }
}

public sealed class CreateShippingMethodRequest
{
    public string Nombre { get; set; } = string.Empty;
    public decimal Costo { get; set; }
}

public sealed class CheckoutItemRequest
{
    public int IdProducto { get; set; }
    public int Cantidad { get; set; }
}

public sealed class CheckoutRequest
{
    public int IdDireccion { get; set; }
    public int IdMetodoEntrega { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public List<CheckoutItemRequest> Items { get; set; } = new();
}

public sealed class CheckoutResponse
{
    public int PedidoId { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? CheckoutUrl { get; set; }
    public string? Mensaje { get; set; }
}
