using System.Net.Http.Json;
using CasaVape.Core.DTOs;

namespace CasaVapeApp.Services;

public class ProductService
{
    private readonly HttpClient _http;

    public ProductService(HttpClient http)
    {
        _http = http;
    }

    public Uri? BaseAddress => _http.BaseAddress;

    public async Task<List<ProductDto>> GetProductsAsync(string? q = null, int? categoryId = null, int page = 1, int pageSize = 12)
    {
        var url = $"api/products?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(q))
        {
            url += $"&q={Uri.EscapeDataString(q)}";
        }
        if (categoryId.HasValue)
        {
            url += $"&categoryId={categoryId.Value}";
        }

        var products = await _http.GetFromJsonAsync<List<ProductDto>>(url) ?? new List<ProductDto>();
        foreach (var product in products)
        {
            product.ImagenUrl = ToAbsoluteUrl(product.ImagenUrl);
        }

        return products;
    }

    public async Task<ProductDto?> GetProductAsync(int id)
    {
        try
        {
            var product = await _http.GetFromJsonAsync<ProductDto>($"api/products/{id}");
            if (product is not null)
            {
                product.ImagenUrl = ToAbsoluteUrl(product.ImagenUrl);
            }

            return product;
        }
        catch
        {
            return null;
        }
    }

    private string? ToAbsoluteUrl(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl))
        {
            return relativeUrl;
        }

        if (Uri.TryCreate(relativeUrl, UriKind.Absolute, out _))
        {
            return relativeUrl;
        }

        if (_http.BaseAddress is null)
        {
            return relativeUrl;
        }

        return new Uri(_http.BaseAddress, relativeUrl.TrimStart('/')).ToString();
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        return await _http.GetFromJsonAsync<List<CategoryDto>>("api/categories") ?? new List<CategoryDto>();
    }

    public async Task<List<ProductDto>> GetAdminProductsAsync(string? q = null)
    {
        try
        {
            var url = "api/products?page=1&pageSize=500&todos=true";
            if (!string.IsNullOrWhiteSpace(q))
            {
                url += $"&q={Uri.EscapeDataString(q)}";
            }

            var products = await _http.GetFromJsonAsync<List<ProductDto>>(url) ?? new List<ProductDto>();
            foreach (var product in products)
            {
                product.ImagenUrl = ToAbsoluteUrl(product.ImagenUrl);
            }

            return products;
        }
        catch
        {
            return new List<ProductDto>();
        }
    }

    public async Task<ApiResult<ProductDto>> CreateAsync(CreateProductRequest request, string? imagenPath = null)
    {
        using var content = BuildProductContent(request, imagenPath);
        var response = await _http.PostAsync("api/products", content);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<ProductDto>.Failure(await ApiError.ReadAsync(response));
        }

        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        if (product is not null)
        {
            product.ImagenUrl = ToAbsoluteUrl(product.ImagenUrl);
        }

        return product is null
            ? ApiResult<ProductDto>.Failure("Respuesta inválida del servidor.")
            : ApiResult<ProductDto>.Success(product);
    }

    public async Task<ApiResult<ProductDto>> UpdateAsync(int id, UpdateProductRequest request, string? imagenPath = null)
    {
        using var content = BuildProductContent(request, imagenPath);
        content.Add(new StringContent(id.ToString()), "IdProducto");
        var response = await _http.PutAsync($"api/products/{id}", content);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<ProductDto>.Failure(await ApiError.ReadAsync(response));
        }

        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        if (product is not null)
        {
            product.ImagenUrl = ToAbsoluteUrl(product.ImagenUrl);
        }

        return product is null
            ? ApiResult<ProductDto>.Failure("Respuesta inválida del servidor.")
            : ApiResult<ProductDto>.Success(product);
    }

    public async Task<ApiResult> DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/products/{id}");
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult.Failure(await ApiError.ReadAsync(response));
        }

        return ApiResult.Success();
    }

    private static MultipartFormDataContent BuildProductContent(CreateProductRequest request, string? imagenPath)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(request.IdCategoria.ToString()), "IdCategoria" },
            { new StringContent(request.Nombre), "Nombre" },
            { new StringContent(request.Descripcion ?? string.Empty), "Descripcion" },
            { new StringContent(request.Precio.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Precio" },
            { new StringContent(request.Stock.ToString()), "Stock" },
            { new StringContent(request.Activo.ToString()), "Activo" }
        };

        if (!string.IsNullOrWhiteSpace(imagenPath) && File.Exists(imagenPath))
        {
            var extension = Path.GetExtension(imagenPath).ToLowerInvariant();
            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            var fileContent = new StreamContent(File.OpenRead(imagenPath));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "imagenArchivo", Path.GetFileName(imagenPath));
        }

        return content;
    }
}
