namespace Siged.Application.Interfaces.Security
{
    public interface IPasswordHasher
    {
        // Genera el hash encriptado
        string Hash(string password);

        // Compara una clave en texto plano con el hash de la BD
        bool Verify(string password, string passwordHash);
    }
}