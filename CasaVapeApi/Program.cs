using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using CasaVape.Core.Data;
using CasaVape.Core.Models.Identity;
using CasaVape.Core.Services;
using CasaVapeApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using System.Text;
using System.Threading.RateLimiting;

QuestPDF.Settings.License = LicenseType.Community;

LoadDotEnv(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
LoadDotEnv(Path.Combine(AppContext.BaseDirectory, ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    jwtKey = "casa-vape-dev-only-key-change-in-production-2026";
}
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    jwtIssuer = "CasaVapeApi";
}
var jwtAudience = builder.Configuration["Jwt:Audience"];
if (string.IsNullOrWhiteSpace(jwtAudience))
{
    jwtAudience = "CasaVapeApp";
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.Configure<AuthenticationOptions>(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddHttpClient<GeminiChatService>(client => client.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddScoped<GeminiChatService>();
builder.Services.AddHttpClient<MercadoPagoService>(client => client.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddScoped<MercadoPagoService>();
builder.Services.AddScoped<PedidoReporteService>(sp =>
    new PedidoReporteService(sp.GetRequiredService<IWebHostEnvironment>().WebRootPath));
builder.Services.AddScoped<TokenService>();

builder.Services.AddMemoryCache();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("chat", limiterOptions =>
    {
        limiterOptions.PermitLimit = 15;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
        limiterOptions.AutoReplenishment = true;
    });
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("API funcionando"));

// await SeedData.InitializeAsync(app.Services);
//await SeedData.InitializeAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.UseRateLimiter();
app.MapControllers();

app.Run();

static void LoadDotEnv(string path)
{
    if (!File.Exists(path))
    {
        return;
    }

    foreach (var rawLine in File.ReadAllLines(path))
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith('#'))
        {
            continue;
        }

        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
        {
            continue;
        }

        var key = line[..separatorIndex].Trim();
        var value = line[(separatorIndex + 1)..].Trim();

        if (value.Length >= 2 &&
            ((value.StartsWith('"') && value.EndsWith('"')) ||
             (value.StartsWith('\'') && value.EndsWith('\''))))
        {
            value = value[1..^1];
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            continue;
        }

        var environmentKey = key switch
        {
            "MP_ACCESS_TOKEN" => "MercadoPago__AccessToken",
            "MP_WEBHOOK_SECRET" => "MercadoPago__WebhookSecret",
            "MP_BASE_URL" => "MercadoPago__BaseUrl",
            "MP_ENVIRONMENT" => "MercadoPago__Environment",
            "GEMINI_API_KEY" => "Gemini__ApiKey",
            "GEMINI_MODEL" => "Gemini__Model",
            "JWT_SECRET" => "Jwt__Key",
            "JWT_ISSUER" => "Jwt__Issuer",
            "JWT_AUDIENCE" => "Jwt__Audience",
            _ => key
        };

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(environmentKey)))
        {
            Environment.SetEnvironmentVariable(environmentKey, value, EnvironmentVariableTarget.Process);
        }
    }
}

public partial class Program { }
