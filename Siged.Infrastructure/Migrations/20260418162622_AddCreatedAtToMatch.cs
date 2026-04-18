using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Siged.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAtToMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("0dced771-6df8-4a70-9456-0b38aad9ed33"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("428e6faf-ae8d-4e67-a82e-b226227bcb23"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("50d800a5-fd68-4100-bfa3-9c2d814cb0e7"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("6181c515-b50a-48ec-9004-af8b219e3a80"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("b66d0d81-16f8-4858-ada7-69663c212339"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("9b38126e-0f1c-42af-8b7d-99c810f5262a"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("bb8f42ce-4740-4b4f-b0b5-5e38cafb1b29"));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.InsertData(
                table: "DisciplineRules",
                columns: new[] { "Id", "DisciplineId", "RuleKey", "RuleValue" },
                values: new object[,]
                {
                    { new Guid("00be158b-0563-49d4-a440-a0de7ea1c8e5"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "USA_SETS", "True" },
                    { new Guid("0f5bb5ce-011a-4ec7-a02a-9a3353709443"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "PUNTOS_POR_VICTORIA", "3" },
                    { new Guid("273b94a9-a3b9-4a54-a7a0-492bd4143326"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "PUNTOS_POR_VICTORIA", "2" },
                    { new Guid("c3d5ebfe-1476-4322-9026-a08e2ecffbeb"), new Guid("c1d2e3f4-a5b6-4c8d-9e0f-1a2b3c4d5e6f"), "CANTIDAD_PERIODOS", "4" },
                    { new Guid("e305600e-7c00-44f3-8cb2-52e7970b2920"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "TIENE_TARJETAS", "True" }
                });

            migrationBuilder.UpdateData(
                table: "Personas",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaRegistro",
                value: new DateTime(2026, 4, 18, 16, 26, 21, 715, DateTimeKind.Utc).AddTicks(9131));

            migrationBuilder.InsertData(
                table: "Venues",
                columns: new[] { "Id", "Address", "Capacity", "Name" },
                values: new object[,]
                {
                    { new Guid("4c8cab32-4239-4396-b759-649c7ff33af0"), "Pabellón de Sistemas", 200, "Losa Deportiva FIIS" },
                    { new Guid("7c6e7706-38d5-4931-a7b5-9f3f6d061f41"), "Campus Principal", 5000, "Estadio Universitario UNAS" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("00be158b-0563-49d4-a440-a0de7ea1c8e5"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("0f5bb5ce-011a-4ec7-a02a-9a3353709443"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("273b94a9-a3b9-4a54-a7a0-492bd4143326"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("c3d5ebfe-1476-4322-9026-a08e2ecffbeb"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("e305600e-7c00-44f3-8cb2-52e7970b2920"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("4c8cab32-4239-4396-b759-649c7ff33af0"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("7c6e7706-38d5-4931-a7b5-9f3f6d061f41"));

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Matches");

            migrationBuilder.InsertData(
                table: "DisciplineRules",
                columns: new[] { "Id", "DisciplineId", "RuleKey", "RuleValue" },
                values: new object[,]
                {
                    { new Guid("0dced771-6df8-4a70-9456-0b38aad9ed33"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "TIENE_TARJETAS", "True" },
                    { new Guid("428e6faf-ae8d-4e67-a82e-b226227bcb23"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "PUNTOS_POR_VICTORIA", "3" },
                    { new Guid("50d800a5-fd68-4100-bfa3-9c2d814cb0e7"), new Guid("c1d2e3f4-a5b6-4c8d-9e0f-1a2b3c4d5e6f"), "CANTIDAD_PERIODOS", "4" },
                    { new Guid("6181c515-b50a-48ec-9004-af8b219e3a80"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "PUNTOS_POR_VICTORIA", "2" },
                    { new Guid("b66d0d81-16f8-4858-ada7-69663c212339"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "USA_SETS", "True" }
                });

            migrationBuilder.UpdateData(
                table: "Personas",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaRegistro",
                value: new DateTime(2026, 4, 18, 16, 12, 47, 544, DateTimeKind.Utc).AddTicks(2240));

            migrationBuilder.InsertData(
                table: "Venues",
                columns: new[] { "Id", "Address", "Capacity", "Name" },
                values: new object[,]
                {
                    { new Guid("9b38126e-0f1c-42af-8b7d-99c810f5262a"), "Pabellón de Sistemas", 200, "Losa Deportiva FIIS" },
                    { new Guid("bb8f42ce-4740-4b4f-b0b5-5e38cafb1b29"), "Campus Principal", 5000, "Estadio Universitario UNAS" }
                });
        }
    }
}
