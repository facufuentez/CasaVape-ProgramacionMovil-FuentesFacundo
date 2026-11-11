using System.Net.Http.Json;
using CasaVape.Core.DTOs;

namespace CasaVapeApp.Services;

public class ShippingMethodService
{
    private readonly HttpClient _http;

    public ShippingMethodService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<ShippingMethodDto>> GetShippingMethodsAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<ShippingMethodDto>>("api/shippingmethods") ?? new List<ShippingMethodDto>();
        }
        catch
        {
            return new List<ShippingMethodDto>();
        }
    }
}
