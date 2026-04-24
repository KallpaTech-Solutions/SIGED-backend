using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siged.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlayerTeamDniUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_TeamId",
                table: "Players");

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId_Dni",
                table: "Players",
                columns: new[] { "TeamId", "Dni" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_TeamId_Dni",
                table: "Players");

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId",
                table: "Players",
                column: "TeamId");
        }
    }
}
