using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegistroConsultas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAreasAndZonasFiliales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "areas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codigo_area = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    reg_anulado = table.Column<bool>(type: "bit", nullable: false),
                    creado_el = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modificado_el = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_areas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "zonas_filiales",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codigo_zona = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    reg_anulado = table.Column<bool>(type: "bit", nullable: false),
                    creado_el = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modificado_el = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_zonas_filiales", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_areas_codigo_area",
                table: "areas",
                column: "codigo_area",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_zonas_filiales_codigo_zona",
                table: "zonas_filiales",
                column: "codigo_zona",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "areas");

            migrationBuilder.DropTable(
                name: "zonas_filiales");
        }
    }
}
