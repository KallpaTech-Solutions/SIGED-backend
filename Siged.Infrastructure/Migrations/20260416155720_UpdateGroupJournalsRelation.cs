using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Siged.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGroupJournalsRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("0d235b95-a5e3-4190-9c07-bdd572c84bc9"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("98005249-103d-4205-a3f1-5774fd673b69"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("a8cc8df6-7351-4e6d-a61e-39d48b014146"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("b989f6c7-465c-4eab-8a62-eb64c1580807"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("efa16348-fd60-41f9-b3bd-98b22383a617"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("c03fc16d-e439-4418-85b6-0da00ad29869"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("e91704fa-81f3-4055-b253-f2253e75c4d1"));

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Groups",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.InsertData(
                table: "DisciplineRules",
                columns: new[] { "Id", "DisciplineId", "RuleKey", "RuleValue" },
                values: new object[,]
                {
                    { new Guid("80ca00c3-9049-4f9f-ae21-0e3b163c3e5a"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "PUNTOS_POR_VICTORIA", "2" },
                    { new Guid("856c97fe-5a58-4fd0-ba30-348637920d15"), new Guid("c1d2e3f4-a5b6-4c8d-9e0f-1a2b3c4d5e6f"), "CANTIDAD_PERIODOS", "4" },
                    { new Guid("95f0062f-3c29-40fc-9393-f024e3e5aac0"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "PUNTOS_POR_VICTORIA", "3" },
                    { new Guid("9f697f66-8c97-4ceb-968c-46ff30600ea6"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "TIENE_TARJETAS", "True" },
                    { new Guid("a4c48254-f27f-4029-aa86-c90f655a77d5"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "USA_SETS", "True" }
                });

            migrationBuilder.UpdateData(
                table: "Personas",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaRegistro",
                value: new DateTime(2026, 4, 16, 15, 57, 20, 104, DateTimeKind.Utc).AddTicks(8548));

            migrationBuilder.InsertData(
                table: "Venues",
                columns: new[] { "Id", "Address", "Capacity", "Name" },
                values: new object[,]
                {
                    { new Guid("53e29bc9-b236-4a13-b75e-d3a2e1d3b2eb"), "Campus Principal", 5000, "Estadio Universitario UNAS" },
                    { new Guid("79761e43-9a42-4126-9bc3-ca2c53665f33"), "Pabellón de Sistemas", 200, "Losa Deportiva FIIS" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("80ca00c3-9049-4f9f-ae21-0e3b163c3e5a"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("856c97fe-5a58-4fd0-ba30-348637920d15"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("95f0062f-3c29-40fc-9393-f024e3e5aac0"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("9f697f66-8c97-4ceb-968c-46ff30600ea6"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("a4c48254-f27f-4029-aa86-c90f655a77d5"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("53e29bc9-b236-4a13-b75e-d3a2e1d3b2eb"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("79761e43-9a42-4126-9bc3-ca2c53665f33"));

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Groups",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.InsertData(
                table: "DisciplineRules",
                columns: new[] { "Id", "DisciplineId", "RuleKey", "RuleValue" },
                values: new object[,]
                {
                    { new Guid("0d235b95-a5e3-4190-9c07-bdd572c84bc9"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "TIENE_TARJETAS", "True" },
                    { new Guid("98005249-103d-4205-a3f1-5774fd673b69"), new Guid("c1d2e3f4-a5b6-4c8d-9e0f-1a2b3c4d5e6f"), "CANTIDAD_PERIODOS", "4" },
                    { new Guid("a8cc8df6-7351-4e6d-a61e-39d48b014146"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "PUNTOS_POR_VICTORIA", "3" },
                    { new Guid("b989f6c7-465c-4eab-8a62-eb64c1580807"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "USA_SETS", "True" },
                    { new Guid("efa16348-fd60-41f9-b3bd-98b22383a617"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "PUNTOS_POR_VICTORIA", "2" }
                });

            migrationBuilder.UpdateData(
                table: "Personas",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaRegistro",
                value: new DateTime(2026, 4, 16, 13, 44, 3, 21, DateTimeKind.Utc).AddTicks(4686));

            migrationBuilder.InsertData(
                table: "Venues",
                columns: new[] { "Id", "Address", "Capacity", "Name" },
                values: new object[,]
                {
                    { new Guid("c03fc16d-e439-4418-85b6-0da00ad29869"), "Campus Principal", 5000, "Estadio Universitario UNAS" },
                    { new Guid("e91704fa-81f3-4055-b253-f2253e75c4d1"), "Pabellón de Sistemas", 200, "Losa Deportiva FIIS" }
                });
        }
    }
}
