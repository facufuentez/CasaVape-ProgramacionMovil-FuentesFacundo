using System.Net.Http.Json;
using CasaVape.Core.DTOs;
using CasaVapeApp.Models;

namespace CasaVapeApp.Services;

public class AddressService
{
    private readonly HttpClient _http;

    public AddressService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<AddressDto>> GetAddressesAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<AddressDto>>("api/addresses") ?? new List<AddressDto>();
        }
        catch
        {
            return new List<AddressDto>();
        }
    }

    public async Task<List<ProvinceDto>> GetProvincesAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<ProvinceDto>>("api/addresses/provinces") ?? new List<ProvinceDto>();
        }
        catch
        {
            return new List<ProvinceDto>();
        }
    }

    public async Task<ApiResult<AddressDto>> CreateAsync(CreateAddressRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/addresses", request);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<AddressDto>.Failure(await ApiError.ReadAsync(response));
        }

        var address = await response.Content.ReadFromJsonAsync<AddressDto>();
        return address is null
            ? ApiResult<AddressDto>.Failure("Respuesta inválida del servidor.")
            : ApiResult<AddressDto>.Success(address);
    }

    public async Task<ApiResult<AddressDto>> UpdateAsync(int id, CreateAddressRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/addresses/{id}", request);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<AddressDto>.Failure(await ApiError.ReadAsync(response));
        }

        var address = await response.Content.ReadFromJsonAsync<AddressDto>();
        return address is null
            ? ApiResult<AddressDto>.Failure("Respuesta inválida del servidor.")
            : ApiResult<AddressDto>.Success(address);
    }

    public async Task<ApiResult> DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/addresses/{id}");
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult.Failure(await ApiError.ReadAsync(response));
        }

        return ApiResult.Success();
    }
}
