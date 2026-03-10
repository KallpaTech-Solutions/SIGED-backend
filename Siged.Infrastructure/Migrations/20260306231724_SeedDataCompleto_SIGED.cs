using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Siged.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedDataCompleto_SIGED : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Nivel",
                table: "Roles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "IdPermiso", "Categoria", "Descripcion" },
                values: new object[] { "security.role.view", "SEGURIDAD", "Ver roles y permisos" });

            migrationBuilder.InsertData(
                table: "Personas",
                columns: new[] { "Id", "Apellidos", "CodigoEstudiante", "Correo", "DNI", "Discriminator", "EstaMatriculado", "FechaRegistro", "FotoPath", "Nombres", "Telefono" },
                values: new object[] { 3, "Alumno FIIS", "0020210456", null, "88888888", "Estudiante", true, new DateTime(2026, 3, 6, 23, 17, 24, 13, DateTimeKind.Utc).AddTicks(6810), null, "Pedro", null });

            migrationBuilder.InsertData(
                table: "Personas",
                columns: new[] { "Id", "Apellidos", "Cargo", "Correo", "DNI", "Discriminator", "FotoPath", "Nombres", "Oficina", "Telefono" },
                values: new object[] { 4, "Docente Encargada", "Director de Escuela", null, "44444444", "Encargado", null, "Maria", "Pabellón Central", null });

            migrationBuilder.InsertData(
                table: "RolPermisos",
                columns: new[] { "PermisosIdPermiso", "RolesId" },
                values: new object[] { "security.role.manage", 2 });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Descripcion", "Nivel" },
                values: new object[] { "Control total (OTI)", 100 });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Descripcion", "Nivel" },
                values: new object[] { "Personal de oficinas centrales", 80 });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Descripcion", "Nivel" },
                values: new object[] { "Docente o administrativo de facultad", 50 });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Descripcion", "Nivel" },
                values: new object[] { "Alumno regular UNAS", 10 });

            migrationBuilder.InsertData(
                table: "RolPermisos",
                columns: new[] { "PermisosIdPermiso", "RolesId" },
                values: new object[,]
                {
                    { "security.role.view", 1 },
                    { "security.role.view", 2 }
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "CreadoPorUsuarioId", "EstaActivo", "FechaRegistro", "OrganizacionId", "PasswordHash", "PersonaId", "RequiereCambioPassword", "RolId", "Username" },
                values: new object[,]
                {
                    { 3, null, true, new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc), 1, "$2a$12$eL7jdgl1iOF508ePe1ScyOrYqdzdDedn1yo5WuGrj1E1.ZovUk0Xe", 3, true, 4, "estudiante_test" },
                    { 4, null, true, new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc), 1, "$2a$12$eL7jdgl1iOF508ePe1ScyOrYqdzdDedn1yo5WuGrj1E1.ZovUk0Xe", 4, true, 3, "encargado_test" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "security.role.manage", 2 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "security.role.view", 1 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "security.role.view", 2 });

            migrationBuilder.DeleteData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "IdPermiso",
                keyValue: "security.role.view");

            migrationBuilder.DeleteData(
                table: "Personas",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Personas",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "Nivel",
                table: "Roles");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "Descripcion",
                value: "Control total del sistema (OTI)");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "Descripcion",
                value: "Personal administrativo de oficinas centrales");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "Descripcion",
                value: "Docente o administrativo encargado de facultad");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                column: "Descripcion",
                value: "Alumno regular de la UNAS");
        }
    }
}
