namespace TendexAI.Application.Common.Interfaces;

/// <summary>
/// Service for encrypting and decrypting connection strings.
/// Uses AES-256 encryption as per project security requirements.
/// </summary>
public interface IConnectionStringEncryptor
{
    /// <summary>
    /// Encrypts a plain-text connection string using AES-256.
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts an AES-256 encrypted connection string.
    /// </summary>
    string Decrypt(string cipherText);
}
