using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Siged.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitionHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Phases_Tournaments_TournamentId",
                table: "Phases");

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("112dbd92-afc8-4331-9f07-bf698f1a26ac"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("327a96ee-0cf9-4062-8cb2-5a07087ea4c2"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("940216d2-e659-4f95-8de1-5ed932d592e4"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("eb7166fc-4c96-4604-b646-1efed46ae473"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("f66641c6-5022-4c60-8f97-83a488205687"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("901cd875-3dce-405e-9905-d93c092aee5e"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("e7a7af40-e061-4b29-a85d-cdb44bc63fc4"));

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Tournaments");

            migrationBuilder.RenameColumn(
                name: "TournamentId",
                table: "Phases",
                newName: "CompetitionId");

            migrationBuilder.RenameIndex(
                name: "IX_Phases_TournamentId",
                table: "Phases",
                newName: "IX_Phases_CompetitionId");

            migrationBuilder.CreateTable(
                name: "Competitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisciplineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    CategoryName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Competitions_Disciplines_DisciplineId",
                        column: x => x.DisciplineId,
                        principalTable: "Disciplines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Competitions_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DisciplineRules",
                columns: new[] { "Id", "DisciplineId", "RuleKey", "RuleValue" },
                values: new object[,]
                {
                    { new Guid("5482c05a-edd9-4693-98e6-9642a50c949f"), new Guid("c1d2e3f4-a5b6-4c8d-9e0f-1a2b3c4d5e6f"), "CANTIDAD_PERIODOS", "4" },
                    { new Guid("9ecc0221-1912-4212-9642-2d55bbebebc1"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "PUNTOS_POR_VICTORIA", "3" },
                    { new Guid("b62ca084-cc6d-4afb-b280-1f7f2ad4b764"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "TIENE_TARJETAS", "True" },
                    { new Guid("cc5677f0-9cd4-4c16-b71b-8c25c5dca3df"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "USA_SETS", "True" },
                    { new Guid("e898632f-6f62-4ada-9522-4b499a70b0d0"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "PUNTOS_POR_VICTORIA", "2" }
                });

            migrationBuilder.UpdateData(
                table: "Personas",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaRegistro",
                value: new DateTime(2026, 4, 15, 20, 55, 42, 335, DateTimeKind.Utc).AddTicks(6193));

            migrationBuilder.InsertData(
                table: "Venues",
                columns: new[] { "Id", "Address", "Capacity", "Name" },
                values: new object[,]
                {
                    { new Guid("6a9278aa-8731-4a13-9b0b-a0970f28ab8c"), "Pabellón de Sistemas", 200, "Losa Deportiva FIIS" },
                    { new Guid("6f86ac07-936a-45a1-8c24-241b1ca95d44"), "Campus Principal", 5000, "Estadio Universitario UNAS" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_DisciplineId",
                table: "Matches",
                column: "DisciplineId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_PhaseId",
                table: "Matches",
                column: "PhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_DisciplineId",
                table: "Competitions",
                column: "DisciplineId");

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_TournamentId",
                table: "Competitions",
                column: "TournamentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Disciplines_DisciplineId",
                table: "Matches",
                column: "DisciplineId",
                principalTable: "Disciplines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Phases_PhaseId",
                table: "Matches",
                column: "PhaseId",
                principalTable: "Phases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Phases_Competitions_CompetitionId",
                table: "Phases",
                column: "CompetitionId",
                principalTable: "Competitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Disciplines_DisciplineId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Phases_PhaseId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Phases_Competitions_CompetitionId",
                table: "Phases");

            migrationBuilder.DropTable(
                name: "Competitions");

            migrationBuilder.DropIndex(
                name: "IX_Matches_DisciplineId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_PhaseId",
                table: "Matches");

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("5482c05a-edd9-4693-98e6-9642a50c949f"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("9ecc0221-1912-4212-9642-2d55bbebebc1"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("b62ca084-cc6d-4afb-b280-1f7f2ad4b764"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("cc5677f0-9cd4-4c16-b71b-8c25c5dca3df"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("e898632f-6f62-4ada-9522-4b499a70b0d0"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("6a9278aa-8731-4a13-9b0b-a0970f28ab8c"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("6f86ac07-936a-45a1-8c24-241b1ca95d44"));

            migrationBuilder.RenameColumn(
                name: "CompetitionId",
                table: "Phases",
                newName: "TournamentId");

            migrationBuilder.RenameIndex(
                name: "IX_Phases_CompetitionId",
                table: "Phases",
                newName: "IX_Phases_TournamentId");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Tournaments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "Tournaments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "DisciplineRules",
                columns: new[] { "Id", "DisciplineId", "RuleKey", "RuleValue" },
                values: new object[,]
                {
                    { new Guid("112dbd92-afc8-4331-9f07-bf698f1a26ac"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "USA_SETS", "True" },
                    { new Guid("327a96ee-0cf9-4062-8cb2-5a07087ea4c2"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "PUNTOS_POR_VICTORIA", "3" },
                    { new Guid("940216d2-e659-4f95-8de1-5ed932d592e4"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "TIENE_TARJETAS", "True" },
                    { new Guid("eb7166fc-4c96-4604-b646-1efed46ae473"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "PUNTOS_POR_VICTORIA", "2" },
                    { new Guid("f66641c6-5022-4c60-8f97-83a488205687"), new Guid("c1d2e3f4-a5b6-4c8d-9e0f-1a2b3c4d5e6f"), "CANTIDAD_PERIODOS", "4" }
                });

            migrationBuilder.UpdateData(
                table: "Personas",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaRegistro",
                value: new DateTime(2026, 4, 15, 20, 30, 43, 869, DateTimeKind.Utc).AddTicks(1112));

            migrationBuilder.InsertData(
                table: "Venues",
                columns: new[] { "Id", "Address", "Capacity", "Name" },
                values: new object[,]
                {
                    { new Guid("901cd875-3dce-405e-9905-d93c092aee5e"), "Pabellón de Sistemas", 200, "Losa Deportiva FIIS" },
                    { new Guid("e7a7af40-e061-4b29-a85d-cdb44bc63fc4"), "Campus Principal", 5000, "Estadio Universitario UNAS" }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Phases_Tournaments_TournamentId",
                table: "Phases",
                column: "TournamentId",
                principalTable: "Tournaments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
