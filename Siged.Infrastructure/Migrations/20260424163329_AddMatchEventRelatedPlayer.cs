using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Siged.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchEventRelatedPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RelatedPlayerId",
                table: "MatchEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvents_RelatedPlayerId",
                table: "MatchEvents",
                column: "RelatedPlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_MatchEvents_Players_RelatedPlayerId",
                table: "MatchEvents",
                column: "RelatedPlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchEvents_Players_RelatedPlayerId",
                table: "MatchEvents");

            migrationBuilder.DropIndex(
                name: "IX_MatchEvents_RelatedPlayerId",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "RelatedPlayerId",
                table: "MatchEvents");
        }
    }
}
