using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Tournaments.Group;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Infrastructure.Persistence;
using System.Linq;

namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GroupsController(ApplicationDbContext context) => _context = context;
        // --- LECTURA GLOBAL ---

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // Usamos el DTO de respuesta para mantener la consistencia y el rendimiento
            var groups = await _context.Groups
                .OrderBy(g => g.Name)
                .Select(g => new GroupResponseDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    QualifiedCount = g.QualifiedCount,
                    TeamsCount = g.GroupTeams.Count
                })
                .ToListAsync();

            return Ok(groups);
        }
        // --- LECTURA ---

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var group = await _context.Groups
                .Include(g => g.GroupTeams)
                    .ThenInclude(gt => gt.Team)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound();

            // Mapeo manual (o podrías usar AutoMapper después)
            var response = new GroupDetailsDto
            {
                Id = group.Id,
                Name = group.Name,
                QualifiedCount = group.QualifiedCount,
                Teams = group.GroupTeams.Select(gt => new TeamSummaryDto
                {
                    Id = gt.TeamId,
                    Name = gt.Team.Name,
                    LogoUrl = gt.Team.LogoUrl
                }).ToList()
            };

            return Ok(response);
        }

        [HttpGet("phase/{phaseId}")]
        public async Task<IActionResult> GetByPhase(Guid phaseId)
        {
            var groups = await _context.Groups
                .Where(g => g.PhaseId == phaseId)
                .Select(g => new GroupResponseDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    QualifiedCount = g.QualifiedCount,
                    TeamsCount = g.GroupTeams.Count
                })
                .ToListAsync();

            return Ok(groups);
        }

        // --- CREACIÓN Y EDICIÓN ---

        [HttpPost]
        public async Task<IActionResult> Create(CreateGroupDto dto)
        {
            var group = new Group
            {
                PhaseId = dto.PhaseId,
                Name = dto.Name,
                QualifiedCount = dto.QualifiedCount
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = group.Id }, group);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateGroupDto dto)
        {
            var group = await _context.Groups.FindAsync(id);
            if (group == null) return NotFound();

            group.Name = dto.Name;
            group.QualifiedCount = dto.QualifiedCount;

            await _context.SaveChangesAsync();
            return Ok(group);
        }

        // --- ELIMINACIÓN ---

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var group = await _context.Groups
                .Include(g => g.Journals) // Para ver si hay fixture
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound();

            // 🛡️ Regla de Oro: Si ya se generó el fixture, no se puede borrar el grupo.
            if (group.Journals.Any())
                return BadRequest("No se puede eliminar el grupo porque ya tiene un fixture generado. Elimine primero las jornadas.");

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // --- GESTIÓN DE EQUIPOS (LAS "INSCRIPCIONES") ---

        [HttpPost("assign-teams-bulk")]
        public async Task<IActionResult> AssignTeamsBulk(BulkAssignTeamsDto dto)
        {
            var phaseId = await _context.Groups
                .Where(g => g.Id == dto.GroupId)
                .Select(g => g.PhaseId)
                .FirstOrDefaultAsync();

            var alreadyAssignedIds = await _context.GroupTeams
                .Where(gt => gt.Group.PhaseId == phaseId && dto.TeamIds.Contains(gt.TeamId))
                .Select(gt => gt.TeamId)
                .ToListAsync();

            var newTeamIds = dto.TeamIds.Except(alreadyAssignedIds).ToList();

            if (!newTeamIds.Any()) return BadRequest("Los equipos ya están en esta fase.");

            var groupTeams = newTeamIds.Select(tId => new GroupTeam { GroupId = dto.GroupId, TeamId = tId });
            _context.GroupTeams.AddRange(groupTeams);
            await _context.SaveChangesAsync();

            return Ok(new { assigned = newTeamIds.Count, ignored = alreadyAssignedIds.Count });
        }
        [HttpPut("{id}/sync-teams")]
        public async Task<IActionResult> SyncTeams(Guid id, SyncGroupTeamsDto dto)
        {
            // 1. Verificar si el grupo existe e incluir las jornadas para validar el fixture
            var group = await _context.Groups
                .Include(g => g.Journals)
                .Include(g => g.GroupTeams)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound("Grupo no encontrado.");

            // 🛡️ REGLA DE ORO: Si ya hay fixture, no permitimos "sincronizar" (editar la lista completa)
            // porque eliminar equipos rompería la integridad de los partidos ya creados.
            if (group.Journals.Any())
            {
                return BadRequest("No se puede editar la lista de equipos porque el fixture ya ha sido generado. Debe eliminar el fixture primero.");
            }

            // 2. Identificar qué equipos quitar y qué equipos agregar
            var currentTeamIds = group.GroupTeams.Select(gt => gt.TeamId).ToList();

            var teamsToRemove = group.GroupTeams
                .Where(gt => !dto.TeamIds.Contains(gt.TeamId))
                .ToList();

            var teamIdsToAdd = dto.TeamIds
                .Where(tId => !currentTeamIds.Contains(tId))
                .Select(tId => new GroupTeam
                {
                    GroupId = id,
                    TeamId = tId
                })
                .ToList();

            // 3. Aplicar cambios
            if (teamsToRemove.Any()) _context.GroupTeams.RemoveRange(teamsToRemove);
            if (teamIdsToAdd.Any()) _context.GroupTeams.AddRange(teamIdsToAdd);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Lista de equipos sincronizada correctamente.",
                added = teamIdsToAdd.Count,
                removed = teamsToRemove.Count,
                total = dto.TeamIds.Count
            });
        }   

        [HttpDelete("{groupId}/teams/{teamId}")]
        public async Task<IActionResult> RemoveTeamFromGroup(Guid groupId, Guid teamId)
        {
            var groupTeam = await _context.GroupTeams
                .FirstOrDefaultAsync(gt => gt.GroupId == groupId && gt.TeamId == teamId);

            if (groupTeam == null) return NotFound("El equipo no está en este grupo.");

            // 🛡️ Validar que el equipo no tenga partidos jugados en este grupo
            var hasMatches = await _context.Matches
                .AnyAsync(m => m.Journal.GroupId == groupId && (m.LocalTeamId == teamId || m.VisitorTeamId == teamId));

            if (hasMatches)
                return BadRequest("No puedes retirar al equipo porque ya tiene partidos programados o jugados.");

            _context.GroupTeams.Remove(groupTeam);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
