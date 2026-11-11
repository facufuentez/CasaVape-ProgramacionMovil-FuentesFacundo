using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace CasaVapeApp.Services;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await SecureStorage.GetAsync("access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            return Anonymous();
        }

        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                return Anonymous();
            }

            var payloadBytes = Base64UrlDecode(parts[1]);
            using var document = JsonDocument.Parse(payloadBytes);

            var claims = new List<Claim>();

            foreach (var property in document.RootElement.EnumerateObject())
            {
                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.String:
                        claims.Add(new Claim(property.Name, property.Value.GetString()!));
                        if (property.Name.Equals("role", StringComparison.OrdinalIgnoreCase))
                        {
                            claims.Add(new Claim(ClaimTypes.Role, property.Value.GetString()!));
                        }
                        break;

                    case JsonValueKind.Array:
                        foreach (var item in property.Value.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                            {
                                var value = item.GetString()!;
                                claims.Add(new Claim(property.Name, value));
                                if (property.Name.Equals("role", StringComparison.OrdinalIgnoreCase) ||
                                    property.Name.Equals("roles", StringComparison.OrdinalIgnoreCase))
                                {
                                    claims.Add(new Claim(ClaimTypes.Role, value));
                                }
                            }
                        }
                        break;

                    case JsonValueKind.Number:
                        claims.Add(new Claim(property.Name, property.Value.ToString()));
                        if (property.Name == "exp" && property.Value.TryGetInt64(out var expiration))
                        {
                            if (DateTimeOffset.FromUnixTimeSeconds(expiration) <= DateTimeOffset.UtcNow)
                            {
                                return Anonymous();
                            }
                        }
                        break;

                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        claims.Add(new Claim(property.Name, property.Value.GetBoolean().ToString()));
                        break;
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return Anonymous();
        }
    }

    public void NotifyAuthenticationChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static AuthenticationState Anonymous() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private static byte[] Base64UrlDecode(string input)
    {
        var output = input.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 2:
                output += "==";
                break;
            case 3:
                output += "=";
                break;
        }

        return Convert.FromBase64String(output);
    }
}
