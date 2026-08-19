using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiArchivoMedico.Web.Data.Migraciones
{
    /// <inheritdoc />
    public partial class IntentosDeInicioDeSesion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Intentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NombreDeUsuarioNormalizado = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Fallos = table.Column<int>(type: "INTEGER", nullable: false),
                    UltimoFalloEn = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intentos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Intentos_NombreDeUsuarioNormalizado",
                table: "Intentos",
                column: "NombreDeUsuarioNormalizado",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Intentos");
        }
    }
}
