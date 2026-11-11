using System.Net.Http.Json;
using CasaVape.Core.DTOs;

namespace CasaVapeApp.Services;

public class AccountService
{
    private readonly HttpClient _http;

    public AccountService(HttpClient http)
    {
        _http = http;
    }

    public Uri? BaseAddress => _http.BaseAddress;

    public async Task<ProfileResponse?> GetProfileAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<ProfileResponse>("api/auth/me");
        }
        catch
        {
            return null;
        }
    }

    public async Task<ApiResult<ProfileResponse>> UpdateProfileAsync(UpdateProfileRequest request)
    {
        var response = await _http.PutAsJsonAsync("api/auth/profile", request);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<ProfileResponse>.Failure(await ApiError.ReadAsync(response));
        }

        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        return profile is null
            ? ApiResult<ProfileResponse>.Failure("Respuesta inválida del servidor.")
            : ApiResult<ProfileResponse>.Success(profile);
    }

    public async Task<ApiResult> ChangePasswordAsync(ChangePasswordRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/change-password", request);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult.Failure(await ApiError.ReadAsync(response));
        }

        return ApiResult.Success();
    }
}
