using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CasaVape.Core.Models.Entities;
using CasaVape.Core.Models.Identity;

namespace CasaVape.Core.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Provincia> Provincias { get; set; }
        public DbSet<Direccion> Direcciones { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<EstadoPedido> EstadosPedido { get; set; }
        public DbSet<MetodoEntrega> MetodosEntrega { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<DetallePedido> DetallesPedido { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<Auditoria> Auditorias { get; set; }
        public DbSet<MercadoPagoCheckout> MercadoPagoCheckouts { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Precision para decimales
            builder.Entity<Producto>()
                .Property(p => p.Precio)
                .HasPrecision(18, 2);

            builder.Entity<MetodoEntrega>()
                .Property(m => m.Costo)
                .HasPrecision(18, 2);

            builder.Entity<Pedido>()
                .Property(p => p.CostoEnvio)
                .HasPrecision(18, 2);

            builder.Entity<DetallePedido>()
                .Property(d => d.PrecioUnitario)
                .HasPrecision(18, 2);

            builder.Entity<Pago>()
                .Property(p => p.Monto)
                .HasPrecision(18, 2);

            builder.Entity<MercadoPagoCheckout>()
                .Property(p => p.Total)
                .HasPrecision(18, 2);

            builder.Entity<MercadoPagoCheckout>()
                .Property(p => p.CostoEnvio)
                .HasPrecision(18, 2);

            // Relaciones y restricciones (evitar borrado en cascada no deseado)
            builder.Entity<Direccion>()
                .HasOne(d => d.Usuario)
                .WithMany(u => u.Direcciones)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Direccion>()
                .HasOne(d => d.Provincia)
                .WithMany(p => p.Direcciones)
                .HasForeignKey(d => d.IdProvincia)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Pedido>()
                .HasOne(p => p.Usuario)
                .WithMany(u => u.Pedidos)
                .HasForeignKey(p => p.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Pedido>()
                .HasOne(p => p.DireccionEnvio)
                .WithMany(d => d.PedidosEnvio)
                .HasForeignKey(p => p.IdDireccionEnvio)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Pedido>()
                .HasOne(p => p.EstadoPedido)
                .WithMany(e => e.Pedidos)
                .HasForeignKey(p => p.IdEstadoPedido)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Pedido>()
                .HasOne(p => p.MetodoEntrega)
                .WithMany(m => m.Pedidos)
                .HasForeignKey(p => p.IdMetodoEntrega)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DetallePedido>()
                .HasOne(d => d.Pedido)
                .WithMany(p => p.DetallesPedido)
                .HasForeignKey(d => d.IdPedido)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DetallePedido>()
                .HasOne(d => d.Producto)
                .WithMany(p => p.DetallesPedido)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Pago>()
                .HasOne(p => p.Pedido)
                .WithMany(ped => ped.Pagos)
                .HasForeignKey(p => p.IdPedido)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Auditoria>()
                .HasOne(a => a.Usuario)
                .WithMany(u => u.Auditorias)
                .HasForeignKey(a => a.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Producto>()
                .HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.IdCategoria)
                .OnDelete(DeleteBehavior.Restrict);

            // Nombre de categoría único
            builder.Entity<Categoria>()
                .HasIndex(c => c.Nombre)
                .IsUnique();

            // Nombre de método de entrega único
            builder.Entity<MetodoEntrega>()
                .HasIndex(m => m.Nombre)
                .IsUnique();
        }
    }
}
