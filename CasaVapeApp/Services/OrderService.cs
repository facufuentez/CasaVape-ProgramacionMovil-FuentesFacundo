using System.Net.Http.Json;
using CasaVape.Core.DTOs;

namespace CasaVapeApp.Services;

public class OrderService
{
    private readonly HttpClient _http;

    public OrderService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<OrderDto>> GetMyOrdersAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<OrderDto>>("api/orders") ?? new List<OrderDto>();
        }
        catch
        {
            return new List<OrderDto>();
        }
    }

    public async Task<OrderDto?> GetOrderAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<OrderDto>($"api/orders/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<OrderDto>> GetAdminOrdersAsync(string? estado = null, string? q = null)
    {
        try
        {
            var url = "api/orders/admin";
            var separator = "?";
            if (!string.IsNullOrWhiteSpace(estado))
            {
                url += $"{separator}estado={Uri.EscapeDataString(estado)}";
                separator = "&";
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                url += $"{separator}q={Uri.EscapeDataString(q)}";
            }

            return await _http.GetFromJsonAsync<List<OrderDto>>(url) ?? new List<OrderDto>();
        }
        catch
        {
            return new List<OrderDto>();
        }
    }

    public async Task<List<OrderDto>> GetReportAsync(DateTime? desde = null, DateTime? hasta = null, string? estado = null)
    {
        try
        {
            var url = "api/orders/admin/reporte";
            var separator = "?";
            if (desde.HasValue)
            {
                url += $"{separator}desde={desde.Value:yyyy-MM-dd}";
                separator = "&";
            }
            if (hasta.HasValue)
            {
                url += $"{separator}hasta={hasta.Value:yyyy-MM-dd}";
                separator = "&";
            }
            if (!string.IsNullOrWhiteSpace(estado))
            {
                url += $"{separator}estado={Uri.EscapeDataString(estado)}";
            }

            return await _http.GetFromJsonAsync<List<OrderDto>>(url) ?? new List<OrderDto>();
        }
        catch
        {
            return new List<OrderDto>();
        }
    }

    public async Task<ApiResult<string>> DownloadReportPdfAsync(DateTime? desde = null, DateTime? hasta = null, string? estado = null)
    {
        try
        {
            var url = "api/orders/admin/reporte/pdf";
            var separator = "?";
            if (desde.HasValue)
            {
                url += $"{separator}desde={desde.Value:yyyy-MM-dd}";
                separator = "&";
            }
            if (hasta.HasValue)
            {
                url += $"{separator}hasta={hasta.Value:yyyy-MM-dd}";
                separator = "&";
            }
            if (!string.IsNullOrWhiteSpace(estado))
            {
                url += $"{separator}estado={Uri.EscapeDataString(estado)}";
            }

            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<string>.Failure(await ApiError.ReadAsync(response));
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            var path = Path.Combine(FileSystem.AppDataDirectory, $"reporte-pedidos-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
            await using var file = File.Create(path);
            await stream.CopyToAsync(file);

            return ApiResult<string>.Success(path);
        }
        catch (Exception ex)
        {
            return ApiResult<string>.Failure(ex.Message);
        }
    }

    public async Task<ApiResult<CheckoutResponseDto>> CheckoutAsync(CheckoutRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/orders/checkout", request);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<CheckoutResponseDto>.Failure(await ApiError.ReadAsync(response));
        }

        var checkout = await response.Content.ReadFromJsonAsync<CheckoutResponseDto>();
        return checkout is null
            ? ApiResult<CheckoutResponseDto>.Failure("Respuesta inválida del servidor.")
            : ApiResult<CheckoutResponseDto>.Success(checkout);
    }

    public async Task<ApiResult> ChangeStatusAsync(int id, string nuevoEstado)
    {
        var response = await _http.PutAsJsonAsync($"api/orders/{id}/status", nuevoEstado);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult.Failure(await ApiError.ReadAsync(response));
        }

        return ApiResult.Success();
    }

    public async Task<ApiResult> ConfirmPaymentAsync(int id)
    {
        var response = await _http.PostAsync($"api/orders/{id}/confirm-payment", null);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult.Failure(await ApiError.ReadAsync(response));
        }

        return ApiResult.Success();
    }

    public async Task<ApiResult> RejectPaymentAsync(int id)
    {
        var response = await _http.PostAsync($"api/orders/{id}/reject-payment", null);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult.Failure(await ApiError.ReadAsync(response));
        }

        return ApiResult.Success();
    }

    public async Task<ApiResult<string>> DownloadReceiptAsync(int id)
    {
        try
        {
            using var response = await _http.GetAsync($"api/orders/{id}/receipt", HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<string>.Failure(await ApiError.ReadAsync(response));
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            var path = Path.Combine(FileSystem.AppDataDirectory, $"comprobante-pedido-{id}.pdf");
            await using var file = File.Create(path);
            await stream.CopyToAsync(file);

            return ApiResult<string>.Success(path);
        }
        catch (Exception ex)
        {
            return ApiResult<string>.Failure(ex.Message);
        }
    }
}
