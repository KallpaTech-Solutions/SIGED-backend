namespace Siged.Domain.Entities.Core.Tournaments.Enums;

/// <summary>
/// Rol de gestión sobre un equipo concreto (distinto del rol global de delegado de torneo).
/// </summary>
public enum TeamGestorKind : byte
{
    Principal = 0,
    Delegado = 1,
}
