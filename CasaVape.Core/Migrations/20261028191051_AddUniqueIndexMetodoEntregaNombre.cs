using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasaVape.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexMetodoEntregaNombre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MetodosEntrega_Nombre",
                table: "MetodosEntrega",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MetodosEntrega_Nombre",
                table: "MetodosEntrega");
        }
    }
}
