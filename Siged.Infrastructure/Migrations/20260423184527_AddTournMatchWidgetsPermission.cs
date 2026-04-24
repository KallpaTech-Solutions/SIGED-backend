using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Siged.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTournMatchWidgetsPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "IdPermiso", "Categoria", "Descripcion" },
                values: new object[] { "tourn.match.widgets", "TORNEOS", "Widgets de transmisión: tableros y tiempos en vivo" });

            migrationBuilder.InsertData(
                table: "RolPermisos",
                columns: new[] { "PermisosIdPermiso", "RolesId" },
                values: new object[,]
                {
                    { "tourn.match.widgets", 1 },
                    { "tourn.match.widgets", 2 },
                    { "tourn.match.widgets", 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.match.widgets", 1 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.match.widgets", 2 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.match.widgets", 3 });

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "IdPermiso",
                keyValue: "tourn.match.widgets");
        }
    }
}
