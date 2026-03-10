namespace Siged.Domain.Entities.Security
{
    public class TokenInvalidado
    {
        public int Id { get; set; }
        public string Token { get; set; } = null!;
        public DateTime FechaExpiracion { get; set; }
    }
}