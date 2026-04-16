using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Siged.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExtendedFieldsToMatchEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "MatchEvents");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "MatchEvents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TeamId",
                table: "MatchEvents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "Note",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "MatchEvents");

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "MatchEvents",
                type: "text",
                nullable: false,
                defaultValue: "");

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
    }
}
