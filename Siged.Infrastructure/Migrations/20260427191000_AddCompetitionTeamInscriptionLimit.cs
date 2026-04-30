using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Siged.Infrastructure.Persistence;

#nullable disable

namespace Siged.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260427191000_AddCompetitionTeamInscriptionLimit")]
    public partial class AddCompetitionTeamInscriptionLimit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxTeamsPerOrganization",
                table: "Competitions",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxTeamsPerOrganization",
                table: "Competitions");
        }
    }
}
