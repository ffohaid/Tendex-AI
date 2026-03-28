namespace TendexAI.Application.Common.Interfaces.Identity;

/// <summary>
/// Abstraction for password hashing operations.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a plaintext password.</summary>
    string HashPassword(string password);

    /// <summary>Verifies a plaintext password against a hash.</summary>
    bool VerifyPassword(string password, string passwordHash);
}
