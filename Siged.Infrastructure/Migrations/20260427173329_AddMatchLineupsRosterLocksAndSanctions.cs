using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Siged.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchLineupsRosterLocksAndSanctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CompetitionTeams_CompetitionId",
                table: "CompetitionTeams");

            migrationBuilder.AddColumn<bool>(
                name: "RosterLocked",
                table: "CompetitionTeams",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RosterLockedAt",
                table: "CompetitionTeams",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RosterLockedByUsuarioId",
                table: "CompetitionTeams",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RosterUnlockedAt",
                table: "CompetitionTeams",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MatchLineups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedByUsuarioId = table.Column<int>(type: "integer", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LockedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Observation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchLineups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchLineups_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchLineups_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerSanctions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    MatchesCount = table.Column<int>(type: "integer", nullable: true),
                    PhasesCount = table.Column<int>(type: "integer", nullable: true),
                    StartsAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUsuarioId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LiftedByUsuarioId = table.Column<int>(type: "integer", nullable: true),
                    LiftedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Observation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSanctions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerSanctions_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlayerSanctions_MatchEvents_MatchEventId",
                        column: x => x.MatchEventId,
                        principalTable: "MatchEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlayerSanctions_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlayerSanctions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerSanctions_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MatchLineupPlayers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchLineupId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    ShirtNumber = table.Column<int>(type: "integer", nullable: true),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    IsCaptain = table.Column<bool>(type: "boolean", nullable: false),
                    IsGoalkeeper = table.Column<bool>(type: "boolean", nullable: false),
                    Observation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchLineupPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchLineupPlayers_MatchLineups_MatchLineupId",
                        column: x => x.MatchLineupId,
                        principalTable: "MatchLineups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchLineupPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "IdPermiso", "Categoria", "Descripcion" },
                values: new object[,]
                {
                    { "tourn.lineup.manage", "TORNEOS", "Gestionar planillas y listas oficiales de equipos" },
                    { "tourn.match.report.download", "TORNEOS", "Descargar actas de partido" },
                    { "tourn.player.sanction.manage", "TORNEOS", "Gestionar sanciones e inhabilitaciones de jugadores" }
                });

            migrationBuilder.InsertData(
                table: "RolPermisos",
                columns: new[] { "PermisosIdPermiso", "RolesId" },
                values: new object[,]
                {
                    { "tourn.lineup.manage", 1 },
                    { "tourn.lineup.manage", 3 },
                    { "tourn.match.report.download", 1 },
                    { "tourn.match.report.download", 2 },
                    { "tourn.match.report.download", 3 },
                    { "tourn.player.sanction.manage", 1 },
                    { "tourn.player.sanction.manage", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionTeams_CompetitionId_TeamId",
                table: "CompetitionTeams",
                columns: new[] { "CompetitionId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchLineupPlayers_MatchLineupId_PlayerId",
                table: "MatchLineupPlayers",
                columns: new[] { "MatchLineupId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchLineupPlayers_PlayerId",
                table: "MatchLineupPlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchLineups_MatchId_TeamId",
                table: "MatchLineups",
                columns: new[] { "MatchId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchLineups_TeamId",
                table: "MatchLineups",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSanctions_CompetitionId",
                table: "PlayerSanctions",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSanctions_MatchEventId",
                table: "PlayerSanctions",
                column: "MatchEventId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSanctions_MatchId",
                table: "PlayerSanctions",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSanctions_PlayerId_CompetitionId_IsActive",
                table: "PlayerSanctions",
                columns: new[] { "PlayerId", "CompetitionId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSanctions_TeamId",
                table: "PlayerSanctions",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchLineupPlayers");

            migrationBuilder.DropTable(
                name: "PlayerSanctions");

            migrationBuilder.DropTable(
                name: "MatchLineups");

            migrationBuilder.DropIndex(
                name: "IX_CompetitionTeams_CompetitionId_TeamId",
                table: "CompetitionTeams");

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.lineup.manage", 1 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.lineup.manage", 3 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.match.report.download", 1 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.match.report.download", 2 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.match.report.download", 3 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.player.sanction.manage", 1 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.player.sanction.manage", 3 });

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "IdPermiso",
                keyValue: "tourn.lineup.manage");

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "IdPermiso",
                keyValue: "tourn.match.report.download");

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "IdPermiso",
                keyValue: "tourn.player.sanction.manage");

            migrationBuilder.DropColumn(
                name: "RosterLocked",
                table: "CompetitionTeams");

            migrationBuilder.DropColumn(
                name: "RosterLockedAt",
                table: "CompetitionTeams");

            migrationBuilder.DropColumn(
                name: "RosterLockedByUsuarioId",
                table: "CompetitionTeams");

            migrationBuilder.DropColumn(
                name: "RosterUnlockedAt",
                table: "CompetitionTeams");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionTeams_CompetitionId",
                table: "CompetitionTeams",
                column: "CompetitionId");
        }
    }
}
