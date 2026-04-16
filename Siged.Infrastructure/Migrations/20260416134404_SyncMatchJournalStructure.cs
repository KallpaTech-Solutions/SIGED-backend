using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Siged.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncMatchJournalStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("5c3074df-0260-4a66-befa-560f514ac2d4"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("66f3fc36-17da-4c9f-be65-8a870908e40f"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("7d99ff7d-3c0a-41a1-a456-aafbbf9bdb59"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("8e1520b5-766b-481a-a012-ef128291c397"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("94bff6f9-8940-4afe-b768-df559fe36bdb"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("8aa04be0-cb65-4c42-bfba-b4f4426b0d67"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("f1f3dd60-3db6-4c6b-b884-6bb22c4c1a3d"));

            migrationBuilder.RenameColumn(
                name: "Number",
                table: "Journals",
                newName: "Sequence");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Matches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "JournalId",
                table: "Matches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Journals",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "Journals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Journals",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.CreateIndex(
                name: "IX_Matches_JournalId",
                table: "Matches",
                column: "JournalId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_VenueId",
                table: "Matches",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_Journals_GroupId",
                table: "Journals",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Journals_Groups_GroupId",
                table: "Journals",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Journals_JournalId",
                table: "Matches",
                column: "JournalId",
                principalTable: "Journals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Venues_VenueId",
                table: "Matches",
                column: "VenueId",
                principalTable: "Venues",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Journals_Groups_GroupId",
                table: "Journals");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Journals_JournalId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Venues_VenueId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_JournalId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_VenueId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Journals_GroupId",
                table: "Journals");

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

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "JournalId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Journals");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Journals");

            migrationBuilder.RenameColumn(
                name: "Sequence",
                table: "Journals",
                newName: "Number");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Journals",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.InsertData(
                table: "DisciplineRules",
                columns: new[] { "Id", "DisciplineId", "RuleKey", "RuleValue" },
                values: new object[,]
                {
                    { new Guid("5c3074df-0260-4a66-befa-560f514ac2d4"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "PUNTOS_POR_VICTORIA", "2" },
                    { new Guid("66f3fc36-17da-4c9f-be65-8a870908e40f"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "USA_SETS", "True" },
                    { new Guid("7d99ff7d-3c0a-41a1-a456-aafbbf9bdb59"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "TIENE_TARJETAS", "True" },
                    { new Guid("8e1520b5-766b-481a-a012-ef128291c397"), new Guid("c1d2e3f4-a5b6-4c8d-9e0f-1a2b3c4d5e6f"), "CANTIDAD_PERIODOS", "4" },
                    { new Guid("94bff6f9-8940-4afe-b768-df559fe36bdb"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "PUNTOS_POR_VICTORIA", "3" }
                });

            migrationBuilder.UpdateData(
                table: "Personas",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaRegistro",
                value: new DateTime(2026, 4, 15, 23, 4, 9, 523, DateTimeKind.Utc).AddTicks(4861));

            migrationBuilder.InsertData(
                table: "Venues",
                columns: new[] { "Id", "Address", "Capacity", "Name" },
                values: new object[,]
                {
                    { new Guid("8aa04be0-cb65-4c42-bfba-b4f4426b0d67"), "Pabellón de Sistemas", 200, "Losa Deportiva FIIS" },
                    { new Guid("f1f3dd60-3db6-4c6b-b884-6bb22c4c1a3d"), "Campus Principal", 5000, "Estadio Universitario UNAS" }
                });
        }
    }
}
