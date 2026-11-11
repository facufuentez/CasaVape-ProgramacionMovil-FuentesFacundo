using System.Net.Http.Json;
using System.Text.Json;
using CasaVape.Core.DTOs;

namespace CasaVapeApp.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly JwtAuthenticationStateProvider _authState;

    public AuthService(HttpClient http, JwtAuthenticationStateProvider authState)
    {
        _http = http;
        _authState = authState;
    }

    public async Task<AuthResponse?> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (auth is not null)
        {
            await SecureStorage.SetAsync("access_token", auth.AccessToken);
            await SecureStorage.SetAsync("refresh_token", auth.RefreshToken);
            _authState.NotifyAuthenticationChanged();
        }

        return auth;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (auth is not null)
        {
            await SecureStorage.SetAsync("access_token", auth.AccessToken);
            await SecureStorage.SetAsync("refresh_token", auth.RefreshToken);
            _authState.NotifyAuthenticationChanged();
        }

        return auth;
    }

    public async Task LogoutAsync()
    {
        SecureStorage.Remove("access_token");
        SecureStorage.Remove("refresh_token");
        _authState.NotifyAuthenticationChanged();
    }

    public async Task<string?> GetTokenAsync()
    {
        return await SecureStorage.GetAsync("access_token");
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        return !string.IsNullOrWhiteSpace(await GetTokenAsync());
    }
}
