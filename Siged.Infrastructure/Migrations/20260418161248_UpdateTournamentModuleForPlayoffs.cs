using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Siged.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTournamentModuleForPlayoffs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("160fc774-6f3b-42e6-a5df-3591042b05c5"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("27d5cba2-2e57-48ad-8365-3336a8f62338"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("aaf07540-f29a-48bb-8da6-7c64f6146b8d"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("ac3e3888-5d4d-4917-b280-f61a8d23a6e1"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("c478bc10-1385-4eb4-a035-26b1f4ba018a"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("0442d2ac-bf30-4f9b-9380-d0da1350c8b2"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("82db8686-bf97-45cd-89a8-ab8fa7079040"));

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "Teams",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "IsDoubleLeg",
                table: "Phases",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionPadreId",
                table: "Organizaciones",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "VisitorTeamId",
                table: "Matches",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "LocalTeamId",
                table: "Matches",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "LocalPenaltyScore",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Matches",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VisitorPenaltyScore",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WinnerId",
                table: "Matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScoringType",
                table: "Disciplines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CompetitionRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleKey = table.Column<string>(type: "text", nullable: false),
                    RuleValue = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetitionRules_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompetitionTeams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaInscripcion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Puntos = table.Column<int>(type: "integer", nullable: false),
                    PartidosJugados = table.Column<int>(type: "integer", nullable: false),
                    EstaDescalificado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetitionTeams_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompetitionTeams_Teams_TeamId",
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
                    { new Guid("0dced771-6df8-4a70-9456-0b38aad9ed33"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "TIENE_TARJETAS", "True" },
                    { new Guid("428e6faf-ae8d-4e67-a82e-b226227bcb23"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "PUNTOS_POR_VICTORIA", "3" },
                    { new Guid("50d800a5-fd68-4100-bfa3-9c2d814cb0e7"), new Guid("c1d2e3f4-a5b6-4c8d-9e0f-1a2b3c4d5e6f"), "CANTIDAD_PERIODOS", "4" },
                    { new Guid("6181c515-b50a-48ec-9004-af8b219e3a80"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "PUNTOS_POR_VICTORIA", "2" },
                    { new Guid("b66d0d81-16f8-4858-ada7-69663c212339"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "USA_SETS", "True" }
                });

            migrationBuilder.UpdateData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: new Guid("1a2b3c4d-5e6f-4a8b-9c0d-1e2f3a4b5c6d"),
                column: "ScoringType",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"),
                column: "ScoringType",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"),
                column: "ScoringType",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Disciplines",
                keyColumn: "Id",
                keyValue: new Guid("c1d2e3f4-a5b6-4c8d-9e0f-1a2b3c4d5e6f"),
                column: "ScoringType",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Organizaciones",
                keyColumn: "Id",
                keyValue: 1,
                column: "OrganizacionPadreId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Organizaciones",
                keyColumn: "Id",
                keyValue: 2,
                column: "OrganizacionPadreId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Organizaciones",
                keyColumn: "Id",
                keyValue: 3,
                column: "OrganizacionPadreId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Organizaciones",
                keyColumn: "Id",
                keyValue: 4,
                column: "OrganizacionPadreId",
                value: null);

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "IdPermiso", "Categoria", "Descripcion" },
                values: new object[,]
                {
                    { "tourn.config", "TORNEOS", "Gestionar fases y sorteo de grupos" },
                    { "tourn.fixture", "TORNEOS", "Generar fixture y programar encuentros" },
                    { "tourn.manage", "TORNEOS", "Crear y editar torneos y disciplinas" },
                    { "tourn.match.control", "TORNEOS", "Control de mesa: registro de eventos y actas" },
                    { "tourn.team.manage", "TORNEOS", "Administrar equipos y enrolamiento de jugadores" },
                    { "tourn.view", "TORNEOS", "Ver torneos, tablas y cronogramas" }
                });

            migrationBuilder.UpdateData(
                table: "Personas",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaRegistro",
                value: new DateTime(2026, 4, 18, 16, 12, 47, 544, DateTimeKind.Utc).AddTicks(2240));

            migrationBuilder.InsertData(
                table: "Venues",
                columns: new[] { "Id", "Address", "Capacity", "Name" },
                values: new object[,]
                {
                    { new Guid("9b38126e-0f1c-42af-8b7d-99c810f5262a"), "Pabellón de Sistemas", 200, "Losa Deportiva FIIS" },
                    { new Guid("bb8f42ce-4740-4b4f-b0b5-5e38cafb1b29"), "Campus Principal", 5000, "Estadio Universitario UNAS" }
                });

            migrationBuilder.InsertData(
                table: "RolPermisos",
                columns: new[] { "PermisosIdPermiso", "RolesId" },
                values: new object[,]
                {
                    { "tourn.config", 1 },
                    { "tourn.config", 2 },
                    { "tourn.config", 3 },
                    { "tourn.fixture", 1 },
                    { "tourn.fixture", 3 },
                    { "tourn.manage", 1 },
                    { "tourn.manage", 2 },
                    { "tourn.match.control", 1 },
                    { "tourn.match.control", 3 },
                    { "tourn.team.manage", 1 },
                    { "tourn.team.manage", 3 },
                    { "tourn.view", 1 },
                    { "tourn.view", 2 },
                    { "tourn.view", 3 },
                    { "tourn.view", 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_OrganizacionId",
                table: "Teams",
                column: "OrganizacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizaciones_OrganizacionPadreId",
                table: "Organizaciones",
                column: "OrganizacionPadreId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionRules_CompetitionId",
                table: "CompetitionRules",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionTeams_CompetitionId",
                table: "CompetitionTeams",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionTeams_TeamId",
                table: "CompetitionTeams",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizaciones_Organizaciones_OrganizacionPadreId",
                table: "Organizaciones",
                column: "OrganizacionPadreId",
                principalTable: "Organizaciones",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Organizaciones_OrganizacionId",
                table: "Teams",
                column: "OrganizacionId",
                principalTable: "Organizaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizaciones_Organizaciones_OrganizacionPadreId",
                table: "Organizaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Organizaciones_OrganizacionId",
                table: "Teams");

            migrationBuilder.DropTable(
                name: "CompetitionRules");

            migrationBuilder.DropTable(
                name: "CompetitionTeams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_OrganizacionId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Organizaciones_OrganizacionPadreId",
                table: "Organizaciones");

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("0dced771-6df8-4a70-9456-0b38aad9ed33"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("428e6faf-ae8d-4e67-a82e-b226227bcb23"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("50d800a5-fd68-4100-bfa3-9c2d814cb0e7"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("6181c515-b50a-48ec-9004-af8b219e3a80"));

            migrationBuilder.DeleteData(
                table: "DisciplineRules",
                keyColumn: "Id",
                keyValue: new Guid("b66d0d81-16f8-4858-ada7-69663c212339"));

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.config", 1 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.config", 2 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.config", 3 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.fixture", 1 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.fixture", 3 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.manage", 1 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.manage", 2 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.match.control", 1 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.match.control", 3 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.team.manage", 1 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.team.manage", 3 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.view", 1 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.view", 2 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.view", 3 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisosIdPermiso", "RolesId" },
                keyValues: new object[] { "tourn.view", 4 });

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("9b38126e-0f1c-42af-8b7d-99c810f5262a"));

            migrationBuilder.DeleteData(
                table: "Venues",
                keyColumn: "Id",
                keyValue: new Guid("bb8f42ce-4740-4b4f-b0b5-5e38cafb1b29"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "IdPermiso",
                keyValue: "tourn.config");

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "IdPermiso",
                keyValue: "tourn.fixture");

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "IdPermiso",
                keyValue: "tourn.manage");

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "IdPermiso",
                keyValue: "tourn.match.control");

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "IdPermiso",
                keyValue: "tourn.team.manage");

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "IdPermiso",
                keyValue: "tourn.view");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "IsDoubleLeg",
                table: "Phases");

            migrationBuilder.DropColumn(
                name: "OrganizacionPadreId",
                table: "Organizaciones");

            migrationBuilder.DropColumn(
                name: "LocalPenaltyScore",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "VisitorPenaltyScore",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "WinnerId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ScoringType",
                table: "Disciplines");

            migrationBuilder.AlterColumn<Guid>(
                name: "VisitorTeamId",
                table: "Matches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "LocalTeamId",
                table: "Matches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "DisciplineRules",
                columns: new[] { "Id", "DisciplineId", "RuleKey", "RuleValue" },
                values: new object[,]
                {
                    { new Guid("160fc774-6f3b-42e6-a5df-3591042b05c5"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "USA_SETS", "True" },
                    { new Guid("27d5cba2-2e57-48ad-8365-3336a8f62338"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "TIENE_TARJETAS", "True" },
                    { new Guid("aaf07540-f29a-48bb-8da6-7c64f6146b8d"), new Guid("b1c2d3e4-f5a6-4b8c-9d0e-1f2a3b4c5d6e"), "PUNTOS_POR_VICTORIA", "2" },
                    { new Guid("ac3e3888-5d4d-4917-b280-f61a8d23a6e1"), new Guid("7f6a5b4c-3d2e-4f0a-9b8c-7d6e5f4a3b2c"), "PUNTOS_POR_VICTORIA", "3" },
                    { new Guid("c478bc10-1385-4eb4-a035-26b1f4ba018a"), new Guid("c1d2e3f4-a5b6-4c8d-9e0f-1a2b3c4d5e6f"), "CANTIDAD_PERIODOS", "4" }
                });

            migrationBuilder.UpdateData(
                table: "Personas",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaRegistro",
                value: new DateTime(2026, 4, 16, 17, 43, 50, 589, DateTimeKind.Utc).AddTicks(8425));

            migrationBuilder.InsertData(
                table: "Venues",
                columns: new[] { "Id", "Address", "Capacity", "Name" },
                values: new object[,]
                {
                    { new Guid("0442d2ac-bf30-4f9b-9380-d0da1350c8b2"), "Campus Principal", 5000, "Estadio Universitario UNAS" },
                    { new Guid("82db8686-bf97-45cd-89a8-ab8fa7079040"), "Pabellón de Sistemas", 200, "Losa Deportiva FIIS" }
                });
        }
    }
}
