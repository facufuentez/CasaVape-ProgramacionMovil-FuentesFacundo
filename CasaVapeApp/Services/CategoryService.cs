using System.Net.Http.Json;
using CasaVape.Core.DTOs;

namespace CasaVapeApp.Services;

public class CategoryService
{
    private readonly HttpClient _http;

    public CategoryService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<CategoryDto>>("api/categories") ?? new List<CategoryDto>();
        }
        catch
        {
            return new List<CategoryDto>();
        }
    }

    public async Task<ApiResult<CategoryDto>> CreateAsync(CreateCategoryRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/categories", request);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<CategoryDto>.Failure(await ApiError.ReadAsync(response));
        }

        var category = await response.Content.ReadFromJsonAsync<CategoryDto>();
        return category is null
            ? ApiResult<CategoryDto>.Failure("Respuesta inválida del servidor.")
            : ApiResult<CategoryDto>.Success(category);
    }

    public async Task<ApiResult<CategoryDto>> UpdateAsync(int id, CreateCategoryRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/categories/{id}", request);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<CategoryDto>.Failure(await ApiError.ReadAsync(response));
        }

        var category = await response.Content.ReadFromJsonAsync<CategoryDto>();
        return category is null
            ? ApiResult<CategoryDto>.Failure("Respuesta inválida del servidor.")
            : ApiResult<CategoryDto>.Success(category);
    }

    public async Task<ApiResult> DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/categories/{id}");
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult.Failure(await ApiError.ReadAsync(response));
        }

        return ApiResult.Success();
    }
}
