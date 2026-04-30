using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Api.Authorization;
using Siged.Application.DTOs.Tournaments.Player;
using Siged.Application.DTOs.Tournaments.Team;
using Siged.Application.Interfaces.Almacenamiento;
using Siged.Domain.Entities.Core;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;

namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bloqueado por defecto para seguridad SIGED
    public class TeamsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMediaStorageService _storageService;

        public TeamsController(ApplicationDbContext context, IMediaStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        private int? GetExecutorUsuarioId()
        {
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(s, out var id) ? id : null;
        }

        /// <summary>Delegado de escuela con permiso global o administración de torneo (ve todos los equipos de la org).</summary>
        private bool HasFullSchoolTeamAccess() =>
            TournDelegateAuth.IsTournamentAdmin(User) ||
            User.HasClaim("permission", Permissions.TournTeamManage);

        /// <summary>
        /// Retrieves all teams, optionally filtering by active status.
        /// </summary>
        /// <param name="onlyActive">true to include only active teams; false to include all teams.</param>
        /// <returns>An IActionResult containing a list of TeamDto objects.</returns>
        /// <response code="200">Returns the list of teams.</response>
        /// <response code="401">Unauthorized access.</response>
        /// <response code="500">Internal server error.</response>
        /// <response code="400">Bad request.</response>
        /// <response code="404">Not found.</response>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] bool onlyActive = true)
        {
            var query = _context.Teams
                .Include(t => t.Organizacion)
                .AsQueryable();

            if (onlyActive) query = query.Where(t => t.IsActive);

            var teams = await query.OrderBy(t => t.Name).Select(t => new TeamDto
            {
                Id = t.Id,
                Name = t.Name,
                Initials = t.Initials,
                LogoUrl = t.LogoUrl,
                RepresentativeName = t.RepresentativeName,
                IsActive = t.IsActive,
                // ✅ Ahora el compilador encontrará estas propiedades:
                NombreEscuela = t.Organizacion.Nombre,
                ColorEscuela = t.Organizacion.ColorRepresentativo
            }).ToListAsync();

            return Ok(teams);
        }

        [HttpGet("management-catalog")]
        [Authorize(Policy = TournDelegateOrTeamGestorAuth.PolicyName)]
        public async Task<IActionResult> GetManagementCatalog(
            [FromQuery] string? search = null,
            [FromQuery] int? organizacionId = null,
            [FromQuery] bool includeInactive = true)
        {
            var isAdmin = TournDelegateAuth.IsTournamentAdmin(User);
            var executorId = GetExecutorUsuarioId();
            var myOrgId = await TournDelegateAuth.GetOrganizacionIdAsync(User, _context);

            var query = _context.Teams
                .AsNoTracking()
                .Include(t => t.Organizacion)
                .Include(t => t.CreatedByUsuario)
                    .ThenInclude(u => u!.Persona)
                .AsQueryable();

            if (!includeInactive)
                query = query.Where(t => t.IsActive);

            if (isAdmin)
            {
                if (organizacionId is > 0)
                    query = query.Where(t => t.OrganizacionId == organizacionId.Value);
            }
            else
            {
                if (myOrgId == null)
                    return Ok(new { organizations = Array.Empty<object>(), teams = Array.Empty<object>() });

                query = query.Where(t => t.OrganizacionId == myOrgId.Value);
            }

            var s = (search ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(s))
            {
                var like = $"%{s}%";
                query = query.Where(t =>
                    EF.Functions.ILike(t.Name, like) ||
                    (t.Initials != null && EF.Functions.ILike(t.Initials, like)) ||
                    (t.RepresentativeName != null && EF.Functions.ILike(t.RepresentativeName, like)) ||
                    EF.Functions.ILike(t.Organizacion.Nombre, like));
            }

            var rows = await query
                .OrderBy(t => t.Organizacion.Nombre)
                .ThenBy(t => t.Name)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Initials,
                    t.LogoUrl,
                    t.RepresentativeName,
                    t.IsActive,
                    t.CreatedByUsuarioId,
                    createdBy = t.CreatedByUsuario != null
                        ? t.CreatedByUsuario.Persona.Nombres + " " + t.CreatedByUsuario.Persona.Apellidos
                        : null,
                    organizacionId = t.OrganizacionId,
                    escuela = t.Organizacion.Nombre,
                    playerCount = t.Players.Count,
                    activePlayerCount = t.Players.Count(p => p.IsActive),
                    inscriptions = t.CompetitionTeams.Select(ct => new
                    {
                        ct.CompetitionId,
                        tournamentId = ct.Competition.TournamentId,
                        tournamentName = ct.Competition.Tournament.Name,
                        competitionLabel = ct.Competition.Discipline.Name + " · " + (ct.Competition.CategoryName ?? "—") + " · " + ct.Competition.Gender,
                        ct.RosterLocked
                    }).ToList(),
                    canEdit = isAdmin || (executorId != null && t.CreatedByUsuarioId == executorId.Value),
                    canDelete = isAdmin || (executorId != null && t.CreatedByUsuarioId == executorId.Value)
                })
                .ToListAsync();

            var organizations = await _context.Organizaciones
                .AsNoTracking()
                .Where(o => isAdmin ? rows.Select(r => r.organizacionId).Contains(o.Id) : myOrgId != null && o.Id == myOrgId.Value)
                .OrderBy(o => o.Nombre)
                .Select(o => new { o.Id, o.Nombre })
                .ToListAsync();

            return Ok(new { organizations, teams = rows });
        }

        /// <summary>
        /// Delegados: escuela vinculada al usuario y equipos activos (para inscripción).
        /// </summary>
        [HttpGet("me/context")]
        [Authorize(Policy = TournDelegateOrTeamGestorAuth.PolicyName)]
        public async Task<IActionResult> GetMyContext()
        {
            var orgId = await TournDelegateAuth.GetOrganizacionIdAsync(User, _context);
            if (orgId == null)
                return Ok(new { organizacionId = (int?)null, nombreEscuela = (string?)null, teams = Array.Empty<object>() });

            var org = await _context.Organizaciones.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orgId.Value);

            var teamsQuery = _context.Teams
                .AsNoTracking()
                .Where(t => t.OrganizacionId == orgId.Value && t.IsActive);

            if (!HasFullSchoolTeamAccess())
            {
                var executorId = GetExecutorUsuarioId();
                if (executorId == null)
                    return Ok(new { organizacionId = orgId, nombreEscuela = org?.Nombre, teams = Array.Empty<object>() });

                var managedIds = await _context.TeamGestores.AsNoTracking()
                    .Where(g => g.UsuarioId == executorId.Value)
                    .Select(g => g.TeamId)
                    .ToListAsync();
                teamsQuery = teamsQuery.Where(t => managedIds.Contains(t.Id));
            }

            var teams = await teamsQuery
                .OrderBy(t => t.Name)
                .Select(t => new { t.Id, t.Name, t.Initials })
                .ToListAsync();

            return Ok(new { organizacionId = orgId, nombreEscuela = org?.Nombre, teams });
        }

        /// <summary>
        /// Panel delegado: equipos de la escuela, inscripciones por competencia/torneo y planteles (activos e inactivos).
        /// </summary>
        [HttpGet("me/summary")]
        [Authorize(Policy = TournDelegateOrTeamGestorAuth.PolicyName)]
        public async Task<IActionResult> GetMyDelegateSummary()
        {
            var orgId = await TournDelegateAuth.GetOrganizacionIdAsync(User, _context);
            if (orgId == null)
                return Ok(new { organizacionId = (int?)null, nombreEscuela = (string?)null, teams = Array.Empty<object>() });

            var org = await _context.Organizaciones.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orgId.Value);

            var teamQuery = _context.Teams
                .AsNoTracking()
                .Where(t => t.OrganizacionId == orgId.Value && t.IsActive);

            if (!HasFullSchoolTeamAccess())
            {
                var filterUid = GetExecutorUsuarioId();
                if (filterUid == null)
                    return Ok(new { organizacionId = orgId, nombreEscuela = org?.Nombre, teams = Array.Empty<object>() });

                var managedIds = await _context.TeamGestores.AsNoTracking()
                    .Where(g => g.UsuarioId == filterUid.Value)
                    .Select(g => g.TeamId)
                    .ToListAsync();
                teamQuery = teamQuery.Where(t => managedIds.Contains(t.Id));
            }

            var teamList = await teamQuery
                .OrderBy(t => t.Name)
                .Select(t => new { t.Id, t.Name, t.Initials, t.CreatedByUsuarioId })
                .ToListAsync();

            var teamIds = teamList.Select(t => t.Id).ToList();
            if (teamIds.Count == 0)
                return Ok(new { organizacionId = orgId, nombreEscuela = org?.Nombre, teams = Array.Empty<object>() });

            var gestorRows = await _context.TeamGestores.AsNoTracking()
                .Where(g => teamIds.Contains(g.TeamId))
                .Select(g => new { g.TeamId, g.UsuarioId, g.Kind })
                .ToListAsync();

            var executorId = GetExecutorUsuarioId();

            var ctRows = await _context.CompetitionTeams
                .AsNoTracking()
                .Where(ct => teamIds.Contains(ct.TeamId))
                .Select(ct => new
                {
                    ct.TeamId,
                    ct.CompetitionId,
                    TournamentId = ct.Competition.TournamentId,
                    TournamentName = ct.Competition.Tournament.Name,
                    DisciplineName = ct.Competition.Discipline.Name,
                    ct.Competition.CategoryName,
                    Gender = ct.Competition.Gender,
                    ct.RosterLocked,
                    ct.RosterLockedAt,
                    ct.RosterUnlockedAt
                })
                .ToListAsync();

            var playersRaw = await _context.Players
                .AsNoTracking()
                .Where(p => teamIds.Contains(p.TeamId))
                .OrderBy(p => p.Number)
                .ThenBy(p => p.Name)
                .ToListAsync();

            static PlayerDto MapPlayer(Player p) => new()
            {
                Id = p.Id,
                TeamId = p.TeamId,
                Name = p.Name,
                Dni = p.Dni,
                BirthDate = p.BirthDate,
                Number = p.Number,
                Position = p.Position,
                PhotoUrl = p.PhotoUrl,
                IsActive = p.IsActive,
                IsEligible = p.IsEligible
            };

            var teamsOut = teamList.Select(team =>
            {
                var inscriptions = ctRows
                    .Where(x => x.TeamId == team.Id)
                    .GroupBy(x => x.CompetitionId)
                    .Select(g => g.First())
                    .Select(x => new
                    {
                        competitionId = x.CompetitionId,
                        tournamentId = x.TournamentId,
                        tournamentName = x.TournamentName,
                        competitionLabel = $"{x.DisciplineName} · {x.CategoryName?.Trim() ?? "—"} · {x.Gender}",
                        rosterLocked = x.RosterLocked,
                        rosterLockedAt = x.RosterLockedAt,
                        rosterUnlockedAt = x.RosterUnlockedAt
                    })
                    .OrderBy(x => x.tournamentName)
                    .ThenBy(x => x.competitionLabel)
                    .ToList();

                var players = playersRaw
                    .Where(p => p.TeamId == team.Id)
                    .Select(MapPlayer)
                    .ToList();

                var gForTeam = gestorRows.Where(x => x.TeamId == team.Id).ToList();
                var explicitGestores = gForTeam.Count > 0;
                var canManage = TournDelegateAuth.IsTournamentAdmin(User) || (
                    executorId != null && (
                        !explicitGestores
                        || gForTeam.Any(x => x.UsuarioId == executorId.Value)
                    ));
                var iAmPrincipal = executorId != null && gForTeam.Any(x =>
                    x.UsuarioId == executorId.Value && x.Kind == TeamGestorKind.Principal);

                return new
                {
                    team.Id,
                    team.Name,
                    team.Initials,
                    createdByUsuarioId = team.CreatedByUsuarioId,
                    canManage,
                    iAmPrincipal,
                    tieneGestoresExplicitos = explicitGestores,
                    inscriptions,
                    players
                };
            }).ToList();

            return Ok(new { organizacionId = orgId, nombreEscuela = org?.Nombre, teams = teamsOut });
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id, [FromQuery] bool includeInactive = false)
        {
            var team = await _context.Teams
                .Include(t => t.Players)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null) return NotFound();

            var showInactive = includeInactive;
            if (showInactive)
            {
                if (User?.Identity?.IsAuthenticated != true)
                    showInactive = false;
                else if (!TournDelegateAuth.IsTournamentAdmin(User))
                {
                    var myOrg = await TournDelegateAuth.GetOrganizacionIdAsync(User, _context);
                    if (myOrg == null || team.OrganizacionId != myOrg.Value)
                        showInactive = false;
                }
            }

            IEnumerable<Player> playerQuery = team.Players;
            if (!showInactive)
                playerQuery = playerQuery.Where(p => p.IsActive);

            var playersOrdered = playerQuery
                .OrderBy(p => p.Number)
                .ThenBy(p => p.Name)
                .Select(p => new PlayerDto
                {
                    Id = p.Id,
                    TeamId = p.TeamId,
                    Name = p.Name,
                    Dni = p.Dni,
                    BirthDate = p.BirthDate,
                    Number = p.Number,
                    Position = p.Position,
                    PhotoUrl = p.PhotoUrl,
                    IsActive = p.IsActive,
                    IsEligible = p.IsEligible
                })
                .ToList();

            var response = new TeamDetailsDto
            {
                Id = team.Id,
                Name = team.Name,
                Initials = team.Initials,
                LogoUrl = team.LogoUrl,
                RepresentativeName = team.RepresentativeName,
                Players = playersOrdered
            };

            return Ok(response);
        }

        // --- CREACIÓN ---

        [HttpPost]
        [Authorize(Policy = TournDelegateAuth.PolicyName)]
        public async Task<IActionResult> Create([FromForm] CreateTeamDto dto)
        {
            var executorId = GetExecutorUsuarioId();
            if (executorId == null) return Unauthorized();

            // Delegado: el OrganizacionId viene siempre del usuario (no del formulario).
            int organizacionIdEquipo;
            if (!TournDelegateAuth.IsTournamentAdmin(User))
            {
                var myOrg = await TournDelegateAuth.GetOrganizacionIdAsync(User, _context);
                if (myOrg == null) return BadRequest("Tu usuario no tiene escuela asignada.");
                organizacionIdEquipo = myOrg.Value;
            }
            else
            {
                organizacionIdEquipo = dto.OrganizacionId;
                if (organizacionIdEquipo <= 0)
                    return BadRequest("Indicá la organización (OrganizacionId).");
            }

            var org = await _context.Organizaciones.FindAsync(organizacionIdEquipo);
            if (org == null) return BadRequest("La organización no existe.");

            if (!OrganizationCanFieldTeams(org))
                return BadRequest(
                    "Solo se pueden crear equipos vinculados a una organización de tipo Escuela o Facultad.");

            var isSuperAdmin = User.IsInRole("SuperAdmin");
            int principalUserId;
            int createdByUsuarioIdField;

            if (isSuperAdmin)
            {
                if (dto.PrincipalUsuarioId is not > 0)
                    return BadRequest(
                        "Como SuperAdmin debés indicar el delegado principal del equipo (PrincipalUsuarioId).");
                var principal = await _context.Usuarios.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == dto.PrincipalUsuarioId!.Value);
                if (principal == null || principal.OrganizacionId != organizacionIdEquipo)
                    return BadRequest(
                        "El delegado principal debe ser un usuario activo de la misma organización del equipo.");
                principalUserId = principal.Id;
                createdByUsuarioIdField = principal.Id;
            }
            else if (TournDelegateAuth.IsTournamentAdmin(User))
            {
                if (dto.PrincipalUsuarioId is > 0)
                {
                    var designated = await _context.Usuarios.AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == dto.PrincipalUsuarioId!.Value);
                    if (designated == null || designated.OrganizacionId != organizacionIdEquipo)
                        return BadRequest(
                            "El delegado principal indicado debe pertenecer a la organización del equipo.");
                    principalUserId = designated.Id;
                    createdByUsuarioIdField = designated.Id;
                }
                else
                {
                    principalUserId = executorId.Value;
                    createdByUsuarioIdField = executorId.Value;
                }
            }
            else
            {
                principalUserId = executorId.Value;
                createdByUsuarioIdField = executorId.Value;
            }

            string? logoUrl = dto.LogoFile != null
                ? await _storageService.UploadFileAsync(dto.LogoFile, "equipos")
                : null;

            var team = new Team
            {
                Name = dto.Name,
                OrganizacionId = organizacionIdEquipo,
                Initials = dto.Initials?.ToUpper(),
                RepresentativeName = dto.RepresentativeName,
                LogoUrl = logoUrl,
                IsActive = true,
                CreatedByUsuarioId = createdByUsuarioIdField
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            _context.TeamGestores.Add(new TeamGestor
            {
                TeamId = team.Id,
                UsuarioId = principalUserId,
                Kind = TeamGestorKind.Principal,
                AssignedAt = DateTime.UtcNow,
                AssignedByUsuarioId = executorId
            });
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = team.Id }, team);
        }

        // --- EDICIÓN ---

        [HttpPut("{id}")]
        [Authorize(Policy = TournDelegateAuth.PolicyName)]
        public async Task<IActionResult> Update(Guid id, [FromForm] CreateTeamDto dto)
        {
            var team = await _context.Teams.FindAsync(id);
            if (team == null) return NotFound();

            if (!await TeamManagementAuthorization.CanManageTeamAsync(User, _context, id))
                return Forbid();

            if (dto.LogoFile != null)
            {
                team.LogoUrl = await _storageService.UploadFileAsync(dto.LogoFile, "equipos");
            }

            team.Name = dto.Name;
            team.Initials = dto.Initials?.ToUpper();
            team.RepresentativeName = dto.RepresentativeName;

            await _context.SaveChangesAsync();
            return Ok(team);
        }

        // --- ESTADO Y ELIMINACIÓN ---

        [HttpPatch("{id}/status")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var team = await _context.Teams.FindAsync(id);
            if (team == null) return NotFound();

            team.IsActive = !team.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { id, isActive = team.IsActive });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = TournDelegateOrTeamGestorAuth.PolicyName)]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var executorId = GetExecutorUsuarioId();
            var team = await _context.Teams
                .Include(t => t.Players)
                    .ThenInclude(p => p.MatchEvents)
                .Include(t => t.Players)
                    .ThenInclude(p => p.MatchLineupPlayers)
                .Include(t => t.Players)
                    .ThenInclude(p => p.Sanctions)
                .Include(t => t.CompetitionTeams)
                .Include(t => t.GroupTeams)
                .Include(t => t.Gestores)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null) return NotFound();

            var canDelete = TournDelegateAuth.IsTournamentAdmin(User)
                || (executorId != null && team.CreatedByUsuarioId == executorId.Value);
            if (!canDelete)
                return Forbid();

            if (team.GroupTeams.Any())
                return BadRequest("No se puede eliminar: El equipo ya tiene historial en competiciones. Desactívelo.");

            if (await _context.Matches.AsNoTracking().AnyAsync(m => m.LocalTeamId == id || m.VisitorTeamId == id))
                return BadRequest("No se puede eliminar: El equipo ya tiene partidos generados. Desactívelo.");

            if (await _context.MatchLineups.AsNoTracking().AnyAsync(l => l.TeamId == id))
                return BadRequest("No se puede eliminar: El equipo ya tiene planillas de partido. Desactívelo.");

            if (await _context.PlayerSanctions.AsNoTracking().AnyAsync(s => s.TeamId == id))
                return BadRequest("No se puede eliminar: El equipo tiene historial de sanciones. Desactívelo.");

            if (team.Players.Any(p => p.MatchEvents.Any() || p.MatchLineupPlayers.Any() || p.Sanctions.Any()))
                return BadRequest("No se puede eliminar: uno o más jugadores ya tienen historial deportivo. Desactívelo.");

            _context.TeamGestores.RemoveRange(team.Gestores);
            _context.CompetitionTeams.RemoveRange(team.CompetitionTeams);
            _context.Players.RemoveRange(team.Players);
            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Usuarios de la misma escuela para designar co-delegados de un equipo.</summary>
        [HttpGet("me/org-users")]
        [Authorize(Policy = TournDelegateAuth.PolicyName)]
        public async Task<IActionResult> GetMyOrgUsersForGestores()
        {
            var orgId = await TournDelegateAuth.GetOrganizacionIdAsync(User, _context);
            if (orgId == null) return Ok(Array.Empty<object>());

            var list = await _context.Usuarios.AsNoTracking()
                .Where(u => u.OrganizacionId == orgId && u.EstaActivo)
                .OrderBy(u => u.Persona.Apellidos)
                .ThenBy(u => u.Persona.Nombres)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    nombreCompleto = u.Persona.Nombres + " " + u.Persona.Apellidos
                })
                .ToListAsync();

            return Ok(list);
        }

        /// <summary>Gestores registrados del equipo (misma escuela o admin de torneo).</summary>
        [HttpGet("{id:guid}/gestores")]
        [Authorize(Policy = TournDelegateOrTeamGestorAuth.PolicyName)]
        public async Task<IActionResult> GetTeamGestores(Guid id)
        {
            var team = await _context.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (team == null) return NotFound();

            if (!await TeamManagementAuthorization.CanManageTeamAsync(User, _context, id))
                return Forbid();

            var rows = await _context.TeamGestores.AsNoTracking()
                .Where(g => g.TeamId == id)
                .Join(_context.Usuarios.AsNoTracking(),
                    g => g.UsuarioId,
                    u => u.Id,
                    (g, u) => new { g.UsuarioId, g.Kind, u.Username, u.PersonaId })
                .Join(_context.Personas.AsNoTracking(),
                    x => x.PersonaId,
                    p => p.Id,
                    (x, p) => new
                    {
                        x.UsuarioId,
                        kind = x.Kind.ToString(),
                        x.Username,
                        nombreCompleto = p.Nombres + " " + p.Apellidos
                    })
                .OrderBy(x => x.nombreCompleto)
                .ToListAsync();

            return Ok(rows);
        }

        /// <summary>Delegado principal agrega un co-delegado (máx. 2).</summary>
        [HttpPost("{id:guid}/gestores")]
        [Authorize(Policy = TournDelegateAuth.PolicyName)]
        public async Task<IActionResult> AddTeamGestor(Guid id, [FromBody] AddTeamGestorDto dto)
        {
            var executorId = GetExecutorUsuarioId();
            if (executorId == null) return Unauthorized();

            if (!await TeamManagementAuthorization.TeamHasExplicitGestoresAsync(_context, id))
                return BadRequest(
                    "Este equipo no tiene delegación explícita en el sistema. Contactá a OTI para migrar el registro.");

            if (!TournDelegateAuth.IsTournamentAdmin(User) &&
                !await TeamManagementAuthorization.IsPrincipalGestorAsync(_context, id, executorId.Value))
                return Forbid();

            if (await _context.TeamGestores.AnyAsync(g => g.TeamId == id && g.UsuarioId == dto.UsuarioId))
                return BadRequest("Ese usuario ya es gestor del equipo.");

            var delegadoCount =
                await _context.TeamGestores.CountAsync(g =>
                    g.TeamId == id && g.Kind == TeamGestorKind.Delegado);
            if (delegadoCount >= TeamManagementAuthorization.MaxDelegadosPorEquipo)
                return BadRequest(
                    $"Se permiten como máximo {TeamManagementAuthorization.MaxDelegadosPorEquipo} co-delegados por equipo.");

            var team = await _context.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (team == null) return NotFound();

            var target = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == dto.UsuarioId);
            if (target == null || target.OrganizacionId != team.OrganizacionId || !target.EstaActivo)
                return BadRequest("El usuario debe estar activo y pertenecer a la misma organización del equipo.");

            _context.TeamGestores.Add(new TeamGestor
            {
                TeamId = id,
                UsuarioId = dto.UsuarioId,
                Kind = TeamGestorKind.Delegado,
                AssignedAt = DateTime.UtcNow,
                AssignedByUsuarioId = executorId
            });
            await _context.SaveChangesAsync();
            return Ok(new { message = "Co-delegado agregado." });
        }

        [HttpDelete("{id:guid}/gestores/{usuarioId:int}")]
        [Authorize(Policy = TournDelegateAuth.PolicyName)]
        public async Task<IActionResult> RemoveTeamGestor(Guid id, int usuarioId)
        {
            var executorId = GetExecutorUsuarioId();
            if (executorId == null) return Unauthorized();

            var row = await _context.TeamGestores.FirstOrDefaultAsync(g =>
                g.TeamId == id && g.UsuarioId == usuarioId);
            if (row == null) return NotFound();

            if (row.Kind == TeamGestorKind.Principal && !TournDelegateAuth.IsTournamentAdmin(User))
                return BadRequest("Solo administración de torneos puede quitar al delegado principal.");

            if (!TournDelegateAuth.IsTournamentAdmin(User) &&
                !await TeamManagementAuthorization.IsPrincipalGestorAsync(_context, id, executorId.Value))
                return Forbid();

            _context.TeamGestores.Remove(row);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Equipos de torneo se asocian a facultades o escuelas (no a la universidad raíz u otros tipos).</summary>
        private static bool OrganizationCanFieldTeams(Organizacion org)
        {
            var t = (org.Tipo ?? string.Empty).Trim();
            return t.Equals("Escuela", StringComparison.OrdinalIgnoreCase)
                || t.Equals("Facultad", StringComparison.OrdinalIgnoreCase);
        }
    }
}
