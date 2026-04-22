using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siged.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueComplex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VenueComplexes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MapUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OpeningHoursNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueComplexes", x => x.Id);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "ComplexId",
                table: "Venues",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Venues_ComplexId",
                table: "Venues",
                column: "ComplexId");

            migrationBuilder.AddForeignKey(
                name: "FK_Venues_VenueComplexes_ComplexId",
                table: "Venues",
                column: "ComplexId",
                principalTable: "VenueComplexes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Venues_VenueComplexes_ComplexId",
                table: "Venues");

            migrationBuilder.DropTable(
                name: "VenueComplexes");

            migrationBuilder.DropIndex(
                name: "IX_Venues_ComplexId",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "ComplexId",
                table: "Venues");
        }
    }
}
