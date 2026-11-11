using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CasaVape.Core.Models.Entities;
using CasaVape.Core.Models.Identity;

namespace CasaVape.Core.Data
{
    public static class SeedData
    {
        public const string AdminRole = "Administrador";
        public const string ClientRole = "Cliente";

        private const string AdminEmail = "facu.fuentez@gmail.com";
        private const string AdminPassword = "facuadmin11";

        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<ApplicationDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            await SeedRolesAsync(roleManager);
            await SeedAdminUserAsync(userManager);
            await SeedProvinciasAsync(context);
            await SeedEstadosPedidoAsync(context);
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in new[] { AdminRole, ClientRole })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
        {
            var adminUser = await userManager.FindByEmailAsync(AdminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = AdminEmail,
                    Email = AdminEmail,
                    Nombre = "Facundo",
                    Apellido = "Fuentes",
                    FechaNacimiento = new DateTime(2004, 5, 15),
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, AdminPassword);
                Console.WriteLine("SEED_ADMIN_RESULT: " + string.Join(",", result.Errors.Select(e => e.Code)));
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, AdminRole);
                }
            }
            else if (!await userManager.IsInRoleAsync(adminUser, AdminRole))
            {
                await userManager.AddToRoleAsync(adminUser, AdminRole);
            }
        }

        private static async Task SeedProvinciasAsync(ApplicationDbContext context)
        {
            if (await context.Provincias.AnyAsync())
            {
                return;
            }

            var provincias = new[]
            {
                "Buenos Aires",
                "Ciudad Autónoma de Buenos Aires",
                "Catamarca",
                "Chaco",
                "Chubut",
                "Córdoba",
                "Corrientes",
                "Entre Ríos",
                "Formosa",
                "Jujuy",
                "La Pampa",
                "La Rioja",
                "Mendoza",
                "Misiones",
                "Neuquén",
                "Río Negro",
                "Salta",
                "San Juan",
                "San Luis",
                "Santa Cruz",
                "Santa Fe",
                "Santiago del Estero",
                "Tierra del Fuego, Antártida e Islas del Atlántico Sur",
                "Tucumán"
            };

            context.Provincias.AddRange(provincias.Select(nombre => new Provincia { Nombre = nombre }));
            await context.SaveChangesAsync();
        }

        private static async Task SeedEstadosPedidoAsync(ApplicationDbContext context)
        {
            if (await context.EstadosPedido.AnyAsync())
            {
                return;
            }

            var estados = new[]
            {
                "Pendiente",
                "Pagado",
                "Enviado",
                "Entregado",
                "Cancelado"
            };

            context.EstadosPedido.AddRange(estados.Select(nombre => new EstadoPedido { Nombre = nombre }));
            await context.SaveChangesAsync();
        }
    }
}
