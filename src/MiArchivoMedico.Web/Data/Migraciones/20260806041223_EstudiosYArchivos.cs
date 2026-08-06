using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiArchivoMedico.Web.Data.Migraciones
{
    /// <inheritdoc />
    public partial class EstudiosYArchivos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Estudios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Profesional = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Institucion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreadoUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    TituloNormalizado = table.Column<string>(type: "TEXT", nullable: false),
                    ProfesionalNormalizado = table.Column<string>(type: "TEXT", nullable: false),
                    InstitucionNormalizada = table.Column<string>(type: "TEXT", nullable: false),
                    DescripcionNormalizada = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estudios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Archivos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EstudioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: false),
                    NombreOriginal = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    TipoMime = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TamanoEnBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    HashSha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CargadoUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Archivos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Archivos_Estudios_EstudioId",
                        column: x => x.EstudioId,
                        principalTable: "Estudios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Etiquetas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EstudioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: false),
                    Texto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TextoNormalizado = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Etiquetas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Etiquetas_Estudios_EstudioId",
                        column: x => x.EstudioId,
                        principalTable: "Estudios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Archivos_EstudioId",
                table: "Archivos",
                column: "EstudioId");

            migrationBuilder.CreateIndex(
                name: "IX_Estudios_DescripcionNormalizada",
                table: "Estudios",
                column: "DescripcionNormalizada");

            migrationBuilder.CreateIndex(
                name: "IX_Estudios_InstitucionNormalizada",
                table: "Estudios",
                column: "InstitucionNormalizada");

            migrationBuilder.CreateIndex(
                name: "IX_Estudios_OwnerId_Fecha",
                table: "Estudios",
                columns: new[] { "OwnerId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Estudios_ProfesionalNormalizado",
                table: "Estudios",
                column: "ProfesionalNormalizado");

            migrationBuilder.CreateIndex(
                name: "IX_Estudios_TituloNormalizado",
                table: "Estudios",
                column: "TituloNormalizado");

            migrationBuilder.CreateIndex(
                name: "IX_Etiquetas_EstudioId",
                table: "Etiquetas",
                column: "EstudioId");

            migrationBuilder.CreateIndex(
                name: "IX_Etiquetas_TextoNormalizado",
                table: "Etiquetas",
                column: "TextoNormalizado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Archivos");

            migrationBuilder.DropTable(
                name: "Etiquetas");

            migrationBuilder.DropTable(
                name: "Estudios");
        }
    }
}
