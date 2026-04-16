using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Siged.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedTournamentData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Disciplines",
                columns: new[] { "Id", "IconUrl", "Name" },
                values: new object[,]
                {
                    { new Guid("1a2b3c4d-5e6f-4a8b-9c0d-1e2f3a4b5c6d"), "futsal-pitch", "Futsal" },
                    { new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "soccer-ball", "Fútbol Varones" },
                    { new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "volleyball", "Vóley Mixto" },
                    { new Guid("c1d2e3f4-a5b6-4c8d-9e0f-1a2b3c4d5e6f"), "basketball", "Básquet" }
                });

            migrationBuilder.UpdateData(
                table: "Personas",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaRegistro",
                value: new DateTime(2026, 4, 15, 20, 24, 45, 431, DateTimeKind.Utc).AddTicks(1688));

            migrationBuilder.InsertData(
                table: "Venues",
                columns: new[] { "Id", "Address", "Capacity", "Name" },
                values: new object[,]
                {
                    { new Guid("090f4be4-ad65-40f1-8883-c84cb7c95dfd"), "Campus Principal", 5000, "Estadio Universitario UNAS" },
                    { new Guid("e6c9775f-ffc0-445c-ba05-2ef72a858725"), "Pabellón de Sistemas", 200, "Losa Deportiva FIIS" }
                });

            migrationBuilder.InsertData(
                table: "DisciplineRules",
                columns: new[] { "Id", "DisciplineId", "RuleKey", "RuleValue" },
                values: new object[,]
                {
                    { new Guid("296c10a5-e6c2-435b-98c8-e655e3ee64eb"), new Guid("c1d2e3f4-a5b6-4c8d-9e0f-1a2b3c4d5e6f"), "CANTIDAD_PERIODOS", "4" },
                    { new Guid("355d7a50-1a4d-4174-aa4f-52d576db4d22"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "TIENE_TARJETAS", "True" },
                    { new Guid("3d8a1227-a2c8-4b4e-af4f-51c1e321f5bd"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "PUNTOS_POR_VICTORIA", "3" },
                    { new Guid("4ee6419e-9e3b-4d4f-927b-e73ae83e8098"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "USA_SETS", "True" },
                    { new Guid("c001a1e8-5a41-487c-965a-0d6d3e3e10b0"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "PUNTOS_POR_VICTORIA", "2" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("296c10a5-e6c2-435b-98c8-e655e3ee64eb"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("355d7a50-1a4d-4174-aa4f-52d576db4d22"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("3d8a1227-a2c8-4b4e-af4f-51c1e321f5bd"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("4ee6419e-9e3b-4d4f-927b-e73ae83e8098"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("c001a1e8-5a41-487c-965a-0d6d3e3e10b0"));

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: new Guid("1a2b3c4d-5e6f-4a8b-9c0d-1e2f3a4b5c6d"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("090f4be4-ad65-40f1-8883-c84cb7c95dfd"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("e6c9775f-ffc0-445c-ba05-2ef72a858725"));

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"));

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"));

            migrationBuilder.DeleteData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: new Guid("c1d2e3f4-a5b6-4c8d-9e0f-1a2b3c4d5e6f"));

            migrationBuilder.UpdateData(
                table: "Personas",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaRegistro",
                value: new DateTime(2026, 4, 15, 17, 23, 35, 124, DateTimeKind.Utc).AddTicks(1853));
        }
    }
}
