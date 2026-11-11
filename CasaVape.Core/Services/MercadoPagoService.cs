using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CasaVape.Core.Data;
using CasaVape.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CasaVape.Core.Services
{
    public class MercadoPagoService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly AuditoriaService _auditoria;
        private readonly ILogger<MercadoPagoService> _logger;

        public MercadoPagoService(
            HttpClient httpClient,
            IConfiguration configuration,
            ApplicationDbContext context,
            AuditoriaService auditoria,
            ILogger<MercadoPagoService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _context = context;
            _auditoria = auditoria;
            _logger = logger;

            if (_httpClient.BaseAddress is null)
            {
                var baseUrl = _configuration["MercadoPago:BaseUrl"] ?? "https://api.mercadopago.com/";
                _httpClient.BaseAddress = new Uri(baseUrl);
            }
        }

        public bool EstaConfigurado =>
            !string.IsNullOrWhiteSpace(_configuration["MercadoPago:AccessToken"]);

        public virtual async Task<MercadoPagoOrderResult> CrearOrderAsync(
            MercadoPagoCheckout checkout,
            string userEmail,
            string successUrl,
            string failureUrl,
            string pendingUrl,
            CancellationToken cancellationToken)
        {
            var options = GetOptions();
            if (string.IsNullOrWhiteSpace(options.AccessToken))
            {
                throw new MercadoPagoException(
                    StatusCodes.Status503ServiceUnavailable,
                    "Mercado Pago no está configurado. Agrega MP_ACCESS_TOKEN en el archivo .env.");
            }

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                throw new MercadoPagoException(StatusCodes.Status400BadRequest, "El usuario no tiene email configurado.");
            }

            var items = JsonSerializer.Deserialize<List<MercadoPagoCheckoutItem>>(checkout.ItemsJson, JsonOptions)
                ?? new List<MercadoPagoCheckoutItem>();

            if (items.Count == 0)
            {
                throw new MercadoPagoException(StatusCodes.Status400BadRequest, "El carrito está vacío.");
            }

            var requestBody = new
            {
                type = "online",
                processing_mode = "manual",
                capture_mode = "automatic_async",
                total_amount = checkout.Total.ToString("F2", CultureInfo.InvariantCulture),
                external_reference = checkout.Id.ToString(),
                description = $"Checkout Mercado Pago #{checkout.Id}",
                expiration_time = "P1D",
                payer = new
                {
                    email = userEmail
                },
                items = items.Select(item => new
                {
                    title = item.Nombre,
                    quantity = item.Cantidad,
                    unit_price = item.Precio.ToString("F2", CultureInfo.InvariantCulture)
                }).ToList(),
                config = new
                {
                    online = new
                    {
                        success_url = successUrl,
                        failure_url = failureUrl,
                        pending_url = pendingUrl,
                        auto_return = "all"
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/orders")
            {
                Content = content
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.AccessToken);
            request.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Mercado Pago devolvió {StatusCode}: {Body}", response.StatusCode, responseBody);
                throw new MercadoPagoException(
                    StatusCodes.Status502BadGateway,
                    "No se pudo crear la orden de pago en Mercado Pago.");
            }

            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            var orderId = root.TryGetProperty("id", out var idProperty)
                ? idProperty.GetString()
                : null;
            var checkoutUrl = root.TryGetProperty("checkout_url", out var urlProperty)
                ? urlProperty.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(checkoutUrl))
            {
                throw new MercadoPagoException(
                    StatusCodes.Status502BadGateway,
                    "Mercado Pago no devolvió la URL de pago.");
            }

            return new MercadoPagoOrderResult(orderId, checkoutUrl);
        }

        public virtual async Task<MercadoPagoOrderDetails> ObtenerOrderAsync(string orderId, CancellationToken cancellationToken)
        {
            var options = GetOptions();
            if (string.IsNullOrWhiteSpace(options.AccessToken))
            {
                throw new MercadoPagoException(
                    StatusCodes.Status503ServiceUnavailable,
                    "Mercado Pago no está configurado. Agrega MP_ACCESS_TOKEN en el archivo .env.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, $"v1/orders/{Uri.EscapeDataString(orderId)}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.AccessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Mercado Pago devolvió {StatusCode}: {Body}", response.StatusCode, responseBody);
                throw new MercadoPagoException(
                    StatusCodes.Status502BadGateway,
                    "No se pudo consultar la orden en Mercado Pago.");
            }

            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            var status = root.TryGetProperty("status", out var statusProperty)
                ? statusProperty.GetString() ?? "created"
                : "created";
            var statusDetail = root.TryGetProperty("status_detail", out var detailProperty)
                ? detailProperty.GetString()
                : null;

            string? paymentStatus = null;
            if (root.TryGetProperty("transactions", out var transactions) &&
                transactions.TryGetProperty("payments", out var payments) &&
                payments.ValueKind == JsonValueKind.Array)
            {
                var firstPayment = payments.EnumerateArray().FirstOrDefault();
                if (firstPayment.ValueKind == JsonValueKind.Object &&
                    firstPayment.TryGetProperty("status", out var paymentStatusProperty))
                {
                    paymentStatus = paymentStatusProperty.GetString();
                }
            }

            return new MercadoPagoOrderDetails(status, statusDetail, paymentStatus);
        }

        public async Task<Pedido?> ConvertirCheckoutSiAprobadoAsync(
            MercadoPagoCheckout checkout,
            MercadoPagoOrderDetails details,
            CancellationToken cancellationToken)
        {
            if (checkout.IdPedido.HasValue)
            {
                return await _context.Pedidos
                    .Include(p => p.EstadoPedido)
                    .Include(p => p.MetodoEntrega)
                    .Include(p => p.DireccionEnvio)
                        .ThenInclude(d => d!.Provincia)
                    .Include(p => p.DetallesPedido)
                        .ThenInclude(d => d.Producto)
                    .Include(p => p.Pagos)
                    .FirstOrDefaultAsync(p => p.IdPedido == checkout.IdPedido.Value, cancellationToken);
            }

            if (!(details.PaymentStatus == "approved" || details.Status == "processed"))
            {
                return null;
            }

            var items = JsonSerializer.Deserialize<List<MercadoPagoCheckoutItem>>(checkout.ItemsJson, JsonOptions)
                ?? new List<MercadoPagoCheckoutItem>();

            if (items.Count == 0)
            {
                checkout.Estado = "Fallido";
                await _context.SaveChangesAsync(cancellationToken);
                throw new MercadoPagoException(StatusCodes.Status400BadRequest, "El checkout de Mercado Pago está vacío.");
            }

            var productIds = items.Select(item => item.IdProducto).Distinct().ToList();
            var products = await _context.Productos
                .Where(product => productIds.Contains(product.IdProducto))
                .ToDictionaryAsync(product => product.IdProducto, cancellationToken);

            foreach (var item in items)
            {
                if (!products.TryGetValue(item.IdProducto, out var product))
                {
                    checkout.Estado = "Fallido";
                    await _context.SaveChangesAsync(cancellationToken);
                    throw new MercadoPagoException(StatusCodes.Status409Conflict, $"El producto '{item.Nombre}' ya no está disponible.");
                }

                if (product.Stock < item.Cantidad)
                {
                    checkout.Estado = "Fallido";
                    await _context.SaveChangesAsync(cancellationToken);
                    throw new MercadoPagoException(StatusCodes.Status409Conflict, $"Stock insuficiente para '{item.Nombre}'.");
                }
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var estadoPagado = await _context.EstadosPedido
                    .FirstOrDefaultAsync(e => e.Nombre == "Pagado", cancellationToken);

                if (estadoPagado is null)
                {
                    throw new MercadoPagoException(StatusCodes.Status500InternalServerError, "Falta el estado 'Pagado'.");
                }

                var pedido = new Pedido
                {
                    IdUsuario = checkout.IdUsuario,
                    IdEstadoPedido = estadoPagado.IdEstadoPedido,
                    IdMetodoEntrega = checkout.IdMetodoEntrega,
                    IdDireccionEnvio = checkout.IdDireccionEnvio,
                    Fecha = DateTime.Now,
                    CostoEnvio = checkout.CostoEnvio
                };

                foreach (var item in items)
                {
                    var product = products[item.IdProducto];
                    pedido.DetallesPedido.Add(new DetallePedido
                    {
                        IdProducto = item.IdProducto,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.Precio
                    });
                    product.Stock -= item.Cantidad;
                }

                pedido.Pagos.Add(new Pago
                {
                    MetodoPago = "Mercado Pago",
                    EstadoPago = "Aprobado",
                    Monto = checkout.Total,
                    FechaPago = DateTime.Now,
                    IdMercadoPago = checkout.IdMercadoPago
                });

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync(cancellationToken);

                checkout.Estado = "Convertido";
                checkout.IdPedido = pedido.IdPedido;
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                await _auditoria.RegistrarAsync("Crear", "Pedido", pedido.IdPedido.ToString(), null, new
                {
                    pedido.IdUsuario,
                    Total = checkout.Total,
                    MetodoPago = "Mercado Pago",
                    Items = items.Count,
                    Estado = estadoPagado.Nombre,
                    MercadoPagoCheckoutId = checkout.Id,
                    MercadoPagoOrderId = checkout.IdMercadoPago
                });

                return pedido;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task ActualizarEstadoCheckoutAsync(
            MercadoPagoCheckout checkout,
            MercadoPagoOrderDetails details,
            CancellationToken cancellationToken)
        {
            if (details.PaymentStatus == "rejected" || details.Status == "failed")
            {
                checkout.Estado = "Rechazado";
            }
            else if (details.Status == "canceled")
            {
                checkout.Estado = "Cancelado";
            }
            else if (details.PaymentStatus == "refunded" || details.Status == "refunded")
            {
                checkout.Estado = "Reembolsado";
            }
            else if (details.Status == "processing" || details.Status == "action_required")
            {
                checkout.Estado = "Pendiente";
            }
            else if (details.Status == "expired")
            {
                checkout.Estado = "Expirado";
            }
            else
            {
                checkout.Estado = "Pendiente";
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public static bool ValidarFirmaWebhook(string? xSignature, string? xRequestId, string? dataId, string secret)
        {
            if (string.IsNullOrWhiteSpace(xSignature) ||
                string.IsNullOrWhiteSpace(xRequestId) ||
                string.IsNullOrWhiteSpace(dataId) ||
                string.IsNullOrWhiteSpace(secret))
            {
                return false;
            }

            string? timestamp = null;
            string? hash = null;

            foreach (var part in xSignature.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (part.StartsWith("ts=", StringComparison.OrdinalIgnoreCase))
                {
                    timestamp = part[3..];
                }
                else if (part.StartsWith("v1=", StringComparison.OrdinalIgnoreCase))
                {
                    hash = part[3..];
                }
            }

            if (string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(hash))
            {
                return false;
            }

            var manifest = $"id:{dataId};request-id:{xRequestId};ts:{timestamp};";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computedBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
            var computedHash = Convert.ToHexString(computedBytes).ToLowerInvariant();

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHash),
                Encoding.UTF8.GetBytes(hash.ToLowerInvariant()));
        }

        private MercadoPagoOptions GetOptions()
        {
            return _configuration.GetSection("MercadoPago").Get<MercadoPagoOptions>() ?? new MercadoPagoOptions();
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private sealed class MercadoPagoOptions
        {
            public string BaseUrl { get; set; } = "https://api.mercadopago.com/";
            public string AccessToken { get; set; } = string.Empty;
            public string WebhookSecret { get; set; } = string.Empty;
            public string Environment { get; set; } = "Sandbox";
        }
    }

    public sealed record MercadoPagoOrderResult(string OrderId, string CheckoutUrl);

    public sealed record MercadoPagoOrderDetails(string Status, string? StatusDetail, string? PaymentStatus);

    public sealed class MercadoPagoException : Exception
    {
        public MercadoPagoException(int statusCode, string userMessage)
            : base(userMessage)
        {
            StatusCode = statusCode;
            UserMessage = userMessage;
        }

        public int StatusCode { get; }

        public string UserMessage { get; }
    }
}
