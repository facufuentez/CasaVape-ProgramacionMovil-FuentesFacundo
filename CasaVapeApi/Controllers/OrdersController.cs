using CasaVape.Core.Data;
using CasaVape.Core.DTOs;
using CasaVape.Core.Models.Entities;
using CasaVape.Core.Services;
using CasaVapeApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CasaVapeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly AuditoriaService _auditoria;
    private readonly MercadoPagoService _mercadoPago;

    public OrdersController(ApplicationDbContext context, IWebHostEnvironment environment, AuditoriaService auditoria, MercadoPagoService mercadoPago)
    {
        _context = context;
        _environment = environment;
        _auditoria = auditoria;
        _mercadoPago = mercadoPago;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private bool IsAdmin => User.IsInRole("Administrador");

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetMyOrders()
    {
        var orders = await LoadOrdersAsync(q => q.Where(o => o.IdUsuario == UserId));
        return Ok(orders.Select(MapOrder));
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetAdminOrders(string? estado, string? q, int page = 1, int pageSize = 20)
    {
        var query = _context.Pedidos
            .AsNoTracking()
            .Include(o => o.Usuario)
            .Include(o => o.EstadoPedido)
            .Include(o => o.MetodoEntrega)
            .Include(o => o.DetallesPedido)
                .ThenInclude(d => d.Producto)
            .Include(o => o.Pagos)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(estado))
        {
            query = query.Where(o => o.EstadoPedido != null && o.EstadoPedido.Nombre == estado);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            if (int.TryParse(q, out var orderId))
            {
                query = query.Where(o => o.IdPedido == orderId);
            }
        }

        var orders = await query
            .OrderByDescending(o => o.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(orders.Select(MapOrder));
    }

    [HttpGet("admin/reporte")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetReporte(DateTime? desde, DateTime? hasta, string? estado)
    {
        var orders = await ObtenerPedidosReporteAsync(desde, hasta, estado);
        return Ok(orders.Select(MapOrder));
    }

    [HttpGet("admin/reporte/pdf")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> DescargarReportePdf(DateTime? desde, DateTime? hasta, string? estado)
    {
        var orders = await ObtenerPedidosReporteAsync(desde, hasta, estado);
        var pdf = new PedidoReporteService(_environment.WebRootPath).GenerarListadoPdf(orders, desde, hasta, estado);
        return File(pdf, "application/pdf", $"reporte-pedidos-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
    }

    private async Task<List<Pedido>> ObtenerPedidosReporteAsync(DateTime? desde, DateTime? hasta, string? estado)
    {
        var query = _context.Pedidos
            .AsNoTracking()
            .Include(p => p.Usuario)
            .Include(p => p.EstadoPedido)
            .Include(p => p.MetodoEntrega)
            .Include(p => p.DetallesPedido)
                .ThenInclude(d => d.Producto)
            .Include(p => p.Pagos)
            .AsQueryable();

        if (desde.HasValue)
        {
            query = query.Where(p => p.Fecha >= desde.Value.Date);
        }

        if (hasta.HasValue)
        {
            var limite = hasta.Value.Date.AddDays(1);
            query = query.Where(p => p.Fecha < limite);
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            query = query.Where(p => p.EstadoPedido != null && p.EstadoPedido.Nombre == estado);
        }

        return await query.OrderByDescending(p => p.Fecha).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetById(int id)
    {
        var order = await LoadOrderAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        if (!IsAdmin && order.IdUsuario != UserId)
        {
            return NotFound();
        }

        return Ok(MapOrder(order));
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutResponseDto>> Checkout(CheckoutRequest request)
    {
        var user = await _context.Users.FindAsync(UserId);
        if (user is null)
        {
            return Unauthorized();
        }

        if (request.Items.Count == 0)
        {
            return BadRequest(new { error = "El carrito está vacío." });
        }

        var productIds = request.Items.Select(i => i.IdProducto).Distinct().ToList();
        var products = await _context.Productos
            .Where(p => productIds.Contains(p.IdProducto))
            .ToDictionaryAsync(p => p.IdProducto);

        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.IdProducto, out var product) || !product.Activo)
            {
                return BadRequest(new { error = "Uno de los productos no está disponible." });
            }

            if (item.Cantidad < 1 || item.Cantidad > product.Stock)
            {
                return BadRequest(new { error = $"Stock insuficiente para '{product.Nombre}'. Disponible: {product.Stock}." });
            }
        }

        var address = await _context.Direcciones
            .FirstOrDefaultAsync(d => d.IdDireccion == request.IdDireccion && d.IdUsuario == UserId && d.Activa);

        if (address is null)
        {
            return BadRequest(new { error = "La dirección seleccionada no es válida." });
        }

        var shippingMethod = await _context.MetodosEntrega
            .FirstOrDefaultAsync(m => m.IdMetodoEntrega == request.IdMetodoEntrega);

        if (shippingMethod is null)
        {
            return BadRequest(new { error = "El método de entrega seleccionado no es válido." });
        }

        var validPaymentMethods = new[] { "Efectivo", "Transferencia por QR", "Mercado Pago" };
        if (!validPaymentMethods.Contains(request.MetodoPago))
        {
            return BadRequest(new { error = "Seleccioná un medio de pago válido." });
        }

        var subtotal = request.Items.Sum(item => products[item.IdProducto].Precio * item.Cantidad);
        var total = subtotal + shippingMethod.Costo;

        if (request.MetodoPago == "Mercado Pago" && !_mercadoPago.EstaConfigurado)
        {
            return BadRequest(new { error = "Mercado Pago no está configurado." });
        }

        if (request.MetodoPago == "Mercado Pago" && string.IsNullOrWhiteSpace(user.Email))
        {
            return BadRequest(new { error = "Tu cuenta no tiene un email válido para Mercado Pago." });
        }

        if (request.MetodoPago == "Mercado Pago")
        {
            var checkoutItems = request.Items.Select(item =>
            {
                var product = products[item.IdProducto];
                return new MercadoPagoCheckoutItem(product.IdProducto, product.Nombre, product.Precio, item.Cantidad);
            }).ToList();

            var checkout = new MercadoPagoCheckout
            {
                IdUsuario = UserId,
                IdDireccionEnvio = address.IdDireccion,
                IdMetodoEntrega = shippingMethod.IdMetodoEntrega,
                Total = total,
                CostoEnvio = shippingMethod.Costo,
                ItemsJson = System.Text.Json.JsonSerializer.Serialize(checkoutItems),
                Estado = "Pendiente",
                Fecha = DateTime.Now
            };

            _context.MercadoPagoCheckouts.Add(checkout);
            await _context.SaveChangesAsync();

            var successUrl = Url.Action("Success", "MercadoPago", null, Request.Scheme)!;
            var failureUrl = Url.Action("Failure", "MercadoPago", null, Request.Scheme)!;
            var pendingUrl = Url.Action("Pending", "MercadoPago", null, Request.Scheme)!;

            MercadoPagoOrderResult mpOrder;
            try
            {
                mpOrder = await _mercadoPago.CrearOrderAsync(
                    checkout,
                    user.Email!,
                    successUrl,
                    failureUrl,
                    pendingUrl,
                    CancellationToken.None);
            }
            catch (MercadoPagoException ex)
            {
                _context.MercadoPagoCheckouts.Remove(checkout);
                await _context.SaveChangesAsync();
                return BadRequest(new { error = ex.UserMessage });
            }

            checkout.IdMercadoPago = mpOrder.OrderId;
            checkout.CheckoutUrl = mpOrder.CheckoutUrl;
            await _context.SaveChangesAsync();

            await _auditoria.RegistrarAsync("Crear", "CheckoutMercadoPago", checkout.Id.ToString(), null, new
            {
                checkout.IdUsuario,
                Total = total,
                Items = request.Items.Count,
                MercadoPagoOrderId = mpOrder.OrderId
            });

            return Ok(new CheckoutResponseDto
            {
                PedidoId = 0,
                Total = total,
                Estado = "Pendiente",
                CheckoutUrl = mpOrder.CheckoutUrl,
                Mensaje = "Redirigir a Mercado Pago para completar el pago."
            });
        }

        var estadoPendiente = await _context.EstadosPedido
            .FirstOrDefaultAsync(e => e.Nombre == "Pendiente");

        if (estadoPendiente is null)
        {
            return BadRequest(new { error = "Falta el estado 'Pendiente'." });
        }

        var pedido = new Pedido
        {
            IdUsuario = UserId,
            IdEstadoPedido = estadoPendiente.IdEstadoPedido,
            IdMetodoEntrega = shippingMethod.IdMetodoEntrega,
            IdDireccionEnvio = address.IdDireccion,
            Fecha = DateTime.Now,
            CostoEnvio = shippingMethod.Costo
        };

        foreach (var item in request.Items)
        {
            var product = products[item.IdProducto];
            pedido.DetallesPedido.Add(new DetallePedido
            {
                IdProducto = product.IdProducto,
                Cantidad = item.Cantidad,
                PrecioUnitario = product.Precio
            });
            product.Stock -= item.Cantidad;
        }

        pedido.Pagos.Add(new Pago
        {
            MetodoPago = request.MetodoPago,
            EstadoPago = "Pendiente",
            Monto = total,
            FechaPago = null
        });

        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();

        await _auditoria.RegistrarAsync("Crear", "Pedido", pedido.IdPedido.ToString(), null, new
        {
            pedido.IdUsuario,
            Total = total,
            MetodoPago = request.MetodoPago,
            Items = request.Items.Count,
            Estado = estadoPendiente.Nombre
        });

        return Ok(new CheckoutResponseDto
        {
            PedidoId = pedido.IdPedido,
            Total = total,
            Estado = estadoPendiente.Nombre,
            Mensaje = "Pedido registrado correctamente. Aguardamos la confirmación del pago."
        });
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] string nuevoEstado)
    {
        var pedido = await _context.Pedidos
            .Include(p => p.EstadoPedido)
            .Include(p => p.DetallesPedido)
            .FirstOrDefaultAsync(p => p.IdPedido == id);

        if (pedido is null)
        {
            return NotFound();
        }

        var estado = await _context.EstadosPedido
            .FirstOrDefaultAsync(e => e.Nombre == nuevoEstado);

        if (estado is null)
        {
            return BadRequest(new { error = "El estado no existe." });
        }

        var actual = pedido.EstadoPedido?.Nombre ?? string.Empty;
        var permitidos = nuevoEstado switch
        {
            "Pagado" => new[] { "Pendiente" },
            "Enviado" => new[] { "Pagado" },
            "Entregado" => new[] { "Enviado" },
            "Cancelado" => new[] { "Pendiente", "Pagado" },
            _ => Array.Empty<string>()
        };

        if (!permitidos.Contains(actual, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = $"No se puede pasar de '{actual}' a '{nuevoEstado}'." });
        }

        if (string.Equals(nuevoEstado, "Cancelado", StringComparison.OrdinalIgnoreCase))
        {
            var ids = pedido.DetallesPedido.Select(d => d.IdProducto).Distinct().ToList();
            var products = await _context.Productos
                .Where(p => ids.Contains(p.IdProducto))
                .ToDictionaryAsync(p => p.IdProducto);

            foreach (var detail in pedido.DetallesPedido)
            {
                if (products.TryGetValue(detail.IdProducto, out var product))
                {
                    product.Stock += detail.Cantidad;
                }
            }
        }

        pedido.IdEstadoPedido = estado.IdEstadoPedido;
        await _context.SaveChangesAsync();

        await _auditoria.RegistrarAsync("CambiarEstado", "Pedido", pedido.IdPedido.ToString(),
            new { Estado = actual },
            new { Estado = nuevoEstado });

        return NoContent();
    }

    [HttpPost("{id:int}/confirm-payment")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ConfirmPayment(int id)
    {
        var pedido = await _context.Pedidos
            .Include(p => p.EstadoPedido)
            .Include(p => p.Pagos)
            .FirstOrDefaultAsync(p => p.IdPedido == id);

        if (pedido is null)
        {
            return NotFound();
        }

        if (string.Equals(pedido.EstadoPedido?.Nombre, "Cancelado", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "No se puede confirmar el pago de un pedido cancelado." });
        }

        var pago = pedido.Pagos.OrderByDescending(p => p.IdPago).FirstOrDefault();
        if (pago is null)
        {
            return BadRequest(new { error = "El pedido no tiene un pago registrado." });
        }

        if (string.Equals(pago.EstadoPago, "Aprobado", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new { message = "El pago ya estaba aprobado." });
        }

        pago.EstadoPago = "Aprobado";
        pago.FechaPago = DateTime.Now;

        if (string.Equals(pedido.EstadoPedido?.Nombre, "Pendiente", StringComparison.OrdinalIgnoreCase))
        {
            var pagado = await _context.EstadosPedido.FirstOrDefaultAsync(e => e.Nombre == "Pagado");
            if (pagado is not null)
            {
                pedido.IdEstadoPedido = pagado.IdEstadoPedido;
            }
        }

        await _context.SaveChangesAsync();

        await _auditoria.RegistrarAsync("ConfirmarPago", "Pedido", pedido.IdPedido.ToString(),
            new { EstadoPago = "Pendiente" },
            new { EstadoPago = pago.EstadoPago });

        return Ok(new { message = "Pago confirmado correctamente." });
    }

    [HttpPost("{id:int}/reject-payment")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> RejectPayment(int id)
    {
        var pedido = await _context.Pedidos
            .Include(p => p.EstadoPedido)
            .Include(p => p.Pagos)
            .Include(p => p.DetallesPedido)
            .FirstOrDefaultAsync(p => p.IdPedido == id);

        if (pedido is null)
        {
            return NotFound();
        }

        var actual = pedido.EstadoPedido?.Nombre ?? string.Empty;
        if (string.Equals(actual, "Cancelado", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "El pedido ya está cancelado." });
        }

        if (!string.Equals(actual, "Pendiente", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(actual, "Pagado", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Solo se puede rechazar el pago de pedidos Pendientes o Pagados." });
        }

        var pago = pedido.Pagos.OrderByDescending(p => p.IdPago).FirstOrDefault();
        if (pago is null)
        {
            return BadRequest(new { error = "El pedido no tiene un pago registrado." });
        }

        var cancelado = await _context.EstadosPedido.FirstOrDefaultAsync(e => e.Nombre == "Cancelado");
        if (cancelado is null)
        {
            return BadRequest(new { error = "No se encontró el estado 'Cancelado'." });
        }

        pago.EstadoPago = "Rechazado";
        pago.FechaPago = null;

        var ids = pedido.DetallesPedido.Select(d => d.IdProducto).Distinct().ToList();
        var products = await _context.Productos
            .Where(p => ids.Contains(p.IdProducto))
            .ToDictionaryAsync(p => p.IdProducto);

        foreach (var detail in pedido.DetallesPedido)
        {
            if (products.TryGetValue(detail.IdProducto, out var product))
            {
                product.Stock += detail.Cantidad;
            }
        }

        pedido.IdEstadoPedido = cancelado.IdEstadoPedido;
        await _context.SaveChangesAsync();

        await _auditoria.RegistrarAsync("RechazarPago", "Pedido", pedido.IdPedido.ToString(),
            new { Estado = actual, EstadoPago = "Pendiente" },
            new { Estado = cancelado.Nombre, EstadoPago = pago.EstadoPago, StockRepuesto = true });

        return Ok(new { message = "Pago rechazado, pedido cancelado y stock repuesto." });
    }

    [HttpGet("{id:int}/receipt")]
    public async Task<IActionResult> Receipt(int id)
    {
        var pedido = await _context.Pedidos
            .Include(p => p.Usuario)
            .Include(p => p.EstadoPedido)
            .Include(p => p.MetodoEntrega)
            .Include(p => p.DireccionEnvio)
                .ThenInclude(d => d!.Provincia)
            .Include(p => p.DetallesPedido)
                .ThenInclude(d => d.Producto)
            .Include(p => p.Pagos)
            .FirstOrDefaultAsync(p => p.IdPedido == id);

        if (pedido is null)
        {
            return NotFound();
        }

        if (!IsAdmin && pedido.IdUsuario != UserId)
        {
            return NotFound();
        }

        var pdf = new PedidoReporteService(_environment.WebRootPath).GenerarComprobantePdf(pedido);
        return File(pdf, "application/pdf", $"comprobante-pedido-{pedido.IdPedido}.pdf");
    }

    private async Task<List<Pedido>> LoadOrdersAsync(Func<IQueryable<Pedido>, IQueryable<Pedido>>? filter = null)
    {
        var query = _context.Pedidos
            .AsNoTracking()
            .Include(o => o.EstadoPedido)
            .Include(o => o.MetodoEntrega)
            .Include(o => o.DetallesPedido)
                .ThenInclude(d => d.Producto)
            .Include(o => o.Pagos)
            .AsQueryable();

        if (filter is not null)
        {
            query = filter(query);
        }

        return await query.OrderByDescending(o => o.Fecha).ToListAsync();
    }

    private async Task<Pedido?> LoadOrderAsync(int id)
    {
        return await _context.Pedidos
            .Include(o => o.EstadoPedido)
            .Include(o => o.MetodoEntrega)
            .Include(o => o.DireccionEnvio)
                .ThenInclude(d => d!.Provincia)
            .Include(o => o.DetallesPedido)
                .ThenInclude(d => d.Producto)
            .Include(o => o.Pagos)
            .FirstOrDefaultAsync(o => o.IdPedido == id);
    }

    private static OrderDto MapOrder(Pedido pedido)
    {
        var pagos = pedido.Pagos
            .OrderByDescending(p => p.IdPago)
            .Select(p => new PaymentDto
            {
                IdPago = p.IdPago,
                MetodoPago = p.MetodoPago,
                EstadoPago = p.EstadoPago,
                Monto = p.Monto,
                FechaPago = p.FechaPago,
                IdMercadoPago = p.IdMercadoPago
            })
            .ToList();

        var detalles = pedido.DetallesPedido.Select(d => new OrderDetailDto
        {
            IdProducto = d.IdProducto,
            Producto = d.Producto?.Nombre ?? string.Empty,
            Cantidad = d.Cantidad,
            PrecioUnitario = d.PrecioUnitario,
            Subtotal = d.Cantidad * d.PrecioUnitario
        }).ToList();

        return new OrderDto
        {
            IdPedido = pedido.IdPedido,
            IdUsuario = int.TryParse(pedido.IdUsuario, out var userId) ? userId : 0,
            Usuario = pedido.Usuario is null ? null : $"{pedido.Usuario.Nombre} {pedido.Usuario.Apellido}",
            IdEstadoPedido = pedido.IdEstadoPedido,
            Estado = pedido.EstadoPedido?.Nombre ?? string.Empty,
            IdMetodoEntrega = pedido.IdMetodoEntrega,
            MetodoEntrega = pedido.MetodoEntrega?.Nombre ?? string.Empty,
            CostoEnvio = pedido.CostoEnvio,
            Fecha = pedido.Fecha,
            Total = detalles.Sum(d => d.Subtotal) + pedido.CostoEnvio,
            Detalles = detalles,
            Pagos = pagos
        };
    }
}
