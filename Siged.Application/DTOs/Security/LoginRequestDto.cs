namespace Siged.Application.DTOs.Security;
public class LoginRequestDto
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}