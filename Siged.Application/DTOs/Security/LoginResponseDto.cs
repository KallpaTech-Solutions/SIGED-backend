namespace Siged.Application.DTOs.Security
{
    public class LoginResponseDto
    {
        public required string Token { get; set; }
        public required string Username { get; set; }
        public required string Rol { get; set; }
        public string? NombreCompleto { get; set; }
        public bool RequiereCambioPassword { get; set; }
    }
}
