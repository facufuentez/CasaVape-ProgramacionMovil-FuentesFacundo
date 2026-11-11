using CasaVapeApp.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace CasaVapeApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<JwtAuthenticationStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());
            builder.Services.AddTransient<AuthHeaderHandler>();
            builder.Services.AddHttpClient("CasaVapeApi", client =>
            {
                client.BaseAddress = new Uri("https://casavape.runasp.net/");
            }).AddHttpMessageHandler<AuthHeaderHandler>();

            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("CasaVapeApi"));
            builder.Services.AddSingleton<CartService>();
            builder.Services.AddScoped<ProductService>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<AccountService>();
            builder.Services.AddScoped<AddressService>();
            builder.Services.AddScoped<OrderService>();
            builder.Services.AddScoped<ShippingMethodService>();
            builder.Services.AddScoped<CategoryService>();
            builder.Services.AddScoped<AdminUserService>();
            builder.Services.AddScoped<AuditService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
