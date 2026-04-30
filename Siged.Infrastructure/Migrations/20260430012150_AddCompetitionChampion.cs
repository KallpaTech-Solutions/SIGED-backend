using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siged.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitionChampion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ChampionDecidedAtUtc",
                table: "Competitions",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ChampionTeamId",
                table: "Competitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_ChampionTeamId",
                table: "Competitions",
                column: "ChampionTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Competitions_Teams_ChampionTeamId",
                table: "Competitions",
                column: "ChampionTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Competitions_Teams_ChampionTeamId",
                table: "Competitions");

            migrationBuilder.DropIndex(
                name: "IX_Competitions_ChampionTeamId",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "ChampionDecidedAtUtc",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "ChampionTeamId",
                table: "Competitions");
        }
    }
}
