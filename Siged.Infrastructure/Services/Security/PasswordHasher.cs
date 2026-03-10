using BCrypt.Net;
using Siged.Application.Interfaces.Security;

namespace Siged.Infrastructure.Services.Security;

public class PasswordHasher : IPasswordHasher
{
    // HashPassword genera automáticamente un 'Salt' aleatorio
    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(password, passwordHash);
}