using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siged.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamGestoresAndCreatedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUsuarioId",
                table: "Teams",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TeamGestores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<byte>(type: "smallint", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AssignedByUsuarioId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamGestores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamGestores_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamGestores_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CreatedByUsuarioId",
                table: "Teams",
                column: "CreatedByUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamGestores_TeamId_UsuarioId",
                table: "TeamGestores",
                columns: new[] { "TeamId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamGestores_UsuarioId",
                table: "TeamGestores",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Usuarios_CreatedByUsuarioId",
                table: "Teams",
                column: "CreatedByUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Usuarios_CreatedByUsuarioId",
                table: "Teams");

            migrationBuilder.DropTable(
                name: "TeamGestores");

            migrationBuilder.DropIndex(
                name: "IX_Teams_CreatedByUsuarioId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "CreatedByUsuarioId",
                table: "Teams");
        }
    }
}
