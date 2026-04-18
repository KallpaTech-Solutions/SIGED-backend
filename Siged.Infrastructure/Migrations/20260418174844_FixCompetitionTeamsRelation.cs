using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Siged.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCompetitionTeamsRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inscription");

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("0e0892c3-4a4a-469f-83a8-fdfbfe387e6b"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("2b0c09a8-1c21-4f65-a6c8-cbc6f8346755"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("7a2e84e7-eede-49ba-a79e-aedb98186632"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("9eb5f587-2d74-4dce-aa93-d1185011205c"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("ebfe8e96-80a0-4454-8f2a-e1c7178ff979"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("4b6d39e3-93ef-4429-a860-5adcb5d22f5e"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("81a9a571-16cc-45ee-a821-7c5b1621d35f"));

            migrationBuilder.InsertData(
                table: "DisciplineRules",
                columns: new[] { "Id", "DisciplineId", "RuleKey", "RuleValue" },
                values: new object[,]
                {
                    { new Guid("0550981f-9625-405d-be5e-891d7ae6dc71"), new Guid("c1d2e3f4-a5b6-4c8d-9e0f-1a2b3c4d5e6f"), "CANTIDAD_PERIODOS", "4" },
                    { new Guid("1c08265a-b57a-4bea-8fe5-cacf6eb5893a"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "TIENE_TARJETAS", "True" },
                    { new Guid("596d4a0e-a556-4613-9274-72080cab0a53"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "PUNTOS_POR_VICTORIA", "2" },
                    { new Guid("ae714f95-c382-4da6-aa1f-a8585433b22d"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "USA_SETS", "True" },
                    { new Guid("d6c7b759-104d-419f-8560-eed7ca441d16"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "PUNTOS_POR_VICTORIA", "3" }
                });

            migrationBuilder.UpdateData(
                table: "Personas",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaRegistro",
                value: new DateTime(2026, 4, 18, 17, 48, 43, 720, DateTimeKind.Utc).AddTicks(4931));

            migrationBuilder.InsertData(
                table: "Venues",
                columns: new[] { "Id", "Address", "Capacity", "Name" },
                values: new object[,]
                {
                    { new Guid("95d0b0ab-5478-4659-b959-4cb23835e898"), "Campus Principal", 5000, "Estadio Universitario UNAS" },
                    { new Guid("dd3cb1e3-45fd-405f-9429-312df500dd10"), "Pabellón de Sistemas", 200, "Losa Deportiva FIIS" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("0550981f-9625-405d-be5e-891d7ae6dc71"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("1c08265a-b57a-4bea-8fe5-cacf6eb5893a"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("596d4a0e-a556-4613-9274-72080cab0a53"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("ae714f95-c382-4da6-aa1f-a8585433b22d"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("d6c7b759-104d-419f-8560-eed7ca441d16"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("95d0b0ab-5478-4659-b959-4cb23835e898"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("dd3cb1e3-45fd-405f-9429-312df500dd10"));

            migrationBuilder.CreateTable(
                name: "Inscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaInscripcion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inscription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inscription_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Inscription_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DisciplineRules",
                columns: new[] { "Id", "DisciplineId", "RuleKey", "RuleValue" },
                values: new object[,]
                {
                    { new Guid("0e0892c3-4a4a-469f-83a8-fdfbfe387e6b"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "PUNTOS_POR_VICTORIA", "3" },
                    { new Guid("2b0c09a8-1c21-4f65-a6c8-cbc6f8346755"), new Guid("c1d2e3f4-a5b6-4c8d-9e0f-1a2b3c4d5e6f"), "CANTIDAD_PERIODOS", "4" },
                    { new Guid("7a2e84e7-eede-49ba-a79e-aedb98186632"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "USA_SETS", "True" },
                    { new Guid("9eb5f587-2d74-4dce-aa93-d1185011205c"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "TIENE_TARJETAS", "True" },
                    { new Guid("ebfe8e96-80a0-4454-8f2a-e1c7178ff979"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "PUNTOS_POR_VICTORIA", "2" }
                });

            migrationBuilder.UpdateData(
                table: "Personas",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaRegistro",
                value: new DateTime(2026, 4, 18, 17, 33, 26, 839, DateTimeKind.Utc).AddTicks(1940));

            migrationBuilder.InsertData(
                table: "Venues",
                columns: new[] { "Id", "Address", "Capacity", "Name" },
                values: new object[,]
                {
                    { new Guid("4b6d39e3-93ef-4429-a860-5adcb5d22f5e"), "Campus Principal", 5000, "Estadio Universitario UNAS" },
                    { new Guid("81a9a571-16cc-45ee-a821-7c5b1621d35f"), "Pabellón de Sistemas", 200, "Losa Deportiva FIIS" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inscription_CompetitionId",
                table: "Inscription",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscription_TeamId",
                table: "Inscription",
                column: "TeamId");
        }
    }
}
