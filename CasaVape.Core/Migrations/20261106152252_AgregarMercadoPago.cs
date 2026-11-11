using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasaVape.Core.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMercadoPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdMercadoPago",
                table: "Pagos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdMercadoPago",
                table: "Pagos");
        }
    }
}
