using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Siged.Infrastructure.Persistence;

#nullable disable

namespace Siged.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260427223000_AddOperationalTournamentRoles")]
    public partial class AddOperationalTournamentRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "Roles" ("Id", "Nombre", "Descripcion", "Nivel")
                VALUES
                    (5, 'Delegado_Escuela', 'Delegado que registra equipos y jugadores de su escuela', 35),
                    (6, 'Gestor_Torneo', 'Configura competencias, fixture y listas oficiales', 60),
                    (7, 'Mesa_Control', 'Registra eventos, actas, sanciones y habilitación deportiva', 45),
                    (8, 'Mesa_Transmision', 'Opera widgets, marcadores y transmisión en vivo', 35),
                    (9, 'Encargado_Disciplina', 'Responsable operativo de una disciplina o competencia', 55)
                ON CONFLICT ("Id") DO UPDATE SET
                    "Nombre" = EXCLUDED."Nombre",
                    "Descripcion" = EXCLUDED."Descripcion",
                    "Nivel" = EXCLUDED."Nivel";

                INSERT INTO "RolPermisos" ("PermisosIdPermiso", "RolesId")
                VALUES
                    ('core.org.view', 5),
                    ('comp.tourn.view', 5),
                    ('tourn.view', 5),
                    ('tourn.team.manage', 5),

                    ('core.org.view', 6),
                    ('comp.tourn.view', 6),
                    ('comp.tourn.manage', 6),
                    ('tourn.view', 6),
                    ('tourn.config', 6),
                    ('tourn.team.manage', 6),
                    ('tourn.fixture', 6),
                    ('tourn.lineup.manage', 6),
                    ('tourn.match.report.download', 6),

                    ('core.org.view', 7),
                    ('comp.tourn.view', 7),
                    ('tourn.view', 7),
                    ('tourn.match.control', 7),
                    ('tourn.match.report.download', 7),
                    ('tourn.player.sanction.manage', 7),

                    ('core.org.view', 8),
                    ('comp.tourn.view', 8),
                    ('tourn.view', 8),
                    ('tourn.match.widgets', 8),

                    ('core.org.view', 9),
                    ('comp.tourn.view', 9),
                    ('comp.tourn.manage', 9),
                    ('tourn.view', 9),
                    ('tourn.config', 9),
                    ('tourn.team.manage', 9),
                    ('tourn.fixture', 9),
                    ('tourn.match.control', 9),
                    ('tourn.lineup.manage', 9),
                    ('tourn.match.report.download', 9),
                    ('tourn.player.sanction.manage', 9)
                ON CONFLICT ("PermisosIdPermiso", "RolesId") DO NOTHING;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "RolPermisos" WHERE "RolesId" IN (5, 6, 7, 8, 9);
                DELETE FROM "Roles" WHERE "Id" IN (5, 6, 7, 8, 9);
                """);
        }
    }
}
