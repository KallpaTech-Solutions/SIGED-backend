using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Siged.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMatchEventsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("6330b8b4-7556-4612-83a6-6fb67a5696b8"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("734fd177-0871-4578-ab4f-a2bda1a92f21"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("90db4261-50ea-4e12-b859-793cc0fa8176"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("d856d085-af10-4978-82a9-1a30a943cd1d"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("fbfd5d17-6804-4771-9103-8d15b0e42630"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("a2b14714-3c13-4b8a-8041-97ade2973496"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("cab256fa-e8d1-4fd0-8c20-2abf28169604"));

            migrationBuilder.InsertData(
                table: "DisciplineRules",
                columns: new[] { "Id", "DisciplineId", "RuleKey", "RuleValue" },
                values: new object[,]
                {
                    { new Guid("160fc774-6f3b-42e6-a5df-3591042b05c5"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "USA_SETS", "True" },
                    { new Guid("27d5cba2-2e57-48ad-8365-3336a8f62338"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "TIENE_TARJETAS", "True" },
                    { new Guid("aaf07540-f29a-48bb-8da6-7c64f6146b8d"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "PUNTOS_POR_VICTORIA", "2" },
                    { new Guid("ac3e3888-5d4d-4917-b280-f61a8d23a6e1"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "PUNTOS_POR_VICTORIA", "3" },
                    { new Guid("c478bc10-1385-4eb4-a035-26b1f4ba018a"), new Guid("c1d2e3f4-a5b6-4c8d-9e0f-1a2b3c4d5e6f"), "CANTIDAD_PERIODOS", "4" }
                });

            migrationBuilder.UpdateData(
                table: "Personas",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaRegistro",
                value: new DateTime(2026, 4, 16, 17, 43, 50, 589, DateTimeKind.Utc).AddTicks(8425));

            migrationBuilder.InsertData(
                table: "Venues",
                columns: new[] { "Id", "Address", "Capacity", "Name" },
                values: new object[,]
                {
                    { new Guid("0442d2ac-bf30-4f9b-9380-d0da1350c8b2"), "Campus Principal", 5000, "Estadio Universitario UNAS" },
                    { new Guid("82db8686-bf97-45cd-89a8-ab8fa7079040"), "Pabellón de Sistemas", 200, "Losa Deportiva FIIS" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("160fc774-6f3b-42e6-a5df-3591042b05c5"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("27d5cba2-2e57-48ad-8365-3336a8f62338"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("aaf07540-f29a-48bb-8da6-7c64f6146b8d"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("ac3e3888-5d4d-4917-b280-f61a8d23a6e1"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("c478bc10-1385-4eb4-a035-26b1f4ba018a"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("0442d2ac-bf30-4f9b-9380-d0da1350c8b2"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("82db8686-bf97-45cd-89a8-ab8fa7079040"));

            migrationBuilder.InsertData(
                table: "DisciplineRules",
                columns: new[] { "Id", "DisciplineId", "RuleKey", "RuleValue" },
                values: new object[,]
                {
                    { new Guid("6330b8b4-7556-4612-83a6-6fb67a5696b8"), new Guid("c1d2e3f4-a5b6-4c8d-9e0f-1a2b3c4d5e6f"), "CANTIDAD_PERIODOS", "4" },
                    { new Guid("734fd177-0871-4578-ab4f-a2bda1a92f21"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "PUNTOS_POR_VICTORIA", "2" },
                    { new Guid("90db4261-50ea-4e12-b859-793cc0fa8176"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "USA_SETS", "True" },
                    { new Guid("d856d085-af10-4978-82a9-1a30a943cd1d"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "TIENE_TARJETAS", "True" },
                    { new Guid("fbfd5d17-6804-4771-9103-8d15b0e42630"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "PUNTOS_POR_VICTORIA", "3" }
                });

            migrationBuilder.UpdateData(
                table: "Personas",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaRegistro",
                value: new DateTime(2026, 4, 16, 17, 36, 2, 24, DateTimeKind.Utc).AddTicks(2836));

            migrationBuilder.InsertData(
                table: "Venues",
                columns: new[] { "Id", "Address", "Capacity", "Name" },
                values: new object[,]
                {
                    { new Guid("a2b14714-3c13-4b8a-8041-97ade2973496"), "Campus Principal", 5000, "Estadio Universitario UNAS" },
                    { new Guid("cab256fa-e8d1-4fd0-8c20-2abf28169604"), "Pabellón de Sistemas", 200, "Losa Deportiva FIIS" }
                });
        }
    }
}
