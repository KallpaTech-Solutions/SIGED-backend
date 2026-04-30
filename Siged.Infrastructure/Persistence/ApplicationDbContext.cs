using Microsoft.EntityFrameworkCore;
using Siged.Domain.Entities.Core;
using Siged.Domain.Entities.Core.Notice;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence.Seeding;

namespace Siged.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        // Definimos el acceso a las entidades
        public DbSet<Persona> Personas { get; set; }
        public DbSet<Estudiante> Estudiantes { get; set; }
        public DbSet<Encargado> Encargados { get; set; }
        public DbSet<Administrador> Administradores { get; set; } 
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Permiso> Permisos { get; set; }
        public DbSet<Dependencia> Dependencias { get; set; }
        public DbSet<Organizacion> Organizaciones { get; set; }
        public DbSet<TokenInvalidado> TokensInvalidados { get; set; }
        public DbSet<AuditoriaLog> AuditoriaLogs { get; set; }
        public DbSet<News> News { get; set; }
        public DbSet<NewsMedia> NewsMedia { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }
        // --- Torneos y Competencia ---
        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<Phase> Phases { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Journal> Journals { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<MatchEvent> MatchEvents { get; set; }
        public DbSet<MatchLineup> MatchLineups { get; set; }
        public DbSet<MatchLineupPlayer> MatchLineupPlayers { get; set; }
        public DbSet<PlayerSanction> PlayerSanctions { get; set; }
        public DbSet<Discipline> Disciplines { get; set; }
        public DbSet<DisciplineRule> DisciplineRules { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamGestor> TeamGestores { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<VenueComplex> VenueComplexes { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<Competition> Competitions { get; set; }
        public DbSet<GroupTeam> GroupTeams { get; set; } = null!;
        public DbSet<CompetitionRule> CompetitionRules { get; set; }
        public DbSet<CompetitionTeam> CompetitionTeams { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Aplica las configuraciones de tablas (DNI, Nombres, etc.)
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
            modelBuilder.Entity<AppSetting>()
                .HasIndex(x => x.Key)
                .IsUnique();

            // ✅ LLAMA AL GRAN SEMBRADOR
            modelBuilder.Seed();
            modelBuilder.SeedTournaments();
        }
    }
}
