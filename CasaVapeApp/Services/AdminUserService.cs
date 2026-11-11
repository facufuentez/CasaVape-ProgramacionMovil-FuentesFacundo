using System.Net.Http.Json;
using CasaVape.Core.DTOs;

namespace CasaVapeApp.Services;

public class AdminUserService
{
    private readonly HttpClient _http;

    public AdminUserService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<UserDto>>("api/admin/users") ?? new List<UserDto>();
        }
        catch
        {
            return new List<UserDto>();
        }
    }

    public async Task<ApiResult> UpdateRolesAsync(string id, List<string> roles)
    {
        var response = await _http.PutAsJsonAsync($"api/admin/users/{id}/roles", new UpdateUserRolesRequest { Roles = roles });
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult.Failure(await ApiError.ReadAsync(response));
        }

        return ApiResult.Success();
    }

    public async Task<ApiResult> ResetPasswordAsync(string id, ResetPasswordRequest request)
    {
        var response = await _http.PostAsJsonAsync($"api/admin/users/{id}/reset-password", request);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult.Failure(await ApiError.ReadAsync(response));
        }

        return ApiResult.Success();
    }
}
