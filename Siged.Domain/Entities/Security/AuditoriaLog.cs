namespace Siged.Domain.Entities.Security
{
    public class AuditoriaLog
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Accion { get; set; } = null!;
        public string? Detalle { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public string? IpAddress { get; set; }
    }
}