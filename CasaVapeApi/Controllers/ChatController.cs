using CasaVape.Core.Models.ViewModels;
using CasaVape.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace CasaVapeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class ChatController : ControllerBase
{
    private readonly GeminiChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(GeminiChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [HttpPost]
    [EnableRateLimiting("chat")]
    public async Task<IActionResult> Post([FromBody] ChatRequest? request, CancellationToken cancellationToken)
    {
        if (request?.Messages is null || request.Messages.Count == 0)
        {
            return BadRequest(new { error = "Envía al menos un mensaje." });
        }

        var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) : null;

        try
        {
            var reply = await _chatService.ResponderAsync(request.Messages, userId, cancellationToken);
            return Ok(reply);
        }
        catch (GeminiChatException ex)
        {
            return StatusCode(ex.StatusCode, new { error = ex.UserMessage });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status408RequestTimeout, new { error = "La consulta fue cancelada o tardó demasiado." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en el chat.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "No se pudo procesar el chat." });
        }
    }
}
