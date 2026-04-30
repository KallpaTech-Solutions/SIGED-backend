using System.Collections.Generic;
using System.Linq;

namespace Siged.Domain.Entities.Security
{
    /// <summary>
    /// Estructura para definir un permiso y sus metadatos de asignación inicial.
    /// </summary>
    public record PermissionDefinition(string Name, string Description, string Category, int[] AssignedRoles);

    public static class Permissions
    {
        // --- 1. IDENTIFICADORES DE ROLES ---
        // Centralizamos los IDs para que coincidan con RolConfiguration
        public const int SuperAdminId = 1;
        public const int AdminId = 2;
        public const int EncargadoId = 3;
        public const int EstudianteId = 4;
        public const int DelegadoEscuelaId = 5;
        public const int GestorTorneoId = 6;
        public const int MesaControlId = 7;
        public const int MesaTransmisionId = 8;
        public const int EncargadoDisciplinaId = 9;

        // --- 2. SEMILLA DE ROLES ---
        // Esta lista alimenta a RolConfiguration.cs
        public static readonly List<Rol> RolesSeed = new()
        {
            new Rol { Id = SuperAdminId, Nombre = "SuperAdmin", Descripcion = "Acceso total a la configuración del sistema SIGED." },
            new Rol { Id = AdminId, Nombre = "Admin", Descripcion = "Responsable de la OTI - Gestión de usuarios y reportes." },
            new Rol { Id = EncargadoId, Nombre = "Encargado", Descripcion = "Docente o personal encargado de una disciplina deportiva." },
            new Rol { Id = EstudianteId, Nombre = "Estudiante", Descripcion = "Alumno matriculado habilitado para participar en eventos." },
            new Rol { Id = DelegadoEscuelaId, Nombre = "Delegado_Escuela", Descripcion = "Delegado que registra equipos y jugadores de su escuela." },
            new Rol { Id = GestorTorneoId, Nombre = "Gestor_Torneo", Descripcion = "Operador que configura competencias, fixture y listas oficiales." },
            new Rol { Id = MesaControlId, Nombre = "Mesa_Control", Descripcion = "Mesa que controla partidos, eventos, actas y habilitación deportiva." },
            new Rol { Id = MesaTransmisionId, Nombre = "Mesa_Transmision", Descripcion = "Operador de transmisión y widgets en vivo." },
            new Rol { Id = EncargadoDisciplinaId, Nombre = "Encargado_Disciplina", Descripcion = "Responsable deportivo de una disciplina o competencia." }
        };

        // --- 3. CATÁLOGO MAESTRO DE PERMISOS ---
        // Aquí defines TODO: Nombre, Descripción, Categoría y quién lo tiene al instalar el sistema.
        public static readonly List<PermissionDefinition> All = new()
        {
            // CATEGORÍA: SEGURIDAD
            new(SecurityUserView, "Ver lista de usuarios", "SEGURIDAD", new[] { SuperAdminId, AdminId }),
            new(SecurityUserManage, "Crear y editar usuarios", "SEGURIDAD", new[] { SuperAdminId, AdminId }),
            
            // Nota: Solo el SuperAdmin puede gestionar roles porque es una configuración crítica del sistema.
            new(SecurityRoleView, "Ver roles y permisos", "SEGURIDAD", new[] { SuperAdminId, AdminId }),
            new(SecurityRoleManage, "Gestionar roles y permisos", "SEGURIDAD", new[] { SuperAdminId,AdminId }),

            // CATEGORÍA: CORE (Organización)
            new(CoreOrgView, "Ver facultades y sedes", "CORE", new[] { SuperAdminId, AdminId, EncargadoId, EstudianteId, DelegadoEscuelaId, GestorTorneoId, MesaControlId, MesaTransmisionId, EncargadoDisciplinaId }),
            new(CoreOrgManage, "Administrar facultades", "CORE", new[] { SuperAdminId }),

            // CATEGORÍA: COMPETENCIAS
            new(CompTournView, "Ver torneos y resultados", "COMPETENCIAS", new[] { SuperAdminId, AdminId, EncargadoId, EstudianteId, DelegadoEscuelaId, GestorTorneoId, MesaControlId, MesaTransmisionId, EncargadoDisciplinaId }),
            new(CompTournManage, "Gestionar fechas y torneos", "COMPETENCIAS", new[] { SuperAdminId, EncargadoId, GestorTorneoId, EncargadoDisciplinaId }),

            // CATEGORÍA: NOTICIAS
            new(NewsView, "Ver gestión de noticias", "NOTICIAS", new[] { SuperAdminId, AdminId, EncargadoId }),
            new(NewsCreate, "Crear borradores de noticias", "NOTICIAS", new[] { SuperAdminId, AdminId, EncargadoId }),
            new(NewsManage, "Publicar, editar y archivar noticias", "NOTICIAS", new[] { SuperAdminId, AdminId }),
            new(NewsHighlight, "Marcar noticias como destacadas", "NOTICIAS", new[] { SuperAdminId, AdminId }),
            // CATEGORÍA: TORNEOS
            new(TournView, "Ver torneos, tablas y cronogramas", "TORNEOS", new[] { SuperAdminId, AdminId, EncargadoId, EstudianteId, DelegadoEscuelaId, GestorTorneoId, MesaControlId, MesaTransmisionId, EncargadoDisciplinaId }),
            new(TournManage, "Crear y editar torneos y disciplinas", "TORNEOS", new[] { SuperAdminId, AdminId }),
            new(TournConfig, "Gestionar fases y sorteo de grupos", "TORNEOS", new[] { SuperAdminId, AdminId, EncargadoId, GestorTorneoId, EncargadoDisciplinaId }),
            new(TournTeamManage, "Administrar equipos y enrolamiento de jugadores", "TORNEOS", new[] { SuperAdminId, EncargadoId, DelegadoEscuelaId, GestorTorneoId, EncargadoDisciplinaId }),
            new(TournFixture, "Generar fixture y programar encuentros", "TORNEOS", new[] { SuperAdminId, EncargadoId, GestorTorneoId, EncargadoDisciplinaId }),
            new(TournMatchControl, "Control de mesa: registro de eventos y actas", "TORNEOS", new[] { SuperAdminId, EncargadoId, MesaControlId, EncargadoDisciplinaId }),
            new(TournMatchWidgets, "Widgets de transmisión: tableros y tiempos en vivo", "TORNEOS", new[] { SuperAdminId, AdminId, EncargadoId, MesaTransmisionId }),
            new(TournLineupManage, "Gestionar planillas y listas oficiales de equipos", "TORNEOS", new[] { SuperAdminId, EncargadoId, GestorTorneoId, EncargadoDisciplinaId }),
            new(TournMatchReportDownload, "Descargar actas de partido", "TORNEOS", new[] { SuperAdminId, AdminId, EncargadoId, GestorTorneoId, MesaControlId, EncargadoDisciplinaId }),
            new(TournPlayerSanctionManage, "Gestionar sanciones e inhabilitaciones de jugadores", "TORNEOS", new[] { SuperAdminId, EncargadoId, MesaControlId, EncargadoDisciplinaId }),
         };

        // --- 4. CONSTANTES DE STRINGS (Para usar en [Authorize(Policy = ...)]) ---
        // Mantener estas constantes te ayuda a evitar errores de dedo en los Controladores.
        public const string SecurityUserView = "security.user.view";
        public const string SecurityUserManage = "security.user.manage";
        
        public const string SecurityRoleManage = "security.role.manage";
        public const string SecurityRoleView = "security.role.view";

        public const string CoreOrgView = "core.org.view";
        public const string CoreOrgManage = "core.org.manage";

        public const string CompTournView = "comp.tourn.view";
        public const string CompTournManage = "comp.tourn.manage";
        //Tournamentes, disciplinas, competencias, resultados
        public const string TournView = "tourn.view";                // Ver torneos, tablas y resultados (Público/Estudiante)
        public const string TournManage = "tourn.manage";            // Crear torneos y disciplinas (Admin OTI)
        public const string TournConfig = "tourn.config";            // Configurar Fases y Grupos (Encargado/Admin)
        public const string TournTeamManage = "tourn.team.manage";    // Inscribir Equipos y Jugadores (Encargado)
        public const string TournFixture = "tourn.fixture";          // Programar fechas, horas y sedes (Encargado/Mesa)
        public const string TournMatchControl = "tourn.match.control"; // Registrar goles, tarjetas y finalizar (Mesa)
        /// <summary>Configurar plantillas de vitrina (tableros deportivos / tiempos) durante la transmisión.</summary>
        public const string TournMatchWidgets = "tourn.match.widgets";
        public const string TournLineupManage = "tourn.lineup.manage";
        public const string TournMatchReportDownload = "tourn.match.report.download";
        public const string TournPlayerSanctionManage = "tourn.player.sanction.manage";
        /// <summary>Política compuesta: mesa o operador de gráficos puede guardar el JSON del widget.</summary>
        public const string TournMesaBroadcast = "tourn.mesa.broadcast";
        // CATEGORÍA: NOTICIAS
        public const string NewsView = "news.view";          // Ver el panel de gestión
        public const string NewsCreate = "news.create";      // Crear borradores
        public const string NewsManage = "news.manage";      // Publicar, editar y archivar
        public const string NewsHighlight = "news.highlight"; // Marcar como destacada

        // --- 5. MÉTODOS AUXILIARES ---
        public static List<string> GetAllNames() => All.Select(p => p.Name).ToList();
    }
}