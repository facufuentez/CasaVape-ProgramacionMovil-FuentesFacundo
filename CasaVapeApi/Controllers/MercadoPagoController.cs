using CasaVape.Core.Data;
using CasaVape.Core.Models.Identity;
using CasaVape.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace CasaVapeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MercadoPagoController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly MercadoPagoService _mercadoPago;
    private readonly AuditoriaService _auditoria;
    private readonly ILogger<MercadoPagoController> _logger;
    private readonly IConfiguration _configuration;

    public MercadoPagoController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        MercadoPagoService mercadoPago,
        AuditoriaService auditoria,
        ILogger<MercadoPagoController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _mercadoPago = mercadoPago;
        _auditoria = auditoria;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet("checkout/{checkoutId:guid}/status")]
    [Authorize]
    public async Task<IActionResult> CheckoutStatus(Guid checkoutId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var checkout = await _context.MercadoPagoCheckouts
            .FirstOrDefaultAsync(c => c.Id == checkoutId && c.IdUsuario == userId, cancellationToken);

        if (checkout is null)
        {
            return NotFound();
        }

        MercadoPagoOrderDetails? details = null;
        if (!string.IsNullOrWhiteSpace(checkout.IdMercadoPago))
        {
            try
            {
                details = await _mercadoPago.ObtenerOrderAsync(checkout.IdMercadoPago, cancellationToken);
            }
            catch (MercadoPagoException ex)
            {
                _logger.LogWarning(ex, "No se pudo consultar la orden {OrderId} en Mercado Pago.", checkout.IdMercadoPago);
            }
        }

        if (details is not null)
        {
            if (details.PaymentStatus == "approved" || details.Status == "processed")
            {
                try
                {
                    await _mercadoPago.ConvertirCheckoutSiAprobadoAsync(checkout, details, cancellationToken);
                }
                catch (MercadoPagoException ex)
                {
                    _logger.LogWarning(ex, "No se pudo convertir el checkout {CheckoutId}.", checkout.Id);
                    checkout.Estado = "Fallido";
                }
            }
            else
            {
                await _mercadoPago.ActualizarEstadoCheckoutAsync(checkout, details, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        return Ok(new
        {
            checkoutId = checkout.Id,
            estado = checkout.Estado,
            pedidoId = checkout.IdPedido,
            mpStatus = details?.Status,
            mpPaymentStatus = details?.PaymentStatus
        });
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        string body;
        using (var reader = new StreamReader(Request.Body))
        {
            body = await reader.ReadToEndAsync(cancellationToken);
        }

        string? dataId = Request.Query["data.id"].ToString();
        string? externalReference = null;

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("id", out var idProperty))
                {
                    dataId ??= idProperty.GetString();
                }

                if (document.RootElement.TryGetProperty("data", out var dataReference) &&
                    dataReference.TryGetProperty("external_reference", out var referenceProperty))
                {
                    externalReference = referenceProperty.GetString();
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Webhook de Mercado Pago con JSON inválido.");
                return BadRequest();
            }
        }

        if (string.IsNullOrWhiteSpace(dataId))
        {
            return Ok();
        }

        var secret = _configuration["MercadoPago:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogWarning("Webhook de Mercado Pago recibido sin secreto configurado.");
            return Unauthorized();
        }

        var xSignature = Request.Headers["x-signature"].ToString();
        var xRequestId = Request.Headers["x-request-id"].ToString();

        if (!MercadoPagoService.ValidarFirmaWebhook(xSignature, xRequestId, dataId, secret))
        {
            _logger.LogWarning("Firma inválida en webhook de Mercado Pago.");
            return Unauthorized();
        }

        var checkout = await _context.MercadoPagoCheckouts
            .FirstOrDefaultAsync(c => c.IdMercadoPago == dataId, cancellationToken);

        if (checkout is null && Guid.TryParse(externalReference, out var checkoutId))
        {
            checkout = await _context.MercadoPagoCheckouts
                .FirstOrDefaultAsync(c => c.Id == checkoutId, cancellationToken);

            if (checkout is not null && string.IsNullOrWhiteSpace(checkout.IdMercadoPago))
            {
                checkout.IdMercadoPago = dataId;
            }
        }

        if (checkout is null)
        {
            _logger.LogInformation("Webhook de Mercado Pago para checkout desconocido: {OrderId}", dataId);
            return Ok();
        }

        MercadoPagoOrderDetails details;
        try
        {
            details = await _mercadoPago.ObtenerOrderAsync(dataId, cancellationToken);
        }
        catch (MercadoPagoException ex)
        {
            _logger.LogWarning(ex, "No se pudo consultar la orden {OrderId} en Mercado Pago.", dataId);
            return Ok();
        }

        if (details.PaymentStatus == "approved" || details.Status == "processed")
        {
            try
            {
                await _mercadoPago.ConvertirCheckoutSiAprobadoAsync(checkout, details, cancellationToken);
            }
            catch (MercadoPagoException ex)
            {
                _logger.LogWarning(ex, "No se pudo convertir el checkout {CheckoutId}.", checkout.Id);
                checkout.Estado = "Fallido";
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            await _mercadoPago.ActualizarEstadoCheckoutAsync(checkout, details, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok();
    }
}
