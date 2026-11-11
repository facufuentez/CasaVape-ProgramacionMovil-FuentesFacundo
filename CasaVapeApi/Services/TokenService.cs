using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CasaVape.Core.DTOs;
using CasaVape.Core.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace CasaVapeApi.Services;

public sealed class TokenService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;

    public TokenService(IConfiguration configuration, UserManager<ApplicationUser> userManager)
    {
        _configuration = configuration;
        _userManager = userManager;
    }

    public async Task<AuthResponse> CreateTokensAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = CreateAccessToken(user, roles);
        var refreshToken = user.Id + ":" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        await _userManager.SetAuthenticationTokenAsync(user, "CasaVape", "RefreshToken", refreshToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenMinutes()),
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            Nombre = user.Nombre,
            Apellido = user.Apellido,
            Roles = roles.ToList()
        };
    }

    public async Task<AuthResponse?> RefreshAsync(string refreshToken)
    {
        var parts = refreshToken.Split(':', 2);
        if (parts.Length != 2)
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(parts[0]);
        if (user is null)
        {
            return null;
        }

        var storedToken = await _userManager.GetAuthenticationTokenAsync(user, "CasaVape", "RefreshToken");
        if (storedToken != refreshToken)
        {
            return null;
        }

        await _userManager.RemoveAuthenticationTokenAsync(user, "CasaVape", "RefreshToken");
        return await CreateTokensAsync(user);
    }

    public async Task RevokeAsync(ApplicationUser user)
    {
        await _userManager.RemoveAuthenticationTokenAsync(user, "CasaVape", "RefreshToken");
    }

    private string CreateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var key = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            key = "casa-vape-dev-only-key-change-in-production-2026";
        }

        var issuer = _configuration["Jwt:Issuer"];
        if (string.IsNullOrWhiteSpace(issuer))
        {
            issuer = "CasaVapeApi";
        }

        var audience = _configuration["Jwt:Audience"];
        if (string.IsNullOrWhiteSpace(audience))
        {
            audience = "CasaVapeApp";
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("nombre", user.Nombre),
            new("apellido", user.Apellido)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(GetAccessTokenMinutes()),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetAccessTokenMinutes()
    {
        return int.TryParse(_configuration["Jwt:AccessTokenMinutes"], out var minutes) ? minutes : 15;
    }
}
